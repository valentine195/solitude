using System;
using System.Linq;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SOLITUDE.Player.Succession
{
    public sealed class SurvivorRecoveryContainer : InteractableBase, IContainerSource
    {
        [SerializeField] private InteractableProximitySensor sensor;
        [SerializeField] private SpriteRenderer presentation;
        private SceneFeatureContext context;
        public IContainerReader Container { get; private set; }
        public Guid SourceSurvivorId { get; private set; }
        public Vector3 DeathPosition { get; private set; }
        public string Label => "Recovered equipment";
        public bool IsPublished { get; private set; }

        public void Configure(InteractableProximitySensor sensor, SpriteRenderer presentation)
        { this.sensor = sensor; this.presentation = presentation; }

        public void ValidateAuthoring()
        {
            if (sensor == null || !sensor.transform.IsChildOf(transform) || !sensor.gameObject.activeSelf ||
                !sensor.enabled || sensor.GetComponentInParent<IInteractable>(true) != (IInteractable)this ||
                !sensor.GetComponents<Collider2D>().Any(c => c.enabled && c.isTrigger))
                throw new InvalidOperationException("Recovery proximity sensor is missing or invalid.");
            if (presentation == null || !presentation.transform.IsChildOf(transform) || !presentation.enabled ||
                !presentation.gameObject.activeSelf || presentation.sprite == null)
                throw new InvalidOperationException("Recovery presentation is missing or invalid.");
            if (GetComponentsInChildren<PlayerInteractor>(true).Length != 0 ||
                GetComponentsInChildren<SaveableId>(true).Length != 0 ||
                GetComponentsInChildren<SOLITUDE.Features.Interactables.LockerContainer>(true).Length != 0)
                throw new InvalidOperationException("Recovery cannot contain a player or saveable locker owner.");
        }

        public static SurvivorRecoveryContainer Prepare(SurvivorRecoveryContainer prefab, SceneFeatureContext context,
            Guid sourceSurvivorId, Vector3 position, int capacity)
        {
            if (prefab == null || prefab.gameObject.activeSelf || context?.Commands == null || context.Commands.IsBusy ||
                context.OpenContainer == null || context.CloseContainer == null || context.Interactor == null ||
                sourceSurvivorId == Guid.Empty || capacity < 1 || !Finite(position))
                throw new InvalidOperationException("Recovery preparation prerequisites are unavailable.");
            prefab.ValidateAuthoring();
            SurvivorRecoveryContainer instance = null;
            try
            {
                instance = Instantiate(prefab, position, Quaternion.identity);
                SceneManager.MoveGameObjectToScene(instance.gameObject, context.Interactor.gameObject.scene);
                instance.ValidateAuthoring();
                instance.context = context; instance.SourceSurvivorId = sourceSurvivorId; instance.DeathPosition = position;
                instance.Container = context.Commands.Register("survivor.recovery." + Guid.NewGuid().ToString("N"), capacity);
                instance.sensor.OnLeave += instance.Left;
                return instance;
            }
            catch
            {
                if (instance != null) { instance.Release(); Destroy(instance.gameObject); }
                throw;
            }
        }
        private static bool Finite(Vector3 p) => !float.IsNaN(p.x) && !float.IsInfinity(p.x) &&
            !float.IsNaN(p.y) && !float.IsInfinity(p.y) && !float.IsNaN(p.z) && !float.IsInfinity(p.z);

        // Prepared ownership remains invisible until the equipment transaction has completed.
        public void Publish()
        {
            if (Container?.IsAvailable != true || context == null || context.Commands.IsBusy ||
                !Container.GetSlots().Any(s => !s.IsEmpty))
                throw new InvalidOperationException("Recovery contents are not ready for publication.");
            ValidateAuthoring(); IsPublished = true; gameObject.SetActive(true);
        }
        public override string GetPrompt() => "Recover equipment";
        public override bool CanInteract(PlayerInteractor player) => IsPublished && isActiveAndEnabled &&
            Container?.IsAvailable == true && context != null && player == context.Interactor;
        public override InteractionResult Interact(PlayerInteractor player) =>
            CanInteract(player) && context.OpenContainer(Container, Label) ? InteractionResult.Success() :
                InteractionResult.Blocked("Recovery unavailable");
        private void Left(bool left) { if (left && Container != null) context?.CloseContainer(Container.Handle); }
        private void OnDisable()
        {
            if (Container != null) context?.CloseContainer(Container.Handle);
            context?.Interactor.Unregister(this);
        }
        public void Release()
        {
            IsPublished = false;
            if (sensor != null) sensor.OnLeave -= Left;
            if (Container != null)
            {
                context?.CloseContainer(Container.Handle);
                context?.Commands.Unregister(Container.Handle);
            }
            // Release can precede Unity's deferred destruction and trigger callbacks.
            context?.Interactor.Unregister(this);
            Container = null; context = null;
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }
        private void OnDestroy() => Release();
    }
}
