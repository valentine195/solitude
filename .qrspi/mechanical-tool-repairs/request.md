# Contextual mechanical repairs

Allow the player to repair a damaged mechanical panel using a compatible tool and a replacement part. Begin with one panel and the maintenance wrench. Tool compatibility should use gameplay tags, including Tool.Mechanical, so other compatible items can support the same interaction later.

Repair is contextual interaction: interacting with the panel repairs it when the player has the necessary tool and part; the primary weapon action continues to swing the wrench. The tool remains available after repair, while the required replacement part is used. Missing requirements should be explained clearly.

Repairing the panel restores a ship system or enables access through a door, and the restored state should survive saving and loading. Reuse the project's item, interaction, restoration and persistence infrastructure wherever appropriate. Keep this first feature small and focused on making tools useful outside combat.

## Clarifications

1. For the first repair, should Research identify an existing panel and replacement part, consuming one part per repair, or do you have specific choices?
   Answer: Reuse an existing panel and one part
2. What should the first successful repair do?
   Answer: Unlock access through a door
