# Opening asset audit — 2026-10-02

Inspected actual running Unity 6000.6.4f1 project, Wakeup scene, source, sprite imports, and gameplay-scale capture before changes. URP 2D is active; correct URP PixelPerfectCamera already exists, 32 PPU, 640×360 reference, camera MSAA disabled. Wakeup reports no missing scripts and SceneBindings.Validate passes. Working tree contains extensive pre-existing gameplay changes.

| Status | Asset/system | Evidence and decision |
|---|---|---|
| KEEP | SolitudeStart atlas and tile assets | Native 32px grid, ivory/graphite/steel palette, north faces and cap boundaries. Prefer quiet floor_composite and plain_bulkhead; reserve busy grates for service accents. |
| KEEP | Nine-frame sliding door | 64px frame, 32 PPU, existing SolitudeSlidingDoor animation/blocker logic. Reuse intact. |
| REPLACE visual; KEEP gameplay | Caretaker and player prefab | User confirmed placeholder is replaceable. Replace taller 28×78 visual with 32×48 four-direction caretaker, retain movement, input and inventory; fit foot collider for new silhouette. |
| KEEP | SceneBindings / GameCompositionRoot | Explicit inventory, modal, pickup, save and input composition. Copy composed scene to preserve wiring. |
| KEEP | Locker / WorldPickup / item definitions | Existing interactions and persistence identities. Change presentation and positions only in review scene. |
| REPLACE in review scene | Wakeup floor/wall presentation | Multiple older tile families, large visible floor seams and mixed visual scales. Use existing canonical 32px atlas instead. |
| REPLACE in review scene | Locker presentation | Pack 01 Locker is 26×74 at 100 PPU with 1.43 transform scale; inconsistent with ship kit. Preserve container/interaction. |
| REPLACE in review scene | Battery presentation | 32×32 at 100 PPU and scale2; replace world renderer only. Preserve inventory icon and pickup definition for now. |
| REPLACE in review scene | Cryopod presentation | Aseprite slice112×233 at128 PPU, scale2. New matched compact prop required. |
| KEEP as source, omit review | Other vendor packs, O2 pickup, legacy Door.cs | Retain files; oxygen is outside opening art requirements. Legacy door does not open on initial Interact in current source; use existing working sliding door instead. |
| MISSING | Matching pod, locker states, battery, intercom, personal shelf | Generate one small coordinated prop-family atlas, record prompt, import and inspect. |
| MISSING | Battery-powered door link / AI fragment | Scene-local interaction bridge around existing inventory and door; no core system refactor. Prototype state resets with scene. |
| MISSING | Art contract and repeatable validation | This direction, JSON spec, import checks and review scene. |
| MISSING | Original board images | Thread read returned text only and empty attachments. Written direction is authoritative; image matching cannot be claimed. |

No existing user changes have been reverted. Wakeup and its source assets remain the baseline; the art milestone uses a sibling review scene.
