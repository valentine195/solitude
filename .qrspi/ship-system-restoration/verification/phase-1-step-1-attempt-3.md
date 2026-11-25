# Phase 1 Step 1 — attempt 3

Status: complete. Commit: `34b63e2a6859ddf1fc6608bec37d8a7069609840`. Working tree clean after commit.

Spec SHA-256: 3396d3146f9a39644b41dafcb38afb8321933f670d07f1fc36288b0a2c97479f
Overview normalized SHA-256: 53cc6bfe3e5e1c80b588d794a9012f3ec5c36ef6b05a41ddfa76bb285d9fc66b
Phase normalized SHA-256: 0e74806e1b54e8cf4e902beef04d801d34f442eaf0de0527357fe63fb0bbc97c

## Verification

Command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-restoration-step1 --output /private/tmp/solitude-restoration-step1-attempt3`

Full gate: **Passed**, macOS ARM64. 56 pure Domain/Application tests; 108 EditMode tests; 6 PlayMode tests; zero failures. Authoring validation, verification and shipping builds, collection/restart/new-save scenarios, malformed/missing/incompatible saves, and interrupted write/recovery scenarios passed. Tests used the isolated project and disposable saves. No partial --skip-build run.

- S1–S4: restoration tests confirm independent states, inventory-only requirement, one-unit consumption, committed state visible to inventory observers, rejected/busy/unavailable/repeated attempts, reentrancy protection, pickup and locker-transfer compatibility. Existing stale-address removal regression passes.
- S5 (Step 1 scope): subscriber exceptions do not block later consumers; state remains through enable cycles. Door/terminal late-enable coverage belongs to later steps.
- S7 (junction scope): focused prompt refresh test passes after explicitly invoking runtime lifecycle methods in EditMode. Initial requirement, restored text without another focus event, disable cleanup, invalid configuration and released bindings pass. Terminal/visual end-to-end coverage remains later work.
- S9 (shared regression): full existing gate passes; source Git index remained unchanged by verification. Disposable tracked Assets index matched source before commit. Authoring reports no findings.

All changed/new Assets bytes were compared with the tested disposable project after verification and after commit; matched. Input revisions matched approved values. No manual visual demo check or adversarial review has occurred. Steps 2–5 remain incomplete.

## Reports

Original logs and builds: `/private/tmp/solitude-restoration-step1-attempt3/`.
Summary/XML copies: `verification/phase-1-step-1-attempt-3-results/`.

## Source snapshot

- `Assets/_Project/Core/Composition/SceneBindings.cs`: `133c8f54a9b38274a3a5553622f71bda131fd6ec58c4b8dc43e112153a3ded1c`
- `Assets/_Project/Domain/ContainerCommandService.cs`: `dc3e315b36f660137b3f7047eff80e602c5cac92df4004937664b6b149d7aea4`
- `Assets/_Project/Domain/Restoration.meta`: `8711fd2a11ef258b1f7687e2d5a6a40747585685b33d51983e2a40cce2668b03`
- `Assets/_Project/Domain/Restoration/RestorableSystemState.cs`: `987047ca254d8a85de620dd97a8972dd84c02b36ed2c4f37615c7e95370e5e14`
- `Assets/_Project/Domain/Restoration/RestorableSystemState.cs.meta`: `06e2792d245151c0f6877beba36659613400a7ebd0041623e7853bcc6e7170b6`
- `Assets/_Project/Features/World/Restoration.meta`: `476fc01a17200e387b30a7f2a9bbac899f3d17de4559ec77ac21b0f5e387946c`
- `Assets/_Project/Features/World/Restoration/RestorableSystem.cs`: `97c901c368a9e07d0c26eed4722b6f9c6ff93d59e69e8ece1fdfcd3cdbcb40a2`
- `Assets/_Project/Features/World/Restoration/RestorableSystem.cs.meta`: `174a4510996a0f28bb66b2ec08e08f065c4a869140c28c3005bbf4c9decc12e4`
- `Assets/_Project/Features/World/Restoration/RestorationJunction.cs`: `614e9da21baf3f3e840dcab0b4579f1e6e3064e641a00c4ec6c449364f7b2dd4`
- `Assets/_Project/Features/World/Restoration/RestorationJunction.cs.meta`: `cf65de5078a55f1fe62d0734d15baf3bace91f86c505eaa06d444219e5bd8c77`
- `Assets/_Project/Systems/Interaction/UI/Controllers/UIController.cs`: `b34c9dec0f2fb818da5c94b0b577d51a1ee05c811e428ac774af31aa6e1bc778`
- `Assets/_Project/Tests/Domain/RestorationTests.cs`: `e61a2ad78fe580d68bb066575575d5cfadd1b614ed940a1a74bc2d984b8c5001`
- `Assets/_Project/Tests/Domain/RestorationTests.cs.meta`: `bea075613a2714178da71e6e8bc97f8abf61445cd6d960ce7565a709372c97dc`
- `Assets/_Project/Tests/Editor/RestorationJunctionTests.cs`: `cc10e4e33b3c60cea3fb692b1320b6343cb12cf9d82615a133953343460c37d1`
- `Assets/_Project/Tests/Editor/RestorationJunctionTests.cs.meta`: `49ecd4d05e72e4f72f24b1bc91fa2a8c8b9e1132a3c6ad5882990ce93f7025f6`
- `Tools/Verification/verify.py`: `db83b85848a3b467565655cbd4c3c04fde15f378666f616fe0624d46f32f9f20`
