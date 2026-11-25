# SOLITUDE Deck Map

Mode: Operate. A full-screen map for finding the player, the tracked objective and the next reachable point. The layout adapts the earlier Service Deck concept in `work/Reviews/ShipLayoutResearch/service_deck_concept.png`. Life Support as the tracked objective, the player position and the unpowered gate are illustrative preview data.

Open `index.html`. Click the Power Gate or objective marker to see its details. The right column toggles the route and objective markers. **Esc** or **M** returns to the exploration mockup. **M** reopens the map. Browser controls are for preview. The Unity keyboard and controller bindings and pause policy are open design decisions.

The solid amber route stops on the reachable side of the gate. The lock and Unpowered label report its condition independently of amber selection. Dashed outlines identify unsurveyed areas. The exploration preview includes vitals and the crew label, with the weapon readout and hotbar omitted.

The map is a visual prototype. Unity scenes, UI prefabs, input bindings and game state are untouched.
