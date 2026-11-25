# Phase 1 Step 4 — attempt 1

Status: complete. Commit: `902b646b027daf3438e1f423c63c6f45b09476b2`.
Spec SHA-256: 3396d3146f9a39644b41dafcb38afb8321933f670d07f1fc36288b0a2c97479f
Overview normalized SHA-256: 53cc6bfe3e5e1c80b588d794a9012f3ec5c36ef6b05a41ddfa76bb285d9fc66b
Phase normalized SHA-256: 0e74806e1b54e8cf4e902beef04d801d34f442eaf0de0527357fe63fb0bbc97c

## Changes and evidence

Separate Unity-API-authored demo reuses opening assets, player, UI, batteries, locker and sliding door. Two independently configured junction/system/terminal sets demonstrate reuse. Opt-in memory persistence is configured before load; reload resets the retained session before rebinding. Production/demo mixing is rejected. Production scene and build list remain unchanged.

Command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-restoration-step1 --output /private/tmp/solitude-restoration-step4-attempt1`

Expected: complete gate passes with reset/save isolation and reusable scene checks. Actual: Passed; 56 pure, 108 EditMode, 16 PlayMode tests; zero failures. Authoring validation, verification/shipping builds and save/restart/recovery scenarios passed.

- S1–S3: lifecycle test diagnoses missing battery, collects and installs one, restores first system while second remains Offline, then restores second independently.
- S6–S7: demo test opens powered door and reads configured terminal content. Existing focused tests preserve blocked-door and terminal behavior.
- S8: reload retains root but resets inventory, pickup availability, systems and terminals; collection/install can repeat. Memory store receives persistence operations; invalid normal-save sentinel stays unchanged. Production-to-demo entry is rejected without changing production session or saved bytes.
- S9: second set works through scene configuration; shared-code full regression gate passes. Existing locker and transfer components retained.

Live Editor inspection: demo starts Ready with disposable session and two systems. Direct interaction calls restore primary circuit and open door; before/after screenshots show unpowered/powered terminal and closed/open door, with independent terminal still dark. Terminal returns placeholder content through existing feedback. See `demo-before.png` and `demo-after.png`. Player was positioned by Editor API for inspection: this is not a manual keyboard-navigation or collider-passage test. Detailed integrated interaction/visual verification remains Step 5. Editor Play mode stopped and original opening scene restored. No adversarial review yet.

Approved input/provenance hashes rechecked; all 14 changed source files matched tested workspace byte-for-byte before commit. `git diff --check` passed.

## Source snapshot

- `Assets/_Project/Editor/RestorationDemoBuilder.cs`: `0c1d19016259faddb01feb60395c5e114cd59d09f6567e628658567ca4f39978`
- `Assets/_Project/Editor/RestorationDemoBuilder.cs.meta`: `56e771ab88055f5a2d27684986941f6e89d2ed242e18fc1464ebe1914c16956c`
- `Assets/_Project/Features/World/Restoration/Demo.meta`: `1a240bf36f4daffadb2a6d078d573afd30a2fc573198eb29c8df547601fe3ac5`
- `Assets/_Project/Features/World/Restoration/Demo/README.md`: `efeb56187d8ffc304a50d1e8070763d23f23d868c979bec92769f6243e8739f5`
- `Assets/_Project/Features/World/Restoration/Demo/README.md.meta`: `d7f70a8b28c45f37babe604943eda5a285991483d508e99caed8eb77fff7848d`
- `Assets/_Project/Features/World/Restoration/Demo/RestorationDemoSession.cs`: `f86f3a251fc98a9411d8e82d5867c33141016aef7142cb4f15de449c70b2cbee`
- `Assets/_Project/Features/World/Restoration/Demo/RestorationDemoSession.cs.meta`: `e13f5e80a9d3ca355bed87362af1362f4319bb6b02c310c6fdeeac9d04325241`
- `Assets/_Project/Scenes.meta`: `8a9bd1849d7588be2f21019c696fd508fd2faa890f32c0789ae1f9177b74fc98`
- `Assets/_Project/Scenes/Demos.meta`: `1155db251b075253f54cc66fd61aabe86febf66595eba276d716ced47f0e4fd9`
- `Assets/_Project/Scenes/Demos/ShipSystemRestoration.unity`: `0c1b967a31b5f18957c07befdca8fbfb92d72341fec6d7518d27e199575cb296`
- `Assets/_Project/Scenes/Demos/ShipSystemRestoration.unity.meta`: `90e317cc8936a00ca2d3902032326950688e187f04b24bb40ac2acf8d63a4123`
- `Assets/_Project/Tests/PlayMode/RestorationDemoLifecycleTests.cs`: `378220dbe499bba5a3c4a97b73f814c0a64e753d5a9a2c80661cbc14273a8d8e`
- `Assets/_Project/Tests/PlayMode/RestorationDemoLifecycleTests.cs.meta`: `6bda7f7e333733d2dd63ec0846ee3beb4e4ba3e9559a62b7f8a1d7e8d86558bb`
- `Assets/_Project/Core/Composition/GameCompositionRoot.cs`: `35eecfb1be44d300972c9d53e073a6b30410d62557e2dbd781b9df659ab5f695`

## Reports

Summary/XML and validation copies: `phase-1-step-4-attempt-1-results/`. Full original logs/builds: `/private/tmp/solitude-restoration-step4-attempt1/`.
