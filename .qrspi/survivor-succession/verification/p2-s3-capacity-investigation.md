# Phase 2 Step 3: recovery capacity blocker

Criteria: S4, S5. Spec SHA-256: f2253ab3429d7c5a34c8e9275f9efac61be9e3f4c30781393ad7d86e68ba85a4.

Production HEAD: 94dee5d3994cf958d37e02efb475cdb0871fba09. Step 3 integration changes remain uncommitted and unqualified.

Inspected the passing Step 2 disposable project at /private/tmp/solitude-succession-p2-s2-gate, with a disposable save override. Seeded a 33-slot recovery owner (24 inventory plus 9 hotbar), opened it using PlayerInteractor.TryInteract, and inspected its authored UI after Canvas.ForceUpdateCanvases.

Expected: every possible occupied recovery slot is reachable through the ordinary transfer screen.
Actual: 33 instantiated target slots, zero ScrollRect components, last slot screen-space Y bounds -73.43 to -9.43, entirely below the screen. Canvas reference rect is 1920x1080; maximized Game view reported 3024x1720. Thus full equipment recovery is not established. Command output: Logs/SurvivorSuccession/p2-s3-capacity-investigation/geometry.json. Diagnostic setup: setup.cs in the same directory.

Capture tools produced gameplay frames without the open modal; these captures are inconclusive and are not visual-pass evidence. The blocker is based on active modal layout geometry and lack of scrolling, not those captures.

User's earlier title-wrapping deferral remains intact. This is a separate functional capacity issue. Proposed amendment: add a bounded scrolling viewport to the existing target panel, preserve current art/title styling and locker APIs, and test reachability/transfer of the final slot at full recovery capacity. Requires plan scope to include the authored transfer prefab and focused UI validation. Await human approval before changing the plan or UI.

Source digests at investigation:
- `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs`: `6ea349c608e40951a365a6fe33acdfc84468f3e19b2a58b1d4b77d8d9755d698`
- `Assets/_Project/Scenes/Wakeup.unity`: `6d542c140c55f1455ea2f1a8eb5d33500a56a0d955927eeeec4c3f4689c00159`
- `Assets/_Project/Tests/PlayMode/SurvivorSuccessionSliceTests.cs`: `bb3d5a29edecaaa55a4e66d92bb6b2dba326349a0e3916fed7e573734cbc034a`
- `Assets/_Project/Systems/Containers/Views/ContainerView.cs`: `6f30e8852a036d5b2e76940cab5dc654544040a909e725f50ff5e9e418d2df2e`
