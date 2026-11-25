from pathlib import Path
import hashlib,re,json
root=Path('/Users/jeremyvalentine/SOLITUDE')
expected={
'.qrspi/task-journal/request.md':'3c42a6d8bb393ec7b205526ea77414cceb9c5218043068446740dc63fdbadba9',
'.qrspi/task-journal/queries.md':'69041f2d27cadb179fc20d7c8c96062bdbddd3783ea9fdf6807f7f038a999662',
'.qrspi/task-journal/research.md':'564cb9591bbfbf900ff92d2081d07c277fe0cbf11775061724b6f50918fa971b',
'.qrspi/task-journal/spec.md':'b2875cdba9c829988e5d3320633c838862e2de5e7debed4a6d7ba470ca987ff8',
'.qrspi/task-journal/design/task-journal-pinned-active.png':'7948d782ab5bb1b155cfb296abcc70ad685ffdca5a3a07a357af6fa59b238cd6',
'.qrspi/task-journal/plan.md':'25fb6f0ef7e58bac12565a665a65de7bf5736df137a48185d22e597e29cca03c',
'.qrspi/task-journal/plans/plan-phase-1.md':'792155963b833c93c474ed51e11c5fcb838585ce53cac738fedcbcab7603b0b4',
'.qrspi/task-journal/plans/plan-phase-2.md':'74829593700d9006b96c955b07b5acdfe12902f68f40ea6536043ad3d7a3ba6b',
'Assets/_Project/UI/README.md':'a87fa092e17bc433dbb3958e6d9b6dc849a2f886e5cd7ce50f254c997a0d3305',
'/private/tmp/solitude-task-journal-spec.md':'cf86cf85b24e6b6b77814f0b97cf6932bddee9331ae82653c4d1cfb0a6695dac'}
for name,digest in expected.items():
 data=(root/name).read_bytes()
 if name.endswith('plan.md') or '/plans/plan-phase-' in name:
  s=data.decode();s=re.sub(r'(?m)^(- )\[[ x~!]\]( Status marker.*)$',r'\1[ ]\2',s)
  if name.endswith('/plan.md'):
   lines=s.splitlines(keepends=True);status_index=None
   for i,line in enumerate(lines):
    if line.startswith('|'):
     cells=line.split('|')
     if 'Status' in [c.strip() for c in cells]:
      status_index=next(j for j,c in enumerate(cells) if c.strip()=='Status')
     elif status_index is not None and len(cells)>status_index:
      cells[status_index]=re.sub(r'\[[ x~!]\]','[ ]',cells[status_index]);lines[i]='|'.join(cells)
   s=''.join(lines)
  data=s.encode()
 actual=hashlib.sha256(data).hexdigest()
 assert actual==digest,(name,actual,digest)
print(json.dumps({'provenance':'PASS','approved_revisions':expected}))
