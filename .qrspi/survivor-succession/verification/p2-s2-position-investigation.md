# Awakening position precision investigation — S7/S12

Source base: 756d0dcc8526afb2d5ca1cd5423fd893504f8334 plus the unchanged ten-file p2-s2-attempt1 source snapshot. Diagnostics were added only to the disposable verification project. No product runtime code was changed by this investigation.

- First diagnostic: 20 successive empty-equipment awakenings, immediate/frame/physics positions all exact; probe test passed.
- Second diagnostic: instrumented the original assertion and ran the surrounding PlayMode suite (including the probe). 55 passed, 1 failed. The same original post-frame assertion reproduced the difference.
- Precise measurement: expected Transform (-0.89, -2.4, 0); actual rendered Transform (-0.890000045, -2.4, 0); Rigidbody (-0.89, -2.4, 0); distance 5.96046448E-08. Rigidbody interpolation is Interpolate.
- This is one binary32 representable increment in the x coordinate. The body and synchronous relocation are correct; the rendered Transform is rounded by interpolation.
- Human-authorized repair: preserve/add exact synchronous Transform and exact post-frame Rigidbody assertions; use <= 1E-06 distance for rendered Transform checks (initial post-frame placement and post-death frame). This is approximately 17 times the observed rounding increment and still rejects meaningful displacement. No runtime movement/awakening change.
- Updated Phase 2 normalized plan SHA-256: `b8f9778b7704f11bdfce91210e532ab6460123b54cb6426e6305da8b73a2d023`; only Step 2 files/tests expand for this narrow approved repair. Original plan digest and failed attempt remain preserved in earlier evidence. Spec, overview, other phases and scope are unchanged.

Diagnostic artifacts: Logs/SurvivorSuccession/p2-s2-position-investigation/probe.xml, probe.log, suite.xml, suite.log. Diagnostic checks do not qualify the repaired source; the full isolated gate still must pass.
