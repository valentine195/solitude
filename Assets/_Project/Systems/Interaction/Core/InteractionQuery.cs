using UnityEngine;

namespace SOLITUDE.Core.Interaction
{
    public static class InteractionQuery
    {
        public static IInteractable GetBestInteractable(
            Vector2 position,
            float range,
            LayerMask mask)
        {
            var hits = Physics2D.OverlapCircleAll(position, range, mask.value);
            int count = hits.Length;


            IInteractable best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var col = hits[i];
                var interactable = col.GetComponent<IInteractable>();
                if (interactable == null) continue;

                float dist = Vector3.Distance(position, col.transform.position);

                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = interactable;
                }
            }

            return best;
        }
    }
}