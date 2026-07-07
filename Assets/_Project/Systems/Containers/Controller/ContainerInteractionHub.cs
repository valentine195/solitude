using UnityEngine;

namespace SOLITUDE.Containers
{
    /// <summary>
    /// Single shared interaction state for every container UI in the scene.
    /// Without this, each ContainerController would own its own held-item
    /// state, making it impossible to pick an item up in one container (e.g.
    /// the player's inventory) and place it in another (e.g. an open chest) -
    /// there must be exactly one "held item" at a time, not one per container.
    ///
    /// Place a single instance of this somewhere persistent in your scene
    /// (e.g. on your UI root). Every ContainerController fetches Interaction
    /// from here instead of constructing its own.
    ///
    /// Instance/Service/Interaction are all lazily resolved rather than only
    /// being set in Awake() - Unity does not guarantee that this object's
    /// Awake() runs before another object's OnEnable() elsewhere in the
    /// scene, so anything that only worked "if Hub.Awake() already ran" would
    /// be a silent, order-dependent bug (this is exactly what was happening).
    ///
    /// This Hub also owns the single shared ContainerInputBridge and wires
    /// its Cancel event directly to Interaction.Cancel(). Cancel used to be
    /// routed through whichever ContainerController happened to have its own
    /// input bridge enabled - which meant closing every open container
    /// window mid-drag left no input path able to cancel the hold at all,
    /// stranding the held item on the cursor with nowhere to drop it.
    /// Routing Cancel through the Hub itself means it always works,
    /// regardless of which - or whether any - container window is open.
    /// </summary>
    public class ContainerInteractionHub : MonoBehaviour
    {
        private static ContainerInteractionHub instance;

        public static ContainerInteractionHub Instance
        {
            get
            {
                if (instance == null)
                {
                    // Works even if this Hub's own Awake() hasn't run yet -
                    // FindFirstObjectByType only needs the component to exist
                    // in the scene, not to have executed Awake().
                    instance = FindAnyObjectByType<ContainerInteractionHub>();
                }

                return instance;
            }
        }

        private ContainerService service;
        public ContainerService Service => service ??= new ContainerService();

        private ContainerInteractionController interaction;
        public ContainerInteractionController Interaction => interaction ??= new ContainerInteractionController(Service);

        private ContainerInputBridge input;
        public ContainerInputBridge Input => input ??= new ContainerInputBridge();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning(
                    $"Multiple {nameof(ContainerInteractionHub)} instances found in the scene; destroying the extra one.",
                    this);
                Destroy(gameObject);
                return;
            }

            instance = this;

            // Touch Interaction now so it exists immediately rather than on
            // first access - purely for predictability, not required for
            // correctness since the properties above are lazy either way.
            _ = Interaction;

            Input.Cancel += OnGlobalCancel;
            Input.Enable();
        }

        private void OnDestroy()
        {
            if (instance != this) return;

            Input.Cancel -= OnGlobalCancel;
            Input.Disable();

            instance = null;
        }

        private void OnGlobalCancel() => Interaction.Cancel();

        // Defensive reset for Unity's "Enter Play Mode without domain
        // reload" option - without this, `instance` could survive between
        // play sessions in the editor and point at a destroyed object from
        // the previous session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }
    }
}
