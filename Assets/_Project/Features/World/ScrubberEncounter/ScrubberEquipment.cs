using SOLITUDE.Restoration;
using UnityEngine;

namespace SOLITUDE.World.ScrubberEncounter
{
    public sealed class ScrubberEquipment : MonoBehaviour
    {
        private readonly ScrubberEncounterState state = new ScrubberEncounterState();
        public ScrubberEncounterState State => state;
    }
}
