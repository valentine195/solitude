# Inventory equipment and quick use

Draft for review. Applies to the next inventory task, demonstrated in `AtmosphericScrubberEncounter`. On approval, this spec supersedes the hotbar and equipment rules in the [inventory design handoff](InventorySpecification.md).

Inventory is where the player equips a weapon and a tool and chooses a consumable to use quickly. During exploration, a Switch Equipment input changes which equipped item is in hand. Primary uses the item in hand, and Use activates the pinned consumable. The numbered hotbar and its duplicate inside Inventory are removed from the updated scene.

## Equipment

**S1.** Inventory has one Weapon slot and one Tool slot. Inspecting a carried item offers Equip to its eligible slot. If both slots are eligible, the player chooses the destination. Equipping leaves the item in carried storage. The first occupied slot becomes active. Equipping into the other slot keeps the current slot active. Unequip clears the chosen slot. A dual-purpose item such as the maintenance wrench can occupy either slot. Moving it to the other slot clears its previous assignment. Inspection and navigation leave equipment alone.

**S2.** Slot eligibility comes from the item's authored classification and supported capabilities. Weapon accepts a weapon with a supported Primary action. Tool accepts a tool with a supported Primary or contextual capability. During exploration, Switch Equipment toggles between the two occupied slots without opening Inventory. Provide a binding for keyboard and controller and show the item in hand in the existing HUD. With only one occupied slot, keep that item in hand. Primary requests the in-hand item's Primary action through the existing executor. If the item has no Primary action, the input starts no action. Contextual interactions keep their existing input and eligibility rules.

**S3.** Equipment refers to carried items by stable item ID, so rearranging inventory preserves both assignments. If the last carried copy leaves inventory, clear its assignment. If the active slot becomes empty, select the other occupied slot, if any. Switching away from, replacing, unequipping or losing the item in hand cancels its active action through the existing executor. The equipped wrench remains available to carried-tool checks in either slot.

## Quick use

**S4.** Inventory can pin one consumable with a supported gameplay effect. Pinning stores the item ID and leaves its stack in inventory. The player can replace or clear the pin from Inventory. An exploration HUD cue shows the pinned item, its carried quantity, and the active input hint. The cue is hidden when the pin is empty.

**S5.** The existing gameplay Use action attempts to use the pinned consumable from carried storage. If there are multiple stacks of that item, use the first occupied stack in inventory order. Apply the effect and consume one unit through the existing consumable flow on success. If a use is rejected, retain the item and use existing gameplay feedback where available. Clear the pin when the last carried unit leaves inventory. Pinning, using and clearing a consumable preserve both equipment assignments and the item in hand.

**S6.** Inventory keeps its current pause and Back policy. Equip, Unequip, Pin and Clear work through the authored UI with keyboard and controller. Opening or closing Inventory, holding an input across the transition, or confirming a menu action must not trigger a swing, consumable use, or equipment switch. Remove numbered hotbar selection and Assign to hotbar from the updated player flow. Preserve carried-item rearrangement.

## Loadout and save compatibility

**S7.** A new run grants the configured starting wrench to carried inventory and equips it in Weapon before the player becomes ready. The grant happens once per run. Tool and quick use start unassigned. Restore both equipment assignments, the active slot and the quick-use pin from the run save. Validate restored items against carried inventory and slot eligibility, and apply the same fallback as when an active slot becomes empty. Scene return preserves valid references.

**S8.** Existing saves retain every carried and legacy hotbar item, its quantity and its item identity. Keep carried items in place where possible and bring legacy hotbar stacks into carried storage using normal stack limits. When normal capacity is insufficient, expose the excess as carried overflow that accepts no new pickups; the player can inspect, equip, use or move items out of it. Resolve migration once per save and publish the migrated containers and references together. A failed write or retried load preserves a complete recoverable snapshot without loss, duplication or a repeated starting grant.

**S9.** If a legacy save contains a valid saved hotbar selection for an equippable item, carry it forward. When that selection is unavailable, use the first equippable item in legacy hotbar order. Put that item in its eligible slot and make it active. Prefer Weapon if it qualifies for both slots. Leave the other slot and quick use unassigned. If no item qualifies, leave both equipment slots empty.

## Delivery and verification

Keep the current Inventory layout where it fits the new flow, using authored prefabs and the existing container, save, input and item-action services. Update the Scrubber builder so rebuilding the demo preserves its wiring. Additional equipment slots, multiple quick-use bindings, in-menu consumable effects, stack splitting, world drop and shipping-scene rollout are deferred.

- **S1–S3, S7:** Enter the demo with the starting wrench in Weapon and swing it. Equip a Tool and switch between the two with keyboard and controller while Inventory is closed. Verify interruption when switching mid-swing, fallback when the active item is removed, and stable equipment when items move between carried slots. Use test-only item data for the second slot if needed.
- **S4–S6:** Pin an existing supported consumable, close Inventory, and use it with keyboard and controller input. Verify one effect and one consumed unit on success, unchanged assignments and item in hand, no consumption when rejected, and a cleared cue on depletion. Check held-input boundaries and contextual input.
- **S7–S9:** Check new run, reload, scene return, legacy migration with and without available space, and interrupted migration followed by retry. Compare item totals, equipment assignments, active slot and quick-use pin before and after.
- **S1, S4, S6:** Inspect the actual demo at 1920×1080 with empty and occupied equipment slots, a pinned item and a full inventory. Check legibility, focus order, and the equipment and quick-use HUD cues. Confirm the numbered hotbar is gone.
