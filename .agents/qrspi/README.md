# QRSPI in SOLITUDE

The eleven `qrspi-*` skills are installed under `.agents/skills/`. Their runtime and role references are self-contained. `installation.json` records the exact local Codex-port revision; the upstream default branch does not contain these local changes yet. Refresh these folders from an approved updated checkout, preserving local customizations. Do not reinstall from upstream main expecting this port.

## Start or resume

Open a SOLITUDE chat and invoke `$qrspi-workflow <feature-name>` with a plain description of one behavior. For example:

```text
$qrspi-workflow locker-transfer
Project root: /Users/jeremyvalentine/SOLITUDE
Players can transfer one inventory item into a locker.
Use interactive gates and stop after the approved plan.
```

Always resolve the repository root before running the helper; the saved Codex project starts in `Assets/_Project`. Every helper call uses `--project <repository-root>`. Artifacts belong to `.qrspi/<feature>/`, and surveys to `.qrspi/explore/<name>/`. No feature workspace has been created by this installation.

Use `$qrspi-implement` for one approved phase, then `$qrspi-review` at its boundary. Start with interactive execution. Use `$qrspi-autoloop` only when the human chooses unattended execution for an approved spec, plan and resolved phase scope. It stops before final review. An old `qrspi/` workspace requires an explicit collision-safe migration; do not merge it with `.qrspi/` automatically.

## Helper

The locally built `qrspi-x` package is installed in this machine's existing Node 22.18.0 prefix. Its executable uses that Node interpreter directly, so the older default Node does not break it. It is on this session's PATH and contains a bundled copy, independent of the original chat's checkout. Use `qrspi-x --help` to inspect commands.

Other machines need Node 22+ and the matching Codex-port helper build. Skill copies alone do not install the helper. Package version `0.10.0` alone does not identify the fork: use `installation.json`'s commit. This setup does not publish or change release packaging.

Interactive steps can use artifact-only mode if the helper is missing. Autoloop requires it. Treat `dirty-tree` as a finding needing attention; never discard unrelated work to satisfy entry checks.

## SOLITUDE verification

For every planned behavior, identify meaningful existing Domain/Application, EditMode, PlayMode, or manual Unity checks, with criterion IDs and observable expected results. Preserve existing tests and assembly boundaries. Keep verification tied to the exact source revision; unrun or partial checks are not passes.

For source changes affecting Wakeup, inventory, hotbar, locker contents or save recovery, consult [the existing verification gate](../../Tools/Verification/README.md). Run from the repository root:

```sh
python3 Tools/Verification/verify.py
```

The gate uses an isolated temporary Unity project and disposable saves. Its final result is `results.json`; failed or missing results fail verification. `--skip-build` is a partial rerun and does not qualify source changes. Its documented qualification is macOS ARM64 and Wakeup; choose relevant additional checks for behaviors outside that scope. Do not run the full gameplay gate just to validate QRSPI installation.

For art work, retain `solitude-art-pipeline` as the production and Unity integration skill; QRSPI can define behavior, scope and acceptance while that skill supplies the production workflow.
