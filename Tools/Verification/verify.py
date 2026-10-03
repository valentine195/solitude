#!/usr/bin/env python3
"""Isolated macOS ARM64 validation, test, player restart and interrupted-file gate."""
import argparse, glob, json, os, pathlib, shutil, subprocess, sys, time, xml.etree.ElementTree as ET
ROOT=pathlib.Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--workspace',type=pathlib.Path,default=pathlib.Path('/private/tmp/solitude-verification-project'));p.add_argument('--output',type=pathlib.Path,default=ROOT/'Logs'/'Phase5');p.add_argument('--unity',default=str(pathlib.Path.home()/'.unity/bin/unity'));p.add_argument('--skip-build',action='store_true',help='Rerun player scenarios against existing outputs; not a complete gate')
a=p.parse_args();workspace=a.workspace.resolve();output=a.output.resolve();output.mkdir(parents=True,exist_ok=True)
if workspace==ROOT or ROOT in workspace.parents or 'solitude-' not in workspace.name or str(workspace.parent) not in ('/private/tmp','/tmp'):raise SystemExit('Use a dedicated solitude-* workspace under /tmp.')
env=os.environ.copy();env['SOLITUDE_VALIDATION_OUTPUT']=str(output/'validation');results=[]
def run(cmd,name,timeout=360,expected=0):
 print(name,flush=True);start=time.monotonic()
 with (output/(name+'.runner.log')).open('w') as log:
  proc=subprocess.run(list(map(str,cmd)),cwd=ROOT,env=env,stdout=log,stderr=subprocess.STDOUT,timeout=timeout)
 if expected is not None and proc.returncode!=expected:raise RuntimeError(f'{name}: exit {proc.returncode}; see {output/name}.runner.log')
 results.append(dict(name=name,seconds=round(time.monotonic()-start,2),exit=proc.returncode));return proc

def editor(method,name):return run([a.unity,'run',workspace,'--timeout','300','--no-tail','--log-file',output/(name+'.log'),'--','-nographics','-executeMethod',method],name)
def tests(mode,name):
 location=output/(name+'-saves');location.mkdir(exist_ok=True)
 run([a.unity,'test',workspace,'--mode',mode,'--filter','SOLITUDE.Tests','--output',output/(name+'.xml'),'--timeout','300','--','-nographics','-logFile',output/(name+'.log'),'-solitudeSaveDirectory',location],name)
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

try:
 if not a.skip_build:
  workspace.mkdir(parents=True,exist_ok=True)
  for folder in ['Assets','Packages','ProjectSettings']:run(['rsync','-a','--delete',str(ROOT/folder)+'/',str(workspace/folder)+'/'],'copy-'+folder,120)
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
 status='Passed' if not a.skip_build else 'Player scenarios passed (partial gate)';(output/'results.json').write_text(json.dumps(dict(status=status,platform='macOS ARM64',results=results),indent=2));print(status,flush=True)
except Exception as error:
 (output/'results.json').write_text(json.dumps(dict(status='Failed',error=str(error),results=results),indent=2));print(error,file=sys.stderr);sys.exit(1)
