# Phase 1 Step 4 — full gate attempt 2 (failed)

Spec SHA-256: bd993d9564af62e92a8ccc723b89b42216216dab1556f4935c31d1a72bda43d5
Base commit: 22308b960ac6977ad656f01e306ad981fce36f25
Command: ["python3", "Tools/Verification/verify.py", "--workspace", "/private/tmp/solitude-vitals-p1-s4-gate-project-8dy_1doa", "--output", "/private/tmp/solitude-vitals-p1-s4-gate2-0r6qf97r"]
Expected: complete isolated verification gate passes.
Actual: pure tests 162 passed, EditMode 265 passed; PlayMode 97 passed, 1 failed, 0 skipped. Gate results.json Failed; builds/player scenarios were not run in this attempt.

Failure: PlayerVitalsSliceTests.HungryPauseMaximumChangeAndPrimaryScreenUseActualSceneBindings compared two float normalization paths exactly: expected 0.150006592, actual 0.150006607. Current health and hunger values were unchanged; this is HUD float rounding at 1.5e-8. The asserted visual fill comparison was corrected to tolerance 1e-6 (0.00026 pixels on a 260-pixel authored bar), with exact unchanged-current assertions retained. The verification-only capture driver also gained runInBackground=true so unattended native screenshots can finish while focus is elsewhere. Subsequent full-gate evidence is separate; no commit/completion marker from this failed attempt.

Full record: /private/tmp/solitude-vitals-p1-s4-gate2-0r6qf97r/results.json; source snapshot /private/tmp/solitude-vitals-p1-s4-gate2-0r6qf97r/source-digests.json; XML /private/tmp/solitude-vitals-p1-s4-gate2-0r6qf97r/playmode.xml. Existing full gate attempt 1 had passed before adding the native capture driver; that earlier source revision is not treated as qualification of the subsequent change. Native capture code/assembly is not a shipping feature. The user's continuing direction was applied to the narrow assertion and verification-environment corrections, without changing production rules or weakening exact authoritative-value tests.
