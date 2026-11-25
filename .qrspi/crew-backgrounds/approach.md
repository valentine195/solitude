# QRSPI Approach: crew-backgrounds

## Context

The playtest asks whether a crew background changes how players read the same fault and choose a repair. The Engineer and Technician share starting vitals, speed, carry capacity, and weapon damage. Their Electrical and Field Repair ranks trade places. Each background can use standard repairs, interpret the fault through training, and improvise a solution in its specialty. The four background and fault combinations must launch independently into one maintenance room with two authored fault setups. The player sees the return door closed on the way in. It opens after airflow recovers. See [request.md](request.md), [queries.md](queries.md), and [research.md](research.md).

## Constraints and Facts

- The composed demos already use disposable sessions. The scene supplies the avatar, equipment and room, and survivor succession awakens a fresh identity. The existing scene entry accepts a path and has no parameter for background or fault. A background can be supplied to the playtest interactions independently of avatar stats or the survivor save schema. See `Assets/_Project/Core/Composition/GameCompositionRoot.cs:59–88`, `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs:49–87`, and `Assets/_Project/Tests/PlayMode/ScrubberEncounterLifecycleTests.cs:51`.
- The scrubber already distinguishes starting airflow from completing recovery. Its sound, indicators and warning subscribe to that state, but its fault state, screen and task bindings are specific to the fuse. Its supply has no normal or bypass feed model. The generic restoration door permits manual opening after restoration and has no close transition. SEC-0 supports an authored waypoint route and binds to the same-scene player through a scene feature. See `Assets/_Project/Domain/Restoration/ScrubberEncounterState.cs:5–34`, `Assets/_Project/Features/World/ScrubberEncounter/ScrubberRoomFeedback.cs:16–36`, `Assets/_Project/Features/World/ScrubberEncounter/ScrubberComposition.cs:57–75`, `Assets/_Project/Features/World/SolitudeStart/SolitudeSlidingDoor.cs:20–46`, and `Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0Composition.cs:14–38`.
- Existing repairs remove a required item and commit the repair together. Container commands also support moving a whole stack between registered slots. A reusable jumper needs a retained owner while installed. A one-item panel slot and a nonstacking jumper fit the existing transfer model. A consumed item plus a `restored` flag would lose ownership. See `Assets/_Project/Domain/Restoration/RestorableSystemState.cs:17–41` and `Assets/_Project/Domain/ContainerCommandService.cs:184–196,231–259`.
- The existing encounter uses painted Tilemaps. Scene compositions validate that referenced equipment, player and patrol endpoints belong to the same scene. The scrubber authoring tool already refuses to overwrite its saved scene. See `Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity:7827,8169,9318,9799`, `Assets/_Project/Features/World/ScrubberEncounter/ScrubberComposition.cs:21–55`, and `Assets/_Project/Editor/ScrubberEncounterBuilder.cs:261–277`.

## Candidate Approaches

### Option A: One saved maintenance scene with four Editor launch presets

Keep one disposable scene with a painted room, one composed player and one set of world feedback. Expose four named Editor launches for the two inputs. A launch applies an authored fault setup and assigns the active survivor’s background for the session before gameplay starts. The preset pair must be scoped to the intended scene load. Failing startup, replaying, or entering ordinary Play must have predictable behavior so stale selections cannot silently change the trial. Tests can load the same scene and presets through the same setup boundary. This keeps layout, collision, timing and interactable positions common across combinations. It adds a small Editor-to-Play handoff that needs focused startup and reset coverage.

Use a bounded encounter model for the two faults, a shared rank lookup for diagnostics and specialist actions, and a single source of truth for airflow and recovery. Represent normal feed, panel bypass and corridor light availability as relationships within the room. The panel’s installed jumper is the source for bypass availability, while the appropriate hardware repair consumes its material through the inventory command path. Bind physical interactions, sound and lighting to the committed world state. This fits the trial without designing ship-wide electricity, proficiency progression or production save migration. It still requires distinct fault presentation and a return door that starts closed and opens on recovery. Extending the fuse-specific screen and journal indiscriminately would increase regression risk for the existing demo.

### Option B: Four directly playable scene assets using a shared room prefab

Place an instance of the same painted room prefab in four disposable demo scenes and serialize the background and fault in each scene. Room art, collision and common encounters can be edited in one prefab. Each variant opens directly in the Editor and by scene path in tests. The encounter still needs authoritative rank, repair and airflow state, including jumper ownership and the door response. Serialized entry settings remove the Editor launch handoff and make a misconfigured combination visible in scene authoring.

Unity references from the instantiated room to the composed player and scene configuration still need explicit binding. Player, patrol and equipment overrides can drift across four scenes even when the room prefab is shared. Authoring and verification would have to cover the entry and shared geometry in each scene, and moving existing painted maps into a prefab would add migration work if the fuse demo were reused. The disposable session keeps rollback local under either option.

## Recommendation

Choose Option A. A dedicated playtest scene preserves the existing fuse demo and gives the comparison one layout and one set of composition bindings. Keep the two faults authored and use the same avatar settings across launches. Put the rank lookup at the encounter interaction boundary so ordinary repairs stay available to both backgrounds and the trained clues and specialist choices come from the same assigned proficiency. Reuse the existing inventory transaction, SEC-0 composition and scrubber feedback patterns where they fit. Drive the return door from completed recovery so its animation and collision reflect the room state.

The most useful verification is a focused pass through the four launches, including the standard fallback paths, repair costs, feedback and door timing. The power test should observe jumper ownership alongside the corridor light and scrubber state. A replay after driving the room to a changed state should verify a fresh avatar, inventory and fault. Existing tests show how to load disposable scenes and observe those patterns. The playtest behavior has yet to be implemented or exercised.

## Decision

Approved Option A. Use one painted maintenance scene and four named Unity Editor launches. The shared room and scene bindings keep layout and encounter conditions consistent across the four playtest combinations.

Approved comparison SHA-256 `2e2e26a70bfd5265fbb87653ad8bc8e36b9ca0f0ad0aa5f1e99599cc025a9d82`.

## Open Questions

No source evidence gap prevents choosing between the approaches. The brief leaves the exact behavior of recovering the jumper after returning to normal power open. Resolve that behavior during specification if the trial needs a playable handoff. The state model should accommodate the distinction between a jumper actively powering the scrubber and one that is physically present in the panel.
