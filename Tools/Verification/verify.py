#!/usr/bin/env python3
"""Isolated macOS ARM64 validation, test, player restart and interrupted-file gate."""
import argparse, glob, json, os, pathlib, shutil, subprocess, sys, time, xml.etree.ElementTree as ET
ROOT=pathlib.Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--workspace',type=pathlib.Path,default=pathlib.Path('/private/tmp/solitude-verification-project'));p.add_argument('--output',type=pathlib.Path,default=ROOT/'Logs'/'Phase5');p.add_argument('--unity',default=str(pathlib.Path.home()/'.unity/bin/unity'));p.add_argument('--skip-build',action='store_true',help='Rerun player scenarios against existing outputs; not a complete gate')
p.add_argument('--starting-loadout-only',action='store_true',help='Focused starting-equipment process scenarios against an explicitly rebuilt verification player; requires --skip-build')
p.add_argument('--mechanical-repair-only',action='store_true',help='Focused repair process scenarios; requires current repair verification build and --skip-build')
a=p.parse_args();workspace=a.workspace.resolve();output=a.output.resolve();output.mkdir(parents=True,exist_ok=True)
if workspace==ROOT or ROOT in workspace.parents or 'solitude-' not in workspace.name or str(workspace.parent) not in ('/private/tmp','/tmp'):raise SystemExit('Use a dedicated solitude-* workspace under /tmp.')
env=os.environ.copy();env['SOLITUDE_VALIDATION_OUTPUT']=str(output/'validation');results=[]
def run(cmd,name,timeout=360,expected=0,input_data=None):
 print(name,flush=True);start=time.monotonic()
 with (output/(name+'.runner.log')).open('w') as log:
  proc=subprocess.run(list(map(str,cmd)),cwd=ROOT,env=env,stdout=log,stderr=subprocess.STDOUT,timeout=timeout,input=input_data)
 if expected is not None and proc.returncode!=expected:raise RuntimeError(f'{name}: exit {proc.returncode}; see {output/name}.runner.log')
 results.append(dict(name=name,seconds=round(time.monotonic()-start,2),exit=proc.returncode));return proc

def editor(method,name):return run([a.unity,'run',workspace,'--timeout','300','--no-tail','--log-file',output/(name+'.log'),'--','-nographics','-executeMethod',method],name)
def tests(mode,name):
 location=output/(name+'-saves');location.mkdir(exist_ok=True)
 # PlayMode includes screenshot/render-request tests and therefore needs a real graphics device.
 graphics=[] if mode=='PlayMode' else ['-nographics']
 # Room authoring fixtures require explicit opt-in in this disposable project.
 isolation=['-solitudeRoomAuthoringVerification'] if mode=='EditMode' else []
 run([a.unity,'test',workspace,'--mode',mode,'--filter','SOLITUDE.Tests','--output',output/(name+'.xml'),'--timeout','300','--',*graphics,*isolation,'-logFile',output/(name+'.log'),'-solitudeSaveDirectory',location],name)
 doc=ET.parse(output/(name+'.xml')).getroot()
 if int(doc.get('failed','0')) or int(doc.get('passed','0'))==0:raise RuntimeError(f'{name}: no passing test suite')
 results[-1]['passed']=int(doc.get('passed'));results[-1]['failed']=int(doc.get('failed','0'))

def player(directory,flags,name,crash=False):
 directory.mkdir(parents=True,exist_ok=True);result=directory/'result.json'
 if result.exists():result.unlink()
 proc=run([output/'verification.app/Contents/MacOS/SolitudeVerification','-batchmode','-logFile',output/(name+'.log'),'-solitudeSaveDirectory',directory,*flags],name,60,None if crash else 0)
 text=(output/(name+'.log')).read_text(errors='replace')
 if 'Exception:' in text or 'MissingReferenceException' in text:raise RuntimeError(f'{name}: unexpected player exception')
 if crash:
  if proc.returncode==0 or not (directory/'checkpoint.txt').exists():raise RuntimeError(f'{name}: missing deliberate interruption')
 else:
  if not result.exists() or not json.loads(result.read_text()).get('success'):raise RuntimeError(f'{name}: absent/failed scenario result')

