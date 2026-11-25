## Verdict: FAIL

Quick source review found two Phase 1 spec divergences: keyboard focus can leave the visible scroll area, and a first-render failure can leave the journal's pause and primary-screen leases held.

## Scope

User explicitly requested a quick review in the implementation chat and overrode separate-reviewer isolation. This is a bounded in-chat source review, not an independent exhaustive review or a fresh runtime qualification. No code changes, new builds, or test runs were performed. Optional supplemental reviewers and explainer were omitted for the requested quick scope.

Phase 1, steps 1–5, committed changes only. Base: 27cd4be3ce7773674f0ce8b1afe2266dec432c59. HEAD: 53b403ee5c732323df0337a481e3501f9a0ad3af.
Exact diff: `git diff 27cd4be3ce7773674f0ce8b1afe2266dec432c59 53b403ee5c732323df0337a481e3501f9a0ad3af -- Assets/_Project`.
Diff SHA-256: 52a7be9ad73220f4b1608f0305639accdc7732231e3c05bbe0bfcc17ce50c4da. QRSPI scaffolding excluded.
Spec `.qrspi/task-journal/spec.md`: 50fa6addebd57b43f7617b1a266a6ba13f17e6b1f7e05c55b73df48fe0af35a9.
Normalized overview: e5b2ee3a15cc48d2506806c8ded0d7364b877f9b27e195749ecf7d750f2597ab.
Normalized Phase 1: 64225dbf598968e3d75c39ded978e64f62699ed48fcf0f144d03977883e40096.
Provenance validator passed. Changed source matches committed snapshot and was checked unchanged before publication. Untracked files are unrelated sprite-audit outputs under work/Reviews; no untracked product source was found. Helper supplied `reviews/phase-1.md` through next-file review --phase 1; status already points to Phase 2's unstarted plan and was not used to widen review scope.

## Findings

| Severity | Blocking | Category | Location | Description | Suggested Fix |
|---|---|---|---|---|---|
| MEDIUM | yes | Spec conformance | Assets/_Project/Systems/Tasks/UI/TaskJournalView.cs:115 | F1 / S8, S17: Keyboard selection preserves the existing scroll offset even when the selected row is outside the masked viewport. | Scroll the focused row into view after layout, preserving offsets on ordinary updates/reopen when focus remains visible. Cover a list longer than the viewport. |
| MEDIUM | yes | Error handling | Assets/_Project/Application/Tasks/TaskJournalPresenter.cs:136 | F2 / S7: Rendering errors during Show are logged and swallowed, so the screen coordinator considers the failed open successful and keeps both leases. | Propagate initial-render failure to the coordinator or explicitly close on presentation failure; keep source writes isolated. Add a concrete view failure test. |

## Evidence

### F1 / S8, S17
Trigger: publish enough visible Active tasks to overflow the list, open the journal, and navigate down into an offscreen row. Generic task count is unbounded; this is reachable without adding scrubber story branches.
Expected: keyboard navigation maintains visible focus and usable scrolling.
Source trace: TaskJournalRowView.cs:20 forwards OnSelect to Activate; TaskJournalView.cs:55 remembers the current scroll offset and selects the task; TaskJournalPresenter.cs:114–117 changes identity and renders without adjusting scrolling; TaskJournalView.cs:115 reapplies the old normalized position. RestoreFocus at lines 79–88 also only changes EventSystem selection. Builder's JScroll uses a plain masked ScrollRect, with no scroll-to-selection adapter. Unity's local ScrollRect implements dragging/scroll events, not selection handling; Selectable.Navigate only transfers selection to an active Selectable. Masking does not deactivate the row. Consequently selection/details move while its visual marker remains clipped. Long details can likewise leave the Pin control outside the visible region when focused. Existing presenter tests exercise stored numeric offsets, and PlayMode tests exercise one/two rows; neither establishes overflow navigation.
Check: source inspection of the above complete view/presenter paths, builder JScroll, local uGUI ScrollRect and Selectable declarations/navigation. No live reproduction claimed.

