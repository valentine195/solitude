# SOLITUDE agent guidance

## Project structure

Follow the ownership, placement, naming, and safe-move rules in [README.md](README.md).
Keep first-party imported assets under `Assets/_Project`; keep review outputs and
disposable exports outside `Assets`. Preserve existing asset GUIDs and assembly
boundaries when moving content.

## Room creation and Tilemap authoring

Prefer painted Unity Tilemaps for room layouts whenever possible. Provide reusable
Tile Palettes and use RuleTiles for connected walls, boundaries, edges, and corners
where neighbor-based selection is appropriate. Use ordinary Tiles for deliberate
variants such as windows, consoles, and specialized wall panels.

Keep the saved scene's Tilemaps editable with Unity's painting tools and preserve
manual layout changes. Builder scripts may prepare assets, palettes, components,
and an initial scaffold, but should not be the primary room-authoring workflow or
overwrite painted layouts. Use programmatic layout generation when explicitly
requested or when procedural gameplay requires it.

## QRSPI project root

Run QRSPI with the Git repository root as its project root, even when the chat starts in `Assets/_Project`. Resolve the root with `git rev-parse --show-toplevel` in the caller. Pass this root explicitly to roles and use `--project <repository-root>` on helper commands. Keep workflow artifacts in `<repository-root>/.qrspi/`, outside `Assets/`. Skills are installed in `<repository-root>/.agents/skills/`.

Before orchestrating a QRSPI workflow, the caller reads [.agents/qrspi/README.md](.agents/qrspi/README.md). Query and Research do not read that setup guide or inherit the caller's reading. It supplies setup and verification guidance, not feature intent.

## QRSPI subagents and gates

When the user invokes or authorizes a QRSPI skill, spawn the subagents required within that step or the explicitly approved Autoloop phases. Query, Research, and Review require new contexts with no inherited conversation history and only their permitted inputs. Supply the complete bundled role and runtime contract; named-agent registration does not establish isolation. If fresh contexts are unavailable or ambient instructions inject forbidden inputs, stop the isolated step and explain what is missing. Query receives only the raw brief and allowed clarifications/questions, never project facts, verification guidance or source evidence. Research receives questions and source roots, never feature intent or design. Review independently checks scoped code against the spec and receives no implementation narrative or claimed tests.

Preserve human gates, exact-revision approval, provenance checks, and criterion-linked evidence. Optional explanation needs opt-in. Autoloop requires approved resolved scope, permits one repair per phase, and stops before final review. Delegation authorization does not authorize external messages, pushing, publishing, merging, deployment, destructive operations, or wider scope.

## Existing work

The caller inspects the working tree before starting a step; implementation and review inspect it within their role contracts. Query must not inspect Git state or gather repository facts. Preserve unrelated uncommitted work and never stage, discard, or commit it as part of a QRSPI feature. Surface helper dirty-tree findings; do not bypass them or clean the tree automatically. Keep the existing project skills and their supporting resources intact.

## Focused verification

Run only tests directly relevant to the changed behavior and its affected dependencies.
Choose focused test filters and targeted scenarios; do not run the complete gameplay
suite or rebuild unrelated targets by default. Reuse passing evidence for unchanged
source, stating its snapshot and limits. Broaden checks only when a concrete failure,
changed dependency, or explicit user request justifies them, and explain the reason.
This applies to QRSPI plans and overrides blanket full-suite guidance in older plans
or verification documentation. Keep corruption, recovery, and behavioral assertions
intact; report any untested behavior honestly.
