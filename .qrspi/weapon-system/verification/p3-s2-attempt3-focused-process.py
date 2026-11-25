#!/usr/bin/env python3
"""Isolated macOS ARM64 validation, test, player restart and interrupted-file gate."""
import argparse, glob, json, os, pathlib, shutil, subprocess, sys, time, xml.etree.ElementTree as ET
ROOT=pathlib.Path('/Users/jeremyvalentine/SOLITUDE')
p=argparse.ArgumentParser();p.add_argument('--workspace',type=pathlib.Path,default=pathlib.Path('/private/tmp/solitude-verification-project'));p.add_argument('--output',type=pathlib.Path,default=ROOT/'Logs'/'Phase5');p.add_argument('--unity',default=str(pathlib.Path.home()/'.unity/bin/unity'));p.add_argument('--skip-build',action='store_true',help='Rerun player scenarios against existing outputs; not a complete gate')
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

try:
 baseline=ROOT/'Logs/WeaponSystemP3S2Attempt2/baseline'
 def scenario(name):
  target=output/name
  if target.exists(): raise RuntimeError('Refusing to overwrite existing scenario evidence: '+str(target))
  shutil.copytree(baseline,target)
  (target/'solitude-save.json.bak').write_bytes((baseline/'solitude-save.json').read_bytes())
  return target
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
 status='Passed (focused write/recovery/lifecycle scenarios)'
 (output/'focused-results.json').write_text(json.dumps(dict(status=status,platform='macOS ARM64',results=results),indent=2));print(status,flush=True)
except Exception as error:
 (output/'focused-results.json').write_text(json.dumps(dict(status='Failed',error=str(error),results=results),indent=2));print(error,file=sys.stderr);sys.exit(1)