def starting_loadout_scenarios():
 def read(directory): return json.loads((directory/'solitude-save.json').read_text())
 def carried(data): return [c for c in data['containers'] if c['saveableId'] in ['player.inventory','player.hotbar']]
 def count(data): return sum(slot['quantity'] for c in carried(data) for slot in c['state']['slots'] if slot['itemId']==item)
 def fixture(name,data):
  directory=output/name
  if directory.exists(): raise RuntimeError('Refusing to replace focused scenario evidence: '+str(directory))
  directory.mkdir();(directory/'solitude-save.json').write_text(json.dumps(data));return directory
 fresh=output/'loadout-fresh'
 if fresh.exists(): raise RuntimeError('Use a fresh output directory for loadout verification')
 player(fresh,['-verifyStartingLoadout','-expectFreshStartingSelection'],'loadout-fresh')
 complete=read(fresh);bar=next(c for c in carried(complete) if c['saveableId']=='player.hotbar')
 item=bar['state']['slots'][0]['itemId']
 if not complete['startingLoadoutInitialized'] or count(complete)!=1: raise RuntimeError('Fresh loadout was not durably complete once')
 pending=json.loads(json.dumps(complete));pending['startingLoadoutInitialized']=False
 for c in carried(pending):c['state']['slots']=[]
 for checkpoint in ['loadout-before-grant','loadout-before-flush','before-temp','after-temp','before-commit','after-commit']:
  name='loadout-'+checkpoint;directory=fixture(name,pending)
  player(directory,['-verifyStartingLoadout','-faultStartingLoadout','-solitudeCrashAt',checkpoint],name,True)
  saved=read(directory);committed=checkpoint=='after-commit'
  if saved['startingLoadoutInitialized']!=committed or count(saved)!=(1 if committed else 0):raise RuntimeError('Interrupted loadout published partial or false completion')
  player(directory,['-verifyStartingLoadout']+([] if committed else ['-expectFreshStartingSelection']),'restart-'+name)
  if not read(directory)['startingLoadoutInitialized'] or count(read(directory))!=1:raise RuntimeError('Restart failed once-only grant')
  player(directory,['-verifyStartingLoadout'],'second-restart-'+name)
  if count(read(directory))!=1:raise RuntimeError('Second restart duplicated grant')
 for name in ['lost','moved','legacy']:
  data=json.loads(json.dumps(complete));saved_bar=next(c for c in carried(data) if c['saveableId']=='player.hotbar');slots=saved_bar['state']['slots'];saved_bar['state']['slots']=[]
  if name=='moved':next(c for c in carried(data) if c['saveableId']=='player.inventory')['state']['slots']=slots
  if name=='legacy':data['version']=3;data.pop('startingLoadoutInitialized')
  directory=fixture('loadout-'+name,data)
  player(directory,['-verifyStartingLoadout']+([] if name=='moved' else ['-expectNoStartingWrench']),'loadout-'+name)
  if count(read(directory))!=(1 if name=='moved' else 0):raise RuntimeError('Completed equipment was refreshed')
  if not read(directory)['startingLoadoutInitialized']:raise RuntimeError('Completed or legacy marker was lost')
 directory=fixture('loadout-backup',complete);backup=(fresh/'solitude-save.json').read_bytes();(directory/'solitude-save.json.bak').write_bytes(backup);(directory/'solitude-save.json').write_text('{')
 player(directory,['-verifyStartingLoadout'],'loadout-backup')
 if (directory/'solitude-save.json.bak').read_bytes()!=backup:raise RuntimeError('Loadout recovery changed backup bytes')
 partial=json.loads(json.dumps(complete));partial['startingLoadoutInitialized']=False;directory=fixture('loadout-partial',partial)
 player(directory,['-expectBlocked'],'loadout-partial')

