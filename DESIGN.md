---
name: SOLITUDE
description: Shipboard interfaces for exploration, equipment, and restoration.
colors:
  primary: "#F1BA68"
  panel-ink: "#10191B"
  inventory-surface: "#182932"
  inventory-action: "#23353E"
  panel-rim: "#A9B9B3"
  inset-rule: "#4B6067"
  vitals-text: "#E7EBE8"
  action-text: "#DCE5E2"
  health: "#83D8A4"
  oxygen: "#A8B9F2"
typography:
  inventory-title:
    fontFamily: "Exo 2"
    fontSize: "36px"
  section-label:
    fontFamily: "Exo 2"
    fontSize: "23px"
  inventory-body:
    fontFamily: "Exo 2"
    fontSize: "20px"
  inventory-action:
    fontFamily: "Exo 2"
    fontSize: "21px"
  vitals-health-label:
    fontFamily: "Exo 2"
    fontSize: "30px"
    fontWeight: 800
spacing:
  inventory-panel-inset: "36px"
  carried-slot-gap: "6px"
components:
  inventory-action:
    backgroundColor: "{colors.inventory-action}"
    textColor: "{colors.action-text}"
    typography: "{typography.inventory-action}"
    height: "40px"
  inventory-action-focused:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.panel-ink}"
  equipment-card:
    backgroundColor: "{colors.panel-ink}"
    width: "268px"
  capacity-counter:
    backgroundColor: "{colors.panel-ink}"
    width: "190px"
    height: "35px"
  vitals-health:
    backgroundColor: "{colors.panel-ink}"
    textColor: "{colors.vitals-text}"
    typography: "{typography.vitals-health-label}"
  vitals-hunger:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.panel-ink}"
    height: "34px"
---

# SOLITUDE design system

## Overview

SOLITUDE uses restrained shipboard instruments. The exploration HUD stays close to the edges of the world. Inventory opens a larger work surface with a carried grid, equipped items, and the detail and actions for the inspected item.

This reference describes the implemented Scrubber Vitals and Inventory. The broader project UI guide covers map, ship systems, notifications, and AI analysis. `@Assets/_Project/UI/README.md`

## Colors

Near-black panel ink, cool blue surfaces, and fine edge lines group related information. A brighter rim defines the top of the Vitals panel and Inventory header. Inset rules are quieter and mark the smaller equipment cards and capacity counter.

Amber marks inventory selection and input focus, and it accompanies the named hunger warning. Mint identifies health. Periwinkle identifies oxygen. The exploration HUD uses a warning symbol and label alongside amber. The current palette values are extracted from Unity authoring and runtime code. `@Assets/_Project/UI/Theme/ShipboardUiColors.cs` `@Assets/_Project/Editor/ExplorationVitalsHudAuthoring.cs`

## Typography

Exo 2 connects both surfaces. Inventory uses the regular TextMeshPro asset for the section headings, descriptions, and actions. The title and item names use larger type. The Vitals HUD uses the ExtraBold TextMeshPro asset for short labels that must read at a glance. Sizes are authored at the 1920 × 1080 reference size.

## Layout

Inventory uses a centered 1520 × 666 panel at the reference size. Equipment and Quick Use sit to the left, the default 24 carried slots sit in a six-column grid in the middle, and the selected item and its actions sit to the right. The live capacity counter sits beside Carried. The upper-left Vitals HUD keeps health above oxygen, with a banner below when hunger is active.

## Elevation & Depth

Flat surfaces separate through value and edge contrast. An ink header and equipment cards sit against the blue inventory panel. The world dimmer establishes the modal boundary. The selected item preview uses a fine registration grid and corner marks behind the sprite.

## Shapes

Cut the upper-right corner of informational surfaces. The inventory header uses a 24-unit cut, equipment cards use 15 units, and the capacity counter uses 10. The health housing uses a 22-unit upper cut and a slanted lower edge. Health segments and the oxygen and hunger strips repeat that diagonal. Carried cells keep square edges so the grid scans cleanly.

## Components

- **Vitals HUD.** Four angled health segments and a separate oxygen strip present current values. The Hunger banner adds a warning symbol and text. `@Assets/_Project/Features/Player/Vitals/Views/ExplorationVitalsHud.prefab`
- **Equipment card.** Weapon and Tool share an icon area, item label, and optional In hand status. Quick Use uses the same visual structure below a divider. Empty slots show their role and Empty.
- **Carried slot and capacity.** Each cell shows the item sprite and name, plus quantity for stacks. Selection uses amber, while corner marks identify navigation focus. The capacity counter shows occupied and available slots, with overflow called out when present.
- **Inventory action.** Quiet blue rows sit below the item description. The focused row fills with amber and uses dark text. Close reflects the active input device. Choosing Move adds the destination prompt and changes Close to Cancel move. `@Assets/_Project/Systems/Containers/Prefabs/InventoryScreen.prefab`

## Do's and Don'ts

- Do preserve the distinction between the inspected item, the item in hand, and the currently focused control.
- Do use single-purpose feedback adjacent to the action it explains.
- Do keep the carried grid visually quiet so item silhouettes and selection marks read first.
- Don't add an always-visible navigation legend to this Inventory.
- Don't use the health or oxygen colors as general inventory accents.
