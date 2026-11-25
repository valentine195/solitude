using SOLITUDE.Application;
using SOLITUDE.Items;
using UnityEngine;
namespace SOLITUDE.Containers
{
    [RequireComponent(typeof(ContainerView))]
    public class ContainerController : MonoBehaviour
    {
        [SerializeField] private ContainerView view;
        [SerializeField] private ContainerTooltipView tooltip;
        [SerializeField] private MonoBehaviour defaultContainerSource;
        private ContainerPresenter presenter;
        private ContainerGestureCoordinator gestures;
        private InputPolicyCoordinator policy;
        private HotbarSelectionState selection;
        private IContainerSource source;
        public IContainerSource CurrentSource => source;
        public IContainerSource DefaultSource => defaultContainerSource as IContainerSource;
        public void ValidateAuthoring()
        {
            var endpoint = view != null ? view : GetComponent<ContainerView>();
            if (endpoint == null) throw new System.InvalidOperationException("Container display is missing.");
            endpoint.ValidateAuthoring();
        }
        public void Initialize(ItemPresentationCatalog catalog, ContainerGestureCoordinator gestures, InputPolicyCoordinator policy, HotbarSelectionState selection = null)
        {
            Unbind(); this.gestures = gestures; this.policy = policy; this.selection = selection;
            if (view == null) view = GetComponent<ContainerView>();
            view.Initialize(catalog); tooltip?.Initialize(catalog);
        }
        public void Bind(IContainerSource source)
        {
            if (ReferenceEquals(this.source, source) && presenter != null) return;
            Unbind();
            if (source?.Container == null || !source.Container.IsAvailable || gestures == null) throw new System.InvalidOperationException("Container panel is not composed.");
            this.source = source;
            presenter = new ContainerPresenter(source.Container, view, source.Label, gestures, policy, tooltip, selection);
        }
        public bool BindDefaultSource() { if (DefaultSource == null) return false; Bind(DefaultSource); return true; }
        public void Unbind() { presenter?.Dispose(); presenter = null; source = null; }
        private void OnDisable() => Unbind();
        private void OnDestroy() => Unbind();
    }
}
