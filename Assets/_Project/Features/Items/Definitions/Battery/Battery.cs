using UnityEngine;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Containers;
using SOLITUDE.Player;

namespace SOLITUDE.Items
{
    public class Battery : InteractableItem
    {
        public override string SuccessMessage()
        {
            return "Picked up battery";
        }
    }
}