using System;
using System.Collections.Generic;
using System.Linq;
using SOLITUDE.Core.Events;
using SOLITUDE.Core.UI;
using SOLITUDE.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SOLITUDE.Core.Interaction
{
    // SceneBindings owns composed scenes; SceneInteractionBindings owns older standalone scenes.
    public sealed class SceneInteractionScope : IDisposable
    {
        private PlayerInteractor player;
        private UIController ui;
        private readonly List<IInteractionFeedbackReceiver> receivers = new();
        public SceneInteractionEvents Events { get; private set; }
        public InteractionFeedbackService Feedback { get; private set; }

        private static T[] Endpoints<T>(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).OfType<T>().ToArray();
        public static void Validate(Scene scene, PlayerInteractor interactor)
        {
            var players = Endpoints<PlayerInteractor>(scene);
            var views = Endpoints<UIController>(scene);
            if (interactor == null || interactor.gameObject.scene != scene || players.Length != 1 || players[0] != interactor)
                throw new InvalidOperationException("Interaction scope requires exactly one scene-local player interactor.");
            if (views.Length != 1) throw new InvalidOperationException("Interaction scope requires exactly one scene-local UI controller.");
            views[0].ValidateAuthoring();
        }
        public void Initialize(Scene scene, PlayerInteractor interactor)
        {
            Dispose(); Validate(scene, interactor);
            Events = new SceneInteractionEvents();
            player = interactor; ui = Endpoints<UIController>(scene)[0];
            Feedback = new InteractionFeedbackService(ui.FeedbackView); Feedback.Clear();
            try
            {
                ui.Bind(Events, Feedback); player.Bind(Events, Feedback);
                foreach (var receiver in Endpoints<IInteractionFeedbackReceiver>(scene))
                {
                    receivers.Add(receiver); receiver.BindFeedback(Feedback);
                }
            }
            catch { Dispose(); throw; }
        }
        public void Dispose()
        {
            if (player != null) player.Release(); player = null;
            if (ui != null) ui.Release(); ui = null;
            foreach (var receiver in receivers)
                if (!(receiver is UnityEngine.Object target) || target != null) receiver.ReleaseFeedback();
            receivers.Clear();
            Feedback?.Dispose(); Feedback = null;
            Events?.Dispose(); Events = null;
        }
    }
}
