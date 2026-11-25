# SOLITUDE gameplay exploration

## Observations

- The local branch `codex/run-lifecycle-playable-slice` at `9554b07` already links Wakeup’s cryobay to a maintenance area with a fuse, SEC-0, a scrubber, supplies, and a shortcut. Its input journey exercises repair, lethal conduit damage, succession, and persistent repairs. The current checkout’s Wakeup has different scene bindings. The branches diverged at `62b17b0`, before the newer wrench combat. See `9554b07:Assets/_Project/Editor/RunLifecycleAuthoring.cs:88` and `9554b07:Assets/_Project/Tests/PlayMode/RunLifecycleSliceTests.cs:127`.
- The lifecycle branch disables SEC-0 with an interaction and powers the shortcut with a separate battery. On the current branch, the SEC-0 demo has directional sight, cover, a charged stun pulse, and wrench attacks with range, windup, and recovery. See `9554b07:Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0ExpeditionEncounter.cs:67`, `Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0Controller.cs:97`, and `Assets/_Project/Tests/PlayMode/WeaponCombatIntegrationTests.cs:83`.
- The scrubber demo gives the repair a visible and audible payoff. After installing a fuse and restarting, the room recovers, the warning clears, and the task completes. See `Assets/_Project/Domain/Restoration/ScrubberEncounterState.cs:12` and `Assets/_Project/Tests/PlayMode/ScrubberEncounterSliceTests.cs:179`.

## Gaps

The expedition shows that the authored steps fit together. Its layout and direct disable interaction leave open whether players will choose to slip past a patrol, time a wrench attack, or change plans under pressure. The battery-powered shortcut also leaves open whether the player would value the scrubber repair more if it opened their return route. Existing test source covers mechanics and persistence. This survey did not run Unity, tests, or a hands-on playtest. The current working tree includes in-progress inventory and equipment changes.

## Candidate Directions

1. Playtest the existing lifecycle expedition with a fresh test save and try the separate wrench-versus-SEC-0 demo. If fuse retrieval feels automatic, author a three-to-five-minute maintenance run with an exposed direct path, a longer route with cover, and an optional wrench fight. If the repair payoff feels flat, have the running scrubber open a previously visible return shortcut. Watch where a new player first makes a choice, what they do after detection, and whether they try the other route on a replay.
2. If the fight feels unclear, tune one exchange in the SEC-0 demo. Can the player recognize the charge, move to safety, and land a hit during recovery? Compare keyboard and controller before adding weapons or enemies.
3. If the fight and room are readable but the route still feels automatic, try a detour for one medical pickup. Watch whether going for the pickup is worth the delay.

## Open Questions

- Does the first encounter support fighting and evasion equally, or should one approach be easier to discover?
- Does detection cost enough position or time to matter?
- Which repair payoff do players notice and use on their own?