def mechanical_repair_scenarios():
 def fresh(name, source=None):
  directory=output/('step4-mechanical-'+name)
  if directory.exists():raise RuntimeError('Refusing to replace repair evidence: '+str(directory))
  directory.mkdir()
  if source:
   for path in source.glob('solitude-save.json*'):
    if path.suffix != '.tmp':shutil.copy2(path,directory/path.name)
  return directory
 def flags(*extra):return ['-verifyMechanicalRepair',*extra]
 def data(directory):return json.loads((directory/'solitude-save.json').read_text())
 def pair(directory):
  saved=data(directory)
  repaired=any(r['id']=='verification.mechanical.auxiliary-socket' and r['installed'] and r['running'] for r in saved['ship']['repairs'])
  count=sum(s['quantity'] for c in saved['containers'] if c['saveableId']=='player.inventory' for s in c['state']['slots'])
  return repaired,count
 seed=fresh('seed');player(seed,flags('-repairSeed'),'repair-seed')
 if pair(seed)!=(False,2):raise RuntimeError('Seed repair/cost mismatch')
 completed=fresh('commit',seed);player(completed,flags('-repairCommit'),'repair-commit')
 if pair(completed)!=(True,1):raise RuntimeError('Completed repair/cost mismatch')
 player(completed,flags('-repairExpectComplete'),'repair-reload')
 failed=fresh('fail-once',seed);player(failed,flags('-repairCommit','-repairFailOnce'),'repair-fail-retry')
 if pair(failed)!=(True,1):raise RuntimeError('Retry double charged or split repair')
 for checkpoint in ['repair-before-commit','before-temp','after-temp','before-commit','after-commit']:
  directory=fresh('write-'+checkpoint,seed)
  player(directory,flags('-repairCommit','-solitudeCrashAt',checkpoint),'repair-write-'+checkpoint,True)
  expected=(True,1) if checkpoint=='after-commit' else (False,2)
  if pair(directory)!=expected:raise RuntimeError('Interrupted repair published mixed snapshot')
  if data(directory)!=data(completed if expected[0] else seed):raise RuntimeError('Interrupted repair changed unrelated snapshot facts')
  player(directory,flags(*(['-repairExpectComplete'] if expected[0] else [])),'repair-restart-'+checkpoint)
 for checkpoint in ['after-receipt','after-temp','before-commit','after-commit']:
  directory=fresh('recovery-'+checkpoint,completed)
  primary=directory/'solitude-save.json';backup=directory/'solitude-save.json.bak'
  backup.write_bytes(primary.read_bytes());original=backup.read_bytes();primary.write_text('{')
  player(directory,['-solitudeCrashAt',checkpoint],'repair-recovery-'+checkpoint,True)
  if backup.read_bytes()!=original:raise RuntimeError('Interrupted repair recovery changed backup bytes')
  player(directory,flags('-repairExpectComplete'),'repair-recovery-restart-'+checkpoint)
  if backup.read_bytes()!=original or pair(directory)!=(True,1):raise RuntimeError('Repair recovery lost coherent backup')
 for name,flag in [('scene-return','-repairSceneReturn'),('successor','-repairSuccessor'),('new-run','-repairNewRun')]:
  directory=fresh(name,completed);player(directory,flags('-repairExpectComplete',flag),'repair-'+name)
 directory=fresh('duplicate',completed);saved=data(directory);saved['ship']['repairs'].append(saved['ship']['repairs'][0]);(directory/'solitude-save.json').write_text(json.dumps(saved));(directory/'solitude-save.json.bak').unlink(missing_ok=True)
 player(directory,['-expectBlocked','-verifyMechanicalRepair'],'repair-duplicate-blocked')

