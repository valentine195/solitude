using UnityEngine;
using TMPro;

namespace SOLITUDE.Containers
{
    /// <summary>
    /// Simple hover tooltip showing an item's name and description.
    /// Assumes it lives under a Screen Space - Overlay canvas, where a
    /// RectTransform's world position maps 1:1 to screen pixels. If you
    /// switch to Screen Space - Camera or World Space, SetPosition will
    /// need to convert via RectTransformUtility.ScreenPointToLocalPointInRectangle
    /// against that canvas instead.
    /// </summary>
    public class ContainerTooltipView : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI description;

        [Tooltip("Offset from the cursor so the tooltip doesn't sit under the pointer.")]
        [SerializeField] private Vector2 cursorOffset = new Vector2(16f, -16f);

        private void Awake()
        {
            Hide();
        }

        public void Show(ItemDefinition item)
        {
            if (item == null)
            {
                Hide();
                return;
            }

            if (title != null) title.text = item.DisplayName;
            if (description != null) description.text = item.Description;

            root.gameObject.SetActive(true);
        }

        public void Hide()
        {
            root.gameObject.SetActive(false);
        }

        public void SetPosition(Vector2 screenPosition)
        {
            root.position = screenPosition + cursorOffset;
        }
    }
}