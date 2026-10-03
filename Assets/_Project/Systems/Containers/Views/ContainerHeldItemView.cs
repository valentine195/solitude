using SOLITUDE.Application;
using SOLITUDE.Items;
using UnityEngine;
using TMPro;
namespace SOLITUDE.Containers
{
    public class ContainerHeldItemView : MonoBehaviour, IHeldItemDisplay
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private UnityEngine.UI.Image icon;
        [SerializeField] private TextMeshProUGUI count;
        [SerializeField] private Vector2 cursorOffset = Vector2.zero;
        private ItemPresentationCatalog catalog;
        public void ValidateAuthoring()
        { if (root == null || icon == null) throw new System.InvalidOperationException("Held item display root/icon is missing."); }
        public void Initialize(ItemPresentationCatalog catalog)
        {
            this.catalog = catalog;
            var group = root.GetComponent<CanvasGroup>(); if (group == null) group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false; group.interactable = false; Render(null, 0);
        }
        public void Render(string id, int quantity)
        {
            if (icon != null) { icon.enabled = id != null; icon.sprite = catalog?.Resolve(id)?.Icon; }
            if (count != null) count.text = quantity > 1 ? quantity.ToString() : string.Empty;
            if (root != null) root.gameObject.SetActive(id != null);
        }
        public void Point(float x, float y) { if (root != null) root.position = new Vector2(x, y) + cursorOffset; }
    }
}
