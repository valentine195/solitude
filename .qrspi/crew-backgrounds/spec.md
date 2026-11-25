# QRSPI Spec: crew backgrounds

## Inputs

Paths are relative to the repository root. Digests are SHA-256 hashes of the exact input bytes.

| Input | SHA-256 |
| --- | --- |
| `.qrspi/crew-backgrounds/request.md` | `ad924352b4499384828f746cdccad7d4f1d58f6b6816ef1d374b01626a01a1cd` |
| `.qrspi/crew-backgrounds/queries.md` | `7a319334c7a3e516ad4b6c6172e52ac805c0acde89a61481548aa516c19f92ed` |
| `.qrspi/crew-backgrounds/research.md` | `e08cb77a36b3857778f8b97eceb45a6c28c51709f663849199bae43daf974f06` |
| `.qrspi/crew-backgrounds/approach.md` | `e30b6eba20f650c4178362ef6c29ba739d19011dc9bfd27acc488be4a8236ac5` |

Initial spec. S1 through S14 are new.

## Playtest launch and character

Today the scrubber demo launches with one authored player and one blown-fuse fault. The new disposable playtest uses one editable, painted maintenance room and four named Unity Editor launches.

| Launch | Background | Fault |
| --- | --- | --- |
| Engineer / Loss of Power | Engineer | Loss of Power |
| Engineer / Broken Intake Linkage | Engineer | Broken Intake Linkage |
| Technician / Loss of Power | Technician | Loss of Power |
| Technician / Broken Intake Linkage | Technician | Broken Intake Linkage |

A launch establishes the selected background and fault before the player gains control. It starts with a fresh colonist, inventory, authored supplies, SEC-0 patrol, scrubber state, and closed return door. Repeating or changing a launch starts from its authored baseline. If the requested setup cannot be applied, the launch reports the failure. Later launches and ordinary Editor Play must use their own startup settings.

Both backgrounds use existing shared starting Health, Oxygen, Hunger, base movement speed, carry capacity, and weapon damage. Within each fault setup, the room, patrol route, initial supplies, and access to standard repairs are the same for both backgrounds.

| Background | Electrical | Field Repair |
| --- | --- | --- |
| Engineer | Specialist (2) | Trained (1) |
| Technician | Trained (1) | Specialist (2) |

Basic (0) covers ordinary inspection and standard repairs. Rank 1 and above grants the corresponding trained diagnosis. Rank 2 also grants the specialist action when the active fault, supplies, and room state allow it. The brief does not require additional skills for this playtest.

## Faults and repair choices

Today the scrubber consumes a fuse and requires a restart before recovery. In the new room the scrubber starts with stopped airflow. The active fault determines which controls and materials can restore it. Ordinary inspection communicates the symptom and gives enough information to follow the standard route. The room also offers an optional specialist route for the matching background.

| Active fault | Trained diagnosis | Standard repair for either background | Specialist repair |
| --- | --- | --- | --- |
| Loss of Power | Trained Electrical traces the dead feed to the normal breaker. | Reach the breaker across SEC-0’s patrol and reset it. The scrubber starts on normal power and the corridor stays lit. | Specialist Electrical uses one reusable jumper at the nearby service panel. Before commitment, the panel explains that the scrubber will start and the corridor lights will go out. Success installs the jumper and commits both power changes together. |
| Broken Intake Linkage | Trained Field Repair identifies the damaged linkage and the correct replacement part. | Retrieve and fit one correct replacement part. The part is consumed and airflow starts. | Specialist Field Repair rigs the linkage with one unit of the authored salvage. The salvage is consumed and airflow starts. The player can complete this route without retrieving the replacement part. |

The power-loss setup starts with an intact linkage, a tripped normal breaker, and working corridor lighting. The linkage setup starts with normal power and a broken linkage. Each setup provides reachable materials as needed for both its standard and specialist routes.

Tool work is a short, interruptible action with physical feedback at the control or machine. An inspection or preview cannot commit a repair. At completion, validate the active fault, action availability, rank where required, and materials. A successful action commits its world and inventory effects together. A rejected, canceled, or interrupted action before completion preserves the pre-action state and materials. Repeating a completed repair cannot consume or transfer another item. The player receives a readable reason when an attempted action is unavailable.

### Installed jumper and normal power

The jumper belongs to the service panel while installed and is unavailable in carried inventory. Leaving and returning during the same playtest retains that installation. Either background can reset the normal breaker during bypass. Normal power then feeds the scrubber and corridor lighting returns without interrupting active airflow or resetting recovery. The jumper stays in the panel as an inactive item. Once normal power is restored, the player can recover the same jumper into inventory. If inventory cannot receive it, it remains installed and the game reports why.

This handoff and manual retrieval are proposed gameplay behavior for Spec review. They give the reusable item an observable lifecycle within one room.

