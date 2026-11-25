# Scrubber equipment and quick-use verification

The focused [Scrubber PlayMode journey](scrubber-journey.json) loaded the authored demo, confirmed the wrench began equipped in carried inventory, and opened and closed Inventory with simulated keyboard input. The test used the authored Equip and Pin buttons, switched between Weapon and Tool with keyboard and gamepad inputs, interrupted an active swing, and used the pinned medical item without changing either equipment assignment. The second tool and medical item were temporary test fixtures.

The [domain checks](domain.json) covered the one-time starting grant, legacy stack merging, overflow conservation, rejected incoming moves, and capture and rebind. The [save codec run](save-codec.json) included an injected interruption before replacing the committed save and a successful retry. The fault test exercised the actual codec and recovery store through an in-memory file adapter. The Scrubber scene remains disposable. Production scene return and native-player save recovery were outside these runs.

The shared [Primary input](primary-input.json) and [legacy consumable use](legacy-use.json) checks passed on the final implementation.

The [starting inventory](scrubber-inventory.png) and [full inventory](scrubber-full.png) captures use Unity's offscreen 1920×1080 scene render. The [visual check](visual-check.json) recorded zero active text overflow in either state after moving the active indicator into the slot heading. The full inventory used temporary stacks in the demo session. Physical controller and native Game-window appearance were outside this check.
