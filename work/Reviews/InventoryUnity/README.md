# Unity inventory evidence

See `Docs/UI/InventoryImplementation.md` for scope and limits. `wrench.png`, `empty.png`, `equipped.png`, and `full.png` are 1920×1080 offscreen renders of the actual Unity scene and authored UI. They are not native Game-window captures. Full inventory uses transient review state, not a gameplay grant.

`journey.json`: ten focused final-script checks. `controller.json`: additional D-pad/A/B checks (before the later icon fallback/copy/cache refinement; input semantics unchanged). `application-tests.json`: 15 focused Unity EditMode test results. `full-state.json`: final full-stack count and TMP overflow result. `source-snapshot.json`: final changed C# hashes. `console.json`: retained integration console entries; see implementation notes before interpreting tool errors as application failures.
