# Wakeup verification gate

Run from any directory on a macOS ARM64 machine with the project's Unity version,
Unity CLI, license, and macOS build support installed:

```sh
python3 Tools/Verification/verify.py
```

The runner copies Assets, Packages, and ProjectSettings into a dedicated temporary
project. It never launches gameplay against your normal player save directory.
Reports, test XML, logs, standalone builds, and disposable save scenarios go to
`Logs/Phase5`. `--workspace` and `--output` can select alternative isolated paths.
The required gameplay entry is Wakeup; Expedition is excluded. Tileset work is
separate from this gate.

The complete gate runs the same Domain/Application test sources in .NET without
Unity, Unity EditMode tests, production-root PlayMode lifecycle tests, authoring
validation, and two native ARM64 builds. Fixtures are generated through Unity's
Editor API by `PhaseFiveFixtureBuilder`; they are never shipping scenes.

The verification player drives the production input asset and EventSystem,
collects a world pickup, saves inventory/hotbar and generated locker contents,
restarts the process, and starts a fresh game. File scenarios include temporary
files, missing/corrupt primary saves, incompatible saves, denied reads, and forced
process termination at durable-write/recovery checkpoints. Every resumed process
must preserve a complete snapshot and collection facts. Recovery must display a
notice until dismissed. Backup bytes must survive recovery and its interruptions.

`results.json` is the final verdict. A nonzero exit, missing result, unexpected
player exception, test failure, validation failure, or build failure fails the gate.
`--skip-build` reruns existing player outputs and explicitly reports a partial gate;
it does not qualify a source change. Other operating systems and CPU targets are
not qualified yet.

Verification hooks compile only with `SOLITUDE_VERIFICATION`; the shipping build
is checked for absence of the verification and test assemblies and NUnit. The
normal player does not accept the verification save-directory argument.
