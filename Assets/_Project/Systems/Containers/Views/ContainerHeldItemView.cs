using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace SOLITUDE.Containers
{
    /// <summary>
    /// Cursor-following icon for whatever item is currently held (see
    /// ContainerInteractionHub). Place a single instance of this at the top
    /// of your UI canvas, always active - it shows/hides itself based on
    /// ContainerInteractionHub.Instance.Interaction.HeldItemChanged, so it
    /// stays correct regardless of which container windows are open.
    ///
    /// Reads the pointer position directly (rather than depending on any one
    /// container's ContainerInputBridge) since the hold isn't tied to a
    /// single container.
    /// </summary>
    public class ContainerHeldItemView : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI count;
        [SerializeField] private Vector2 cursorOffset = Vector2.zero;

        private void Awake()
        {
            var canvasGroup = root.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = root.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            Hide();
        }

        private void OnEnable()
        {
            var hub = ContainerInteractionHub.Instance;
            if (hub == null)
            {
                Debug.LogError($"{nameof(ContainerHeldItemView)}: no {nameof(ContainerInteractionHub)} found in the scene.", this);
                return;
            }

            hub.Interaction.HeldItemChanged += OnHeldItemChanged;

            // Sync immediately in case something is already held when this
            // view becomes active (e.g. it was toggled off and back on).
            OnHeldItemChanged(hub.Interaction.HeldItem);
        }

        private void OnDisable()
        {
            if (ContainerInteractionHub.Instance != null)
                ContainerInteractionHub.Instance.Interaction.HeldItemChanged -= OnHeldItemChanged;
        }

        private void Update()
        {
            if (!root.gameObject.activeSelf) return;
            if (Mouse.current == null) return;

            root.position = (Vector2)Mouse.current.position.ReadValue() + cursorOffset;
        }

        private void OnHeldItemChanged(ItemStack item)
        {
            if (item == null || item.IsEmpty)
            {
                Hide();
                return;
            }

            if (icon != null)
            {
                icon.enabled = true;
                icon.sprite = item.Definition.Icon;
            }

            if (count != null)
            {
                count.text = item.Quantity > 1 ? item.Quantity.ToString() : string.Empty;
            }

            root.gameObject.SetActive(true);
        }

        private void Hide()
        {
            root.gameObject.SetActive(false);
        }
    }
}
