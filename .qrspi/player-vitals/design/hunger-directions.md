# Hunger presentation exploration

Design-only response to the request for clearer hunger presentation. No code, approved spec, or plan changes.

Reference: native Wakeup screenshot `/private/tmp/solitude-vitals-native-visuals-s3tuzpx2/captures/direct-frame-hungry-1280.png`; SOLITUDE UI guide.

Display order is authoritative:

1. Compact reserve — `/Users/jeremyvalentine/.codex/generated_images/01a1319e-3865-75d1-917e-1603a1671b1a/exec-c97b3676-d02a-40df-a179-196e27448c3c.png`
2. Nutrition status — `/Users/jeremyvalentine/.codex/generated_images/01a1319e-3865-75d1-917e-1603a1671b1a/exec-2700d641-c842-4563-8cd0-ea11af3c7666.png`
3. Recovery reserve — `/Users/jeremyvalentine/.codex/generated_images/01a1319e-3865-75d1-917e-1603a1671b1a/exec-10ca6331-66f5-4913-9ab9-a616c6811fa5.png`

## Shared intended behavior

- Label player reserve NUTRITION rather than FOOD, which can imply inventory stock.
- Keep a continuous reserve indicator and the configured fed cutoff tick (20% default), inclusive fed boundary.
- Off-white/muted normal presentation; amber warning plus HUNGRY below cutoff. O2 green remains operational.
- Keep health segment markers and distinct recovery status. Nutrition constrains passive recovery; no starvation damage or new mechanics.
- Views remain authored Unity prefabs, bound to current vital/controller state.

These are enlarged ImageGen concepts, not implemented layouts or verification evidence. Generated dimensions, percentages, cutoff tick positions, and bar geometry are illustrative; selected production layout must use exact normalized values, 4/3/2 configurable health markers, and configured threshold. Native gameplay scale, other recovery reasons, and menu/awakening transitions require checks after implementation. Selecting a design does not approve a different gameplay rule.

## User-selected treatment

User feedback: “I really like the HUNGRY with ‘recovery paused’ underneath”.

Selected element: amber HUNGRY heading with smaller muted Recovery paused directly beneath. This selects the status treatment shared by directions 2 and 3, not an unambiguous entire numbered layout. Refine a single preview around that treatment before production UI changes. A subordinate continuous nutrition reserve and configured cutoff remain required by S14; no gameplay/spec revision is implied.

Status caveat for implementation: use Recovery paused under HUNGRY only when hunger is the effective recovery blocker. At the segment ceiling, dead, unavailable, or policy-paused states must retain the approved status precedence; an independent hunger condition must not fabricate a recovery block.