try:
 if a.mechanical_repair_only:
  if not a.skip_build:raise RuntimeError('Focused repair scenarios require --skip-build and an explicitly rebuilt repair verification player')
  mechanical_repair_scenarios()
  (output/'results.json').write_text(json.dumps(dict(status='Passed (focused mechanical repair process scenarios)',platform='macOS ARM64',results=results),indent=2));print('Focused repair scenarios passed',flush=True);sys.exit(0)
 if a.starting_loadout_only:
  if not a.skip_build:raise RuntimeError('Focused loadout scenarios require --skip-build and an explicitly rebuilt verification player')
  starting_loadout_scenarios()
  (output/'results.json').write_text(json.dumps(dict(status='Passed (focused starting-loadout process scenarios)',platform='macOS ARM64',results=results),indent=2));print('Focused starting-loadout scenarios passed',flush=True);sys.exit(0)
 if not a.skip_build:
  workspace.mkdir(parents=True,exist_ok=True)
  for folder in ['Assets','Packages','ProjectSettings']:run(['rsync','-a','--delete',str(ROOT/folder)+'/',str(workspace/folder)+'/'],'copy-'+folder,120)
  # Authoring validation queries tracked asset/meta pairs. Mirror the source index,
  # not `git add Assets`, which would hide missing tracked metadata by staging it.
  # This metadata-only repository owns its index; it never points at the source .git.
  git_dir=workspace/'.git'
  if git_dir.is_symlink() or (git_dir.exists() and not git_dir.is_dir()):
   raise RuntimeError('Verification workspace must own its disposable .git directory')
  tracked=subprocess.run(['git','ls-files','--stage','-z','--','Assets'],cwd=ROOT,check=True,stdout=subprocess.PIPE).stdout
  run(['git','init',workspace],'prepare-git')
  run(['git','-C',workspace,'read-tree','--empty'],'reset-verification-index')
  run(['git','-C',workspace,'update-index','-z','--index-info'],'copy-verification-index',input_data=tracked)
  version=(ROOT/'ProjectSettings/ProjectVersion.txt').read_text().split('m_EditorVersion: ')[1].splitlines()[0]
  sdk=pathlib.Path('/Applications/Unity/Hub/Editor')/version/'Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet'
  nunit=next((ROOT/'Library/PackageCache').glob('com.unity.ext.nunit@*/net472/unity-custom/nunit.framework.dll'))
  run([sdk,'run','--project',ROOT/'Tools/Verification/PureTests.csproj','-p:NUnitPath='+str(nunit)],'pure-tests')
  editor('SOLITUDE.Editor.WakeupValidation.ValidateForCI','authoring')
  tests('EditMode','editmode')
  editor('SOLITUDE.Editor.PhaseFiveFixtureBuilder.Prepare','prepare-fixtures')
  tests('PlayMode','playmode')
  editor('SOLITUDE.Editor.PhaseFiveFixtureBuilder.RestoreBuildScope','restore-scope')
  for method,name in [('BuildVerification','verification'),('BuildShipping','shipping')]:
   run([a.unity,'build',workspace,'--target','StandaloneOSX','--execute-method','SOLITUDE.Editor.WakeupPlayerBuilder.'+method,'--output-path',output/(name+'.app'),'--allow-dirty-build','--timeout','300','--no-tail','--log-file',output/(name+'-build.log')],name+'-build')
 first=output/'world';shutil.rmtree(first,ignore_errors=True);player(first,[],'world-collect')
 baseline=output/'baseline';shutil.rmtree(baseline,ignore_errors=True);shutil.copytree(first,baseline)
 player(first,['-verifyRestart'],'world-restart-newsave')
 def scenario(name):
  target=output/name;shutil.rmtree(target,ignore_errors=True);shutil.copytree(baseline,target);(target/'solitude-save.json.bak').write_bytes((baseline/'solitude-save.json').read_bytes());return target
 for kind in ['partial-temp','missing-primary','corrupt-primary','both-corrupt','temp-only','incompatible-primary','unknown-item','capacity-primary','capacity-backup','read-denied']:
  directory=scenario(kind);primary=directory/'solitude-save.json';backup=directory/'solitude-save.json.bak';before=primary.read_bytes()
  flags=['-verifySnapshotOnly'];blocked=False
  if kind=='partial-temp':(directory/'solitude-save.json.tmp').write_text('{')
  if kind=='missing-primary':primary.unlink();flags+=['-expectRecovery']
  if kind=='corrupt-primary':primary.write_text('{');flags+=['-expectRecovery']
  if kind=='both-corrupt':primary.write_text('{');backup.write_text('{');blocked=True
  if kind=='temp-only':primary.unlink();backup.unlink();(directory/'solitude-save.json.tmp').write_bytes(before);blocked=True
  if kind=='incompatible-primary':data=json.loads(primary.read_text());data['version']=99;primary.write_text(json.dumps(data));blocked=True
  if kind in ['unknown-item','capacity-primary','capacity-backup']:
   data=json.loads(primary.read_text());record=next(c for c in data['containers'] if c['saveableId']==json.loads((directory/'world-expected.json').read_text())['lockerId'])
   if not record['state']['slots']:raise RuntimeError('Capacity fixture requires saved locker loot')
   if kind=='unknown-item':record['state']['slots'][0]['itemId']='removed.verification.item'
   else:record['state']['slots'][0]['quantity']=50000
   if kind=='capacity-backup':backup.write_text(json.dumps(data));primary.write_text('{')
   else:primary.write_text(json.dumps(data))
   blocked=True
  if kind=='read-denied':primary.chmod(0);blocked=True
  try:player(directory,['-expectBlocked'] if blocked else flags,kind)
  finally:
   if kind=='read-denied':primary.chmod(0o600)
  if kind in ['missing-primary','corrupt-primary'] and backup.read_bytes()!=before:raise RuntimeError('Recovery corrupted backup')
 for checkpoint in ['before-temp','after-temp','before-commit','after-commit']:
  directory=scenario('write-'+checkpoint);before=(directory/'solitude-save.json').read_bytes()
  player(directory,['-faultWrite','-solitudeCrashAt',checkpoint],'write-'+checkpoint,True)
  data=json.loads((directory/'solitude-save.json').read_text());original=json.loads(before)
  if data['worldSeed']!=original['worldSeed']+(1 if checkpoint=='after-commit' else 0):raise RuntimeError('Interrupted write changed committed snapshot incorrectly')
  player(directory,['-verifySnapshotOnly'],'restart-write-'+checkpoint)
 for checkpoint in ['after-receipt','after-temp','before-commit','after-commit']:
  directory=scenario('recovery-'+checkpoint);(directory/'solitude-save.json').write_text('{');backup=(directory/'solitude-save.json.bak').read_bytes()
  player(directory,['-verifySnapshotOnly','-solitudeCrashAt',checkpoint],'recovery-'+checkpoint,True)
  if (directory/'solitude-save.json.bak').read_bytes()!=backup:raise RuntimeError('Interrupted recovery changed backup')
  player(directory,['-verifySnapshotOnly','-expectRecovery'],'restart-recovery-'+checkpoint)
 # Living-colonist process qualification uses separate saves and compares exact snapshots.
 living=output/'lifecycle-write';shutil.rmtree(living,ignore_errors=True)
 player(living,['-lifecycleWrite'],'lifecycle-write')
 expected=json.loads((living/'lifecycle-expected.json').read_text())
 saved=json.loads((living/'solitude-save.json').read_text())
 if saved['survivor']!=expected['survivor'] or saved['expedition']['id']!=expected['expeditionId']:
  raise RuntimeError('Orderly quit did not capture the clean living session')
 for kind in ['resume','fallback','unavailable-scene']:
  directory=output/('lifecycle-'+kind);shutil.rmtree(directory,ignore_errors=True);shutil.copytree(living,directory)
  primary=directory/'solitude-save.json';backup=directory/'solitude-save.json.bak'
  data=json.loads(primary.read_text())
  if kind=='fallback':data['survivor']['x']=100000;data['survivor']['y']=100000
  if kind=='unavailable-scene':data['survivor']['scenePath']='Assets/_Project/Scenes/MissingProductionScene.unity'
  if kind!='resume':primary.write_text(json.dumps(data))
  before=primary.read_bytes();backup_before=backup.read_bytes()
  player(directory,['-expectBlocked'] if kind=='unavailable-scene' else ['-lifecycleFallback' if kind=='fallback' else '-lifecycleResume'],'lifecycle-'+kind)
  if kind=='unavailable-scene' and (primary.read_bytes()!=before or backup.read_bytes()!=backup_before):
   raise RuntimeError('Unavailable saved scene changed primary or backup bytes')
 status='Passed' if not a.skip_build else 'Player scenarios passed (partial gate)';(output/'results.json').write_text(json.dumps(dict(status=status,platform='macOS ARM64',results=results),indent=2));print(status,flush=True)
except Exception as error:
 (output/'results.json').write_text(json.dumps(dict(status='Failed',error=str(error),results=results),indent=2));print(error,file=sys.stderr);sys.exit(1)
