# SOLITUDE agent guidance

## QRSPI project root

Run QRSPI with the Git repository root as its project root, even when the chat starts in `Assets/_Project`. Resolve the root with `git rev-parse --show-toplevel` in the caller. Pass this root explicitly to roles and use `--project <repository-root>` on helper commands. Keep workflow artifacts in `<repository-root>/.qrspi/`, outside `Assets/`. Skills are installed in `<repository-root>/.agents/skills/`.

Before orchestrating a QRSPI workflow, the caller reads [.agents/qrspi/README.md](.agents/qrspi/README.md). Query and Research do not read that setup guide or inherit the caller's reading. It supplies setup and verification guidance, not feature intent.

## QRSPI subagents and gates

When the user invokes or authorizes a QRSPI skill, spawn the subagents required within that step or the explicitly approved Autoloop phases. Query, Research, and Review require new contexts with no inherited conversation history and only their permitted inputs. Supply the complete bundled role and runtime contract; named-agent registration does not establish isolation. If fresh contexts are unavailable or ambient instructions inject forbidden inputs, stop the isolated step and explain what is missing. Query receives only the raw brief and allowed clarifications/questions, never project facts, verification guidance or source evidence. Research receives questions and source roots, never feature intent or design. Review independently checks scoped code against the spec and receives no implementation narrative or claimed tests.

Preserve human gates, exact-revision approval, provenance checks, and criterion-linked evidence. Optional explanation needs opt-in. Autoloop requires approved resolved scope, permits one repair per phase, and stops before final review. Delegation authorization does not authorize external messages, pushing, publishing, merging, deployment, destructive operations, or wider scope.

## Existing work

The caller inspects the working tree before starting a step; implementation and review inspect it within their role contracts. Query must not inspect Git state or gather repository facts. Preserve unrelated uncommitted work and never stage, discard, or commit it as part of a QRSPI feature. Surface helper dirty-tree findings; do not bypass them or clean the tree automatically. Keep the existing project skills and their supporting resources intact.
