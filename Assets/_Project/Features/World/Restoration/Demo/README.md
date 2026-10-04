# Ship System Restoration demonstration

Open `Assets/_Project/Scenes/Demos/ShipSystemRestoration.unity` directly in Unity, then enter Play mode. Stop any existing Play session first. Production and demo sessions deliberately refuse to mix.

Use the existing movement and interaction bindings (WASD and E; inventory/hotbar controls remain unchanged). You start in the service bay beside the locker. Inspect the auxiliary junction near the north exit; collect the nearby battery and install it. Power makes the exit door usable: interact with the door separately, then approach the cyan AI terminal beyond it and read its placeholder message. The second battery, junction, and green service terminal demonstrate independent state. Locker transfers remain available; installation reads player inventory only, so transfer hotbar/locker items back first.

Replay by stopping and restarting Play, or reloading this scene. Inventory, pickup claims, locker contents, and restoration all reset together. The demo uses only in-memory save storage and never loads the normal player save. It is an Editor demonstration, not added to the shipping scene list. No production/reference scene is migrated.

To reuse: add a RestorableSystem, configure a RestorationJunction with that system and an existing ItemDefinition, and include the junction in SceneBindings' Restoration Junctions list. Give each system its own component instance. Add RestorationDoorGate to a SolitudeSlidingDoor and assign the system; configure RestorationTerminal with the same system, a screen SpriteRenderer, colors, and text. Consumers depend on the system, not the junction. Include pickups in SceneBindings and use unique SaveableId values. Restoration requires exactly one inventory item; no extraction or disk persistence is implemented.

`SOLITUDE/Demos/Build Ship System Restoration` creates the demo from the existing opening reference scene through Unity APIs and refuses to overwrite an existing demo.
