# Player vitals

## Intent

The user describes SOLITUDE as a roguelite about rebuilding a ship, with early play constrained somewhat by food availability. Existing vitals are health and O2. This exercise adds hunger and covers the vitals systems and UI.

## Agreed direction from the dialogue

- Hunger constrains passive health recovery. Empty hunger does not directly damage or kill the player.
- Health regenerates to the top of its current segment after a short delay following damage.
- Medical items can heal across segment boundaries and still work when hungry.
- Hunger uses a simple threshold: adequately fed permits passive regeneration; hungry pauses it.
- UI shows health segment boundaries, a hunger threshold marker, and a status beside health such as “Recovery paused: hungry” when appropriate.
- Make segment count configurable now. The player suggested future medical-system improvements with fewer segments: “four tiers -> 3 -> 2”. Implementing ship upgrades is deferred.
- Food production and availability inform the feature; this exercise remains focused on systems and UI.

## Open details

The regeneration threshold, damage delay, healing rate, hunger depletion rate, and food restoration amount remain provisional tuning choices. Balancing through playtesting is deferred.

## Clarifications

- What gameplay actions or elapsed-time conditions should deplete hunger, and how should consuming food restore it?

Answer: Hunger declines slowly during active gameplay, and consuming food restores a fixed amount.

- Should this exercise select provisional tuning values for playtesting, or include playtesting and adjustment before completion?

Answer: Use provisional tuning values for this exercise; leave balancing through playtesting for later.

- Should health initially use four equal-sized segments, or a different count or distribution?

Answer: Four equal-sized health segments initially.

- When health is exactly on a segment boundary, should passive recovery stop there or recover toward the next boundary?

Answer: Passive recovery stops at an exact segment boundary; it does not recover toward the next boundary.

- How should players activate food and medical items in this exercise?

Answer: A dedicated Use action activates the selected hotbar food or medical item.

- Which clock should hunger, passive regeneration, and the post-damage delay use?

Answer: All three share scaled gameplay elapsed time, gated by gameplay eligibility. They stop during menus, pause, transitions, and other states that disallow active gameplay.
