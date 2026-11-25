## QRSPI Plan: crew-backgrounds

## Inputs

- Spec: spec.md — SHA-256: dd8f5bb2d90028526e6c5dcee74e6a8bf40f16f36bfdd06ab033ad73d1b9f339
- Research: research.md — SHA-256: e08cb77a36b3857778f8b97eceb45a6c28c51709f663849199bae43daf974f06
- Selected approach: approach.md — SHA-256: e30b6eba20f650c4178362ef6c29ba739d19011dc9bfd27acc488be4a8236ac5

| Phase | Name | Depends On | Description | Steps | Status |
| --- | --- | --- | --- | --- | --- |
| 1 | Crew backgrounds playtest | none | Launch both backgrounds against each fault in one painted room. Play the standard and specialist repair paths through to a traversable return door. | 5 | [ ] |


## Reusable repair behavior

- A background supplies proficiency ranks. An interaction checks the skill and minimum rank for a particular clue or action. Background names belong to launch configuration.
- Repair controls share preview, eligibility, interruption, and completion. Author the clue, required rank, and part or tool on the control. Bind the control to a typed equipment operation. Use the existing interaction prompts and inventory commands.
- Equipment owns its fault and valid state transitions. A bypass, a consumed replacement, and a breaker reset have different effects. The scrubber's recovery clock and the room's lighting and door bindings consume committed state.
- Test reuse in a fixture with a second repair target and a different authored part. It must use the same control, gate, and completion code, apply the configured cost, and leave the first target's independent state intact. The Technician's rig also exercises the same consuming operation as the standard linkage repair with different requirements.

## Criterion coverage

| Step | Criteria |
| --- | --- |
| 1. Launch and reset the shared room | S1, S2, S11, S14 |
| 2. Restore airflow through the standard routes | S2, S3, S4, S7, S9, S10 |
| 3. Jump the circuit and recover the jumper | S2, S5, S6, S9, S10, S11 |
| 4. Rig the broken linkage | S2, S8, S9, S10 |
| 5. Verify the authored room and retained demos | S1–S14 across the collected phase evidence. Runs the integrated journeys for S1–S12 and confirms S13 and S14 on the final snapshot. |

The component checks in Steps 1–4 establish the outcome and failure contracts. The integrated checks in Step 5 cover their behavior through the saved scene, normal input, and subsequent launches. Record scope and snapshot for each check. Reuse matching evidence where later steps leave its source and dependencies intact.