### F2 / S7
Trigger: after initialization, an authored text endpoint (for example the title TMP component) becomes unavailable before opening. Initialization validates once; Show does not revalidate endpoints. This can occur during runtime UI teardown while the root view remains enabled.
Expected: failed open releases the journal's pause and primary-screen ownership.
Source trace: TaskJournalScreenCoordinator.cs:25–29 acquires both leases and invokes Show. TaskJournalView.cs:59 activates the panel and calls presenter.SetVisible(true). Render's title access at TaskJournalView.cs:105 throws if that component was destroyed. TaskJournalPresenter.cs:133–141 catches this and only invokes PresentationError; the actual view subscribes Debug.LogException at TaskJournalView.cs:47. Control returns to Show, whose RestoreFocus succeeds using the still-valid row/tab controls. Coordinator returns Opened at line 31, retaining both leases. J/Escape can still close; this is failed-open cleanup, not an unrecoverable lock. The isolated coordinator test uses a display that throws directly from Show, bypassing the actual swallowing presenter, so it does not catch this integration gap.
Check: direct source trace through concrete Show, presenter catch, error subscription, and coordinator acquire/catch/return. No new runtime test claimed.

## Open Questions

No additional defects asserted. This quick pass does not re-establish every Phase 1 acceptance criterion or re-run existing runtime/visual evidence. The two source traces are sufficient for a failing checkpoint; all unexamined behavior remains unqualified by this review.

## Supplemental Reviews

None provided. Prior implementation test claims were not treated as newly executed review evidence.

## Spec Conformance

DIVERGED S7 — first-render failure does not release ownership (F2).
DIVERGED S8 — keyboard focus may leave visible scroll region (F1).
DIVERGED S17 — focus visibility fails for overflow navigation (F1); other visual states not requalified.
NOT IN SCOPE S9 — shared notices belong to Phase 2.
S1–S6, S10–S16 — registry, projection, composition/input diff and presenter paths inspected selectively; no further confirmed finding, but no exhaustive PRESENT assertion in this user-requested quick review. Notice portions of S1/S2/S11 remain Phase 2 scope.

## Source snapshot

All 47 changed files are fingerprinted below; hashing does not claim every prefab/generated line received exhaustive review. Source reads focused on TaskJournal, TaskTypes/TaskContracts, TaskJournalPresenter, TaskJournalScreenCoordinator, TaskJournalView/RowView, ScrubberTaskSource, composition/input/access diffs, builder journal authoring, and presenter/coordinator/slice tests. Local uGUI ScrollRect and Selectable were consulted for navigation behavior.