### Room recovery

After either successful repair, the player sees and hears the scrubber come online and receives a clear indication that the room is recovering. Use the existing scrubber demo’s five seconds of active recovery as the playtest baseline. The return door remains closed and blocks passage until recovery completes. It then opens automatically and becomes traversable through its normal opening behavior. The player sees the shut door on the way in and can walk through it after repair. Active recovery respects the existing gameplay pause policy.

## Observable contracts and compatibility

The playtest launch provides a background and a fault. The initial room state, eligible diagnostics, and repair options reflect those inputs. Each interaction reports a result consistent with the committed state. The model distinguishes fault, repair method, airflow and recovery, normal supply and bypass, corridor lighting, and the jumper’s current owner. Effects are observable through player inventory, room controls, sound, lighting, and door traversal. This contract does not prescribe public APIs or storage structures.

The new room uses a disposable session. Production saves and the existing fuse, restoration, and SEC-0 demos retain their current startup and interaction behavior. The shared avatar and existing keyboard and controller interaction paths remain usable. The saved maintenance room remains editable with the Unity Tilemap tools.

## Acceptance criteria

| ID | Observable behavior | Focused verification |
| --- | --- | --- |
| S1 | Each of the four named Editor launches loads the same painted room with its named background and only its named fault. The correct setup is active before input begins. | Launch all four combinations and compare initial state and scene identity. |
| S2 | Both backgrounds start with the same existing vitals, base movement speed, capacity, and weapon damage. The displayed or observable Electrical and Field Repair opportunities match the background ranks. | Compare startup state and the available actions for both backgrounds in each fault. |
| S3 | Both backgrounds can understand the fault and perform standard repairs. Trained Electrical traces the dead feed and Trained Field Repair identifies the replacement part. Specialist rank also grants its trained diagnosis. | Inspect each fault with both backgrounds through normal interaction. |
| S4 | Either background can cross the patrol route and reset the normal breaker. The scrubber starts on normal power, the corridor is lit, and no jumper is installed. | Exercise the breaker route with both backgrounds. Observe the patrol on that route and check room and item state. |
| S5 | The Engineer sees the lighting tradeoff before using the jumper. Successful use starts airflow, darkens the corridor, and transfers one jumper from carried inventory to the panel. The Technician cannot take that specialist action even when carrying the jumper. | Check preview, successful commit, and rank rejection with the item present. |
| S6 | Resetting the normal breaker during bypass restores lighting and maintains airflow and recovery progress. The inactive jumper stays in the panel until reclaimed once. If inventory cannot accept it, the jumper stays in the panel and the player receives feedback. | Exercise changeover during recovery, full-inventory refusal, and later successful reclaim. |
| S7 | Either background can fit the correct intake part. Exactly one part is consumed and airflow starts. A missing or wrong part gives a readable refusal. | Exercise the standard linkage route with both backgrounds and verify inventory and airflow on failure and success. |
| S8 | The Technician can rig the linkage with the authored salvage without retrieving the replacement part. One unit of salvage is consumed. The Engineer cannot take that specialist action even when carrying the salvage. | Exercise the approach with both backgrounds and compare outcome and inventory. |
| S9 | Preview, refusal, cancellation, or interruption before completion has no repair or item side effects. Repeating a completed interaction cannot duplicate the jumper or spend another part or salvage item. | Exercise preview, cancellation or interruption, invalid state, and repeated input around commit. |
| S10 | Successful repair produces visible and audible feedback. The normally closed door blocks traversal through the recovery interval and opens automatically when recovery completes. Its blocker clears as the opening completes. | Exercise the four repair routes and walk through the door. Observe feedback and pause during recovery. |
| S11 | Leaving and returning in one playtest preserves the repair and installed-item state. A new launch resets the colonist, inventory, supplies, patrol, lighting, door, and recovery to the named setup. A failed launch cannot leak its selection into the next launch. | Repeat a launch after a completed bypass and then switch setups. Exercise an invalid launch. |
| S12 | The four combinations can be played through using normal keyboard and controller actions, with readable requirements, tradeoffs, and results. | Targeted in-room manual playtest on both input types. |
| S13 | The existing demos run their current authored paths, and a crew playtest leaves an existing production save intact. | Focused demo smoke checks and a before-and-after comparison of isolated production save bytes. |
| S14 | The room’s saved floor and boundaries can be inspected and edited as authored Unity Tilemaps. All launches load the same layout. | Inspect scene ownership and editability in the Editor and compare the four launches. |

## Out of scope

The trial covers Engineer, Technician, and the two authored faults in one room. Later work can address the other crew backgrounds, a selection screen, proficiency progression, procedural fault selection, production integration and persistence, and ship-wide power simulation.
