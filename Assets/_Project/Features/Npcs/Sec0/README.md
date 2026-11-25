# SEC-0 security patrol NPC

Open `Assets/_Project/Scenes/Demos/Sec0Patrol.unity` directly and press Play.
Use the existing movement keys. Approach the rover from its front, duck behind
cover, or step sideways when it flashes red. The encounter uses the existing
player, camera, HUD and scene bootstrap. Its disposable in-memory save session
never loads or writes the production save. This scene is not in shipping build
settings. Existing Wakeup scene authoring is unchanged.

## First playable behavior

- Blue: follows a four-point route, pausing at each waypoint.
- Amber: confirms a visible intruder for 0.35 seconds or investigates the last
  seen position. After three seconds of searching it returns to patrol. An
  unreachable last-seen position also times out.
- Red: pursues a confirmed intruder at short range. Within 1.8 world units it
  stops and flashes for 0.65 seconds before attempting a stun pulse.
- Aiming locks at the start of the warning; a sideways dodge can miss the shot.
  Solid walls/cover block both sight and the pulse. Leaving range or breaking
  sight cancels the warning. A shot is followed by a 1.6 second recovery.
- A hit immobilizes the player for 0.8 seconds, without dealing health damage.
  Held movement resumes after the stun. Repeated hits extend the remaining
  duration rather than accumulating movement locks.
- Readiness, pause, inventory/screens, transitions and dead/inactive targets gate
  NPC updates. Stun timers freeze while gameplay is gated and release cleanly
  with the scene. NPC state is transient and resets on scene reload.

Sound detection, backup AI, EMP, hacking and NPC persistence are not implemented.
The separately authored radio-call effect remains available for a later pass.

## Authoring

`PF_Sec0.prefab` has a stationary parent, a physics rover child and sibling
waypoints. Keep waypoints outside the moving rover hierarchy. Move the parent
when placing an encounter, then edit waypoint positions. Do not parent the
route under the moving body. The controller exposes speeds, sight cone/range,
attack timing/range and patrol pauses. Its selected gizmos show the route and
sight/attack radii. Movement uses local four-neighbour obstacle routing and a
solid-body cast before every physics step; this avoids cutting through corners.
The local A* route search is bounded to eight units around the rover and 900
visited/discovered cells. A binary priority queue directs the search toward the
destination; per-agent search buffers and a path cursor are reused.

Patrol, pursuit, and investigation all use one target-driven navigator. Callers
report the destination with `SetDestination`, then ask `Next` for a safe waypoint.
An unchanged destination keeps a healthy route indefinitely. SEC-0 allows target
movement within 0.4 units of the planned goal to adjust the final endpoint; larger
accumulated movement requests a fresh plan. The generic API defaults to replanning
on any destination change. Off-route displacement or a blocked next segment also
requests a plan, while every actual movement retains its physics guards.
`InvalidatePath` supports known topology changes without discarding the destination;
`Reset` releases the destination and transient route state.

Failed searches retry at most once per 0.5 seconds so an opening door can be
noticed even without topology events. Explicit destination/invalidation notices
and significant displacement can retry immediately. Healthy routes have no timed
expiry. A newly opened shortcut does not replace an otherwise valid detour unless
it is invalidated; existing doors do not yet publish navigation notifications.

The visual child uses the existing sprite/animation family. Four `Charge_*`
clips pulse the hostile sprite to telegraph firing. The existing sprite clips
and their GUIDs are retained. Signal effects use an unlit material.

Add one `PlayerStunReceiver` to the local player and a `Sec0Composition` to the
scene, configure explicit player/vitals/bot references, and append that feature
once to `SceneBindings.features`. The composition owns binding/release; an
unbound prefab remains inert. No NPC looks up a global player or input host.
`MovementBehavior.BlockMovement()` is a reversible movement lease shared with
the stun receiver; it preserves current requested input.

Editor menus under `SOLITUDE > NPCs` build the prefab and demonstration scene.
The demo builder refuses to overwrite an existing scene. It copies the saved
Wakeup scene and adds an isolated training area; it does not save or replace an
open source scene with unsaved changes.

## Verification

`SecurityBotBrainTests` covers confirmation, lost-sight search, unreachable
search timeout, charge cancellation, one-shot recovery and gameplay freezing.
`Sec0RuntimeTests` checks the actual prefab's sight/wall queries, dodging,
non-lethal stun, movement recovery, pause and scene release, and obstacle routing.
`Sec0EncounterTests` loads the authored demo through its real composition root,
checks patrol/stun behavior and disposable-session reload, and captures the scene.
Verification artifacts are in `work/Reviews/Sec0Npc`, outside Assets.
