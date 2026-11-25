# Inventory action keycaps

Native uGUI inventory refinement in AtmosphericScrubberEncounter at1920×1080. E Equip, R Assign to hotbar and M Move use32×28px keycaps. Select/Navigate instruction block removed. Action widths184/260/184px retain18px labels. Same existing action flows, with shortcuts authored in the shared input asset and dispatched through UnityInputHost; keycap labels resolve the real keyboard bindings.

`journey.json`:9 focused checks pass using synthetic keyboard events through the actual shared input host: M move and real placement, E equipment destination and Back cancellation, R assignment, E active selection, removed instruction block, ignored commands while closed and reopen subscription behavior. Input focus settings restored afterward.

`default.png`, `full.png`, `action-focus.png`: actual1920×1080 Unity offscreen renders; sample stacks are transient review setup. No TMP overflow. Exactly one command receives the focus fill. Earlier compact evidence applies to unchanged layout and services. Headless rendering does not verify a normal Game-window or physical-controller session.

Editor left in Edit mode; no unrelated scene changes or commits made.