```json
{
  "Assets/_Project/Application/Tasks.meta": "cc34fff7f06ebe93c8b57c47449984885d30186432669848cd4c440e2f62cff8",
  "Assets/_Project/Application/Tasks/TaskJournalPresenter.cs": "1443d451cdbbd9003f6489a2157522852032f27d73e781a5ef741a8b94da047c",
  "Assets/_Project/Application/Tasks/TaskJournalPresenter.cs.meta": "28ba286625c85fc1b7f00cecbde011f5ed51d2b24394156035d4168cec87317b",
  "Assets/_Project/Application/Tasks/TaskJournalScreenCoordinator.cs": "c82baa1639f18198cc410d02cabd06131bcfaa9920e5ae718ebf143a824c81e6",
  "Assets/_Project/Application/Tasks/TaskJournalScreenCoordinator.cs.meta": "b9a5da577b1820b9747bf4d08b69222e1ec18130a91c2136f4ed9bd1421693be",
  "Assets/_Project/Core/Composition/GameCompositionRoot.cs": "2a1f357cde756c8501014c0584082870a4fb1051f71734477712cb41a4b1f032",
  "Assets/_Project/Core/Composition/SceneBindings.cs": "3ebf94b734a003a5b742f57efd5db54a729e993e32148e5b238f3f49a3c318bb",
  "Assets/_Project/Core/Input/SOLITUDE_InputActions.cs": "7d5709714cffd2df0b7227f7fa39535f1b358b0cc95606629e180a88f4f904e4",
  "Assets/_Project/Core/Input/SOLITUDE_InputActions.inputactions": "1d1c793f974f7c15f7053107ef51a4fce83b5fe42ce5fa6ecf339109bb802a3a",
  "Assets/_Project/Core/Input/UnityInputHost.cs": "1afba5cbafde1acf1c8bd48a36e589bb02ea37f2ee4ec1a09690e02a31781160",
  "Assets/_Project/Domain/Tasks.meta": "393dade0991f4389ea9129085244beff206ca6548bbaf55a3e7e57bcb3a4bad2",
  "Assets/_Project/Domain/Tasks/TaskContracts.cs": "7b195c6a33ff249e411e26134a1feef65735b106b9c0e37c5215fbd1246a2f08",
  "Assets/_Project/Domain/Tasks/TaskContracts.cs.meta": "b13c39cc91d75ab6cdd4dc39114c70d8ed232482682860a42703011ad2900b9e",
  "Assets/_Project/Domain/Tasks/TaskJournal.cs": "d1014ab71641f6b672fd24cd9eba7727bd0d8b145b92409a6f3c62b2b0abf2b7",
  "Assets/_Project/Domain/Tasks/TaskJournal.cs.meta": "4670168760c6f37b5ffb826c8b4a8b775253f19f4e840e5a0cec47d9d7325f00",
  "Assets/_Project/Domain/Tasks/TaskTypes.cs": "5dbadce709afec8e5a50779835022391f54c91d31e0ef324b0fea421febf25d9",
  "Assets/_Project/Domain/Tasks/TaskTypes.cs.meta": "04870a1e9802b903f0c6a786304413cf522cc664da02e6c9afb098773031c3af",
  "Assets/_Project/Editor/ScrubberEncounterBuilder.cs": "b85ed745b6dd4adf4d3dbc6be1052d863be7db6caa03c5cda6d3fa7857f49ab7",
  "Assets/_Project/Features/World/ScrubberEncounter/ScrubberAccess.cs": "95862e4eea4d21e3bfa6ace5ed15555daecac7e7b50e5c6a8bf06301cf93839b",
  "Assets/_Project/Features/World/ScrubberEncounter/ScrubberTaskSource.cs": "3c26247a6f4180f205a4b156529053a976a7e8985ddc2e2e97fd65053563721b",
  "Assets/_Project/Features/World/ScrubberEncounter/ScrubberTaskSource.cs.meta": "7a79072af8a8600eaeccdc93e3147faa58f287bf08fbbe8f71c3712303cfc62c",
  "Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity": "3638a5de0d1162c392edf395edfba82b5fdabc290c80370d75489e272e61d0d3",
  "Assets/_Project/Systems/Tasks.meta": "809ae489b091a6f9fa8e2994613ee5bf5290f557ef7a2802af010d8e2bb78b3c",
  "Assets/_Project/Systems/Tasks/UI.meta": "e7932bea1843042c19b60322565dfe40e9e8118dfaaf7774d492e66f6e6cf015",
  "Assets/_Project/Systems/Tasks/UI/TaskJournal.prefab": "3f0f75e4039d6f309192908d291e0b1f5521250067655e723af559c298cc4e9d",
  "Assets/_Project/Systems/Tasks/UI/TaskJournal.prefab.meta": "06b44d4afe93f29e78a47056dfcd10707d7e23468ca6be398c37e097cf928b61",
  "Assets/_Project/Systems/Tasks/UI/TaskJournalRow.prefab": "28860f75957e3175ad0b3f7ccfa2e6790a550d36bc877d60b6568b4002a1fcd8",
  "Assets/_Project/Systems/Tasks/UI/TaskJournalRow.prefab.meta": "d94bda8cea91570c208148305b251b54a258b52f38f297dafed6b4f6c3756fc9",
  "Assets/_Project/Systems/Tasks/UI/TaskJournalRowView.cs": "23fb212043b7c6abd00e63aa393aa0a78658bde89992f8b47b9b2e1e0d68ae01",
  "Assets/_Project/Systems/Tasks/UI/TaskJournalRowView.cs.meta": "7037cdbd396d02356b028919e20a8d07c8d1700106eb0fe4546ad129fb5e6b09",
  "Assets/_Project/Systems/Tasks/UI/TaskJournalView.cs": "c4bbccc7aa1120400cc27f4a713244be87eb9d799aadfe891b0111000ed116c3",
  "Assets/_Project/Systems/Tasks/UI/TaskJournalView.cs.meta": "b1013d15be1d7bc2397b056290d2308b35081643044cac0beb478f9346972ee5",
  "Assets/_Project/Tests/Application/TaskJournalPresenterTests.cs": "640aa983669d72a77bbb361dae5556490896b0c4070ed6a4b06b9cc5418a635a",
  "Assets/_Project/Tests/Application/TaskJournalPresenterTests.cs.meta": "6b9f9ce6c9f9bc340db0a0b29c05a30b0217d08021c1060d4f8bdcb7b48f49f2",
  "Assets/_Project/Tests/Application/TaskJournalScreenCoordinatorTests.cs": "71996b2226d881529c88ac62ac339dc400421628a579a6c8d48f44f9603ac071",
  "Assets/_Project/Tests/Application/TaskJournalScreenCoordinatorTests.cs.meta": "c9037a2e908ecd465b5e323368c68ef42dd5b64a9202b5d565ff9c4e84ceef9b",
  "Assets/_Project/Tests/Domain/TaskJournalTests.cs": "660f5dc457973d4734469b92539526ed0532de8dd3302f5cf7bf3d9927722e34",
  "Assets/_Project/Tests/Domain/TaskJournalTests.cs.meta": "417446937270533626df9c01e06041d6aa395e42bbdaacba21316b4bfc10a70d",
  "Assets/_Project/Tests/Editor/ScrubberEncounterAuthoringTests.cs": "1547b0e0b7c74c3d7603d68a12403581f28d5de6d13091802922bec2c4880977",
  "Assets/_Project/Tests/Editor/ScrubberTaskSourceTests.cs": "528e879b0735d5e25533fd8f4c0f993a6fbf9e543ae2585a48a2dab150e5cb78",
  "Assets/_Project/Tests/Editor/ScrubberTaskSourceTests.cs.meta": "c763ed7c59df4403a134b01894262f665474b48c42d68950618bb56b06d53ef7",
  "Assets/_Project/Tests/Editor/TaskJournalCompositionTests.cs": "7ebe033098744c876fa3c4e013c8f54dde06d83722becd55476f3f9bf05d7b5e",
  "Assets/_Project/Tests/Editor/TaskJournalCompositionTests.cs.meta": "d1ad2c66db111197dce892a84a6b296fca3b10c8d63343a1bd3c414f66699c66",
  "Assets/_Project/Tests/Editor/TaskJournalInputTests.cs": "0c1a2020771b036cd7bc984f0c52846b36ab212eddc6ac14c58d054b0bc63c00",
  "Assets/_Project/Tests/Editor/TaskJournalInputTests.cs.meta": "3a2291c83070e3993dce172ec5f00160fbba56712292c31b1f7bfc2eea5c065e",
  "Assets/_Project/Tests/PlayMode/TaskJournalSliceTests.cs": "9d3780d64fc469f8033ecb4a597ec0e3ec0a74ce617781481f54855d0115914e",
  "Assets/_Project/Tests/PlayMode/TaskJournalSliceTests.cs.meta": "899f7e2744f0e3e632a8ed84cd158bbd8a50f373c18257550d39f85077ddc2ea"
}
```

Dependency Library/PackageCache/com.unity.ugui@0727b404f3a0/Runtime/UGUI/UI/Core/ScrollRect.cs: 313f35cd443c4abd7f20d24df1a1b6c7aee795d4fbde7d4c72376787e9af2ee9

Dependency Library/PackageCache/com.unity.ugui@0727b404f3a0/Runtime/UGUI/UI/Core/Selectable.cs: 350695da22cbbfaa294acb193245b270dc858a64a3678c0f265fde18f35dc1a1
