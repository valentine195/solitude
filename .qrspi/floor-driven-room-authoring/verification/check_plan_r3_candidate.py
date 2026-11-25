from pathlib import Path
import hashlib, re, json
root = Path(__file__).resolve().parents[3]
f = root / '.qrspi/floor-driven-room-authoring'
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def normalize(s):
    s = re.sub(r'(?m)^(- )\[[ xX~!]\]( Status marker.*)$', r'\1[ ]\2', s)
    return ''.join(re.sub(r'\|\s*\[[ xX~!]\]\s*\|(\r?\n)?$', lambda m: '| [ ] |' + (m.group(1) or ''), line)
                   if re.match(r'^\| \d+[a-z]? \|', line) else line for line in s.splitlines(keepends=True))
assert digest(f / 'spec.md') == '5e259da45c328e87a404d2cbcc83da875d486a611823ef1760ae03865685cf71'
for name in ['spec.md', 'plan.md']:
    s = (f / name).read_text()
    pairs = re.findall(r'\| [^|]+ \| `([^`]+)` \| `([a-f0-9]{64})` \|', s) if name == 'spec.md' else re.findall(r'^- (?:Spec|Research|Selected approach): `([^`]+)` — SHA-256: `([a-f0-9]{64})`', s, re.M)
    assert len(pairs) == (4 if name == 'spec.md' else 3)
    for rel, expected in pairs: assert digest(root / rel) == expected, rel
manifest = (f / 'plan-revisions-r3.md').read_text()
bundle = hashlib.sha256()
entries = re.findall(r'^\| `([^`]+)` \| `([a-f0-9]{64})` \| `([a-f0-9]{64})` \|', manifest, re.M)
assert len(entries) == 5
for rel, raw, expected in entries:
    b = normalize((root / rel).read_text()).encode()
    assert hashlib.sha256(b).hexdigest() == expected, rel
    bundle.update(rel.encode() + b'\0' + b + b'\0')
assert bundle.hexdigest() == 'cf6677e80ed013e888b39a0722aa3edb3c2924075dd3fc0582c9603f06d01c72'
for rel, expected in json.loads((f / 'verification/plan-r3-preserved-completed-phases.json').read_text()).items():
    assert digest(root / rel) == expected, rel
baseline = json.loads((f / 'verification/phase-1-baseline.json').read_text())
for rel, expected in baseline['existing_modified_file_sha256'].items(): assert digest(root / rel) == expected, rel
plan = (f / 'plan.md').read_text()
rows = re.findall(r'^\| (\d+[a-z]?) \| ([^|]+) \| ([^|]+) \| ([^|]+) \| (\d+) \| (\[[ xX~!]\]) \|$', plan, re.M)
ids = [row[0] for row in rows]
assert ids == ['1', '2', '2a', '3'], ids
phases = {}
for phase, name, deps, description, count, status in rows:
    text = (f / 'plans' / ('plan-phase-' + phase + '.md')).read_text()
    declared = re.search(r'^- Depends on: (.+)$', text, re.M).group(1)
    assert declared == deps.strip(), phase
    dep_ids = [] if declared == 'none' else declared.split(', ')
    assert all(d in ids and ids.index(d) < ids.index(phase) for d in dep_ids), phase
    steps = re.split(r'(?m)^### Step (\d+):', text)[1:]
    assert [int(n) for n in steps[::2]] == list(range(1, int(count) + 1)), phase
    assert len(re.findall(r'^- \[[ xX~!]\] Status marker$', text, re.M)) == int(count)
    phase_steps = {}
    for n, body in zip(steps[::2], steps[1::2]):
        for label in ['Criteria', 'Files', 'Changes', 'Tests', 'Recovery', 'Risk']:
            assert re.search(r'^- ' + label + ':', body, re.M), (phase, n, label)
        declaration = re.search(r'^- Criteria: (.+)$', body, re.M).group(1)
        supported = set()
        for first, last in re.findall(r'S(\d+)(?:–S(\d+))?', declaration):
            supported.update(range(int(first), int(last or first) + 1))
        assert supported and supported <= set(range(1, 30)), (phase, n)
        phase_steps[int(n)] = supported
    phases[phase] = phase_steps
coverage = re.findall(r'^\| S(\d+) [^|]+ \| ([^|]+) \|$', plan, re.M)
assert [int(n) for n, _ in coverage] == list(range(1, 30))
for number, refs in coverage:
    parsed = re.findall(r'(\d+[a-z]?)\.(\d+)(?:–(\d+[a-z]?)\.(\d+))?', refs)
    assert parsed, number
    for phase, first, end_phase, last in parsed:
        assert not end_phase or end_phase == phase
        for step in range(int(first), int(last or first) + 1):
            assert int(number) in phases[phase][step], (number, phase, step)
assert sum(int(row[4]) for row in rows) == 17
print('Candidate Spec/Plan provenance, phase dependencies, all 29 criterion mappings, 17 step declarations, completed-phase bytes and unrelated modified files verified. Human plan approval is not inferred.')
