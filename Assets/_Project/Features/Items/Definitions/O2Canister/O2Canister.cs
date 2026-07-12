using UnityEngine;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Containers;
using SOLITUDE.Player;

namespace SOLITUDE.Items
{
    public class O2Canister : InteractableItem
    {
        public override string SuccessMessage()
        {
            return "Picked up O2 Canister";
        }
    }
}