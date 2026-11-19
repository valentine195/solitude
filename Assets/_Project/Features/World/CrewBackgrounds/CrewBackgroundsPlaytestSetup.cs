using System;

namespace SOLITUDE.World.CrewBackgrounds
{
    public enum CrewBackground { Engineer, Technician }
    public enum CrewProficiency { Electrical, FieldRepair }
    public enum CrewProficiencyRank { Basic, Trained, Specialist }
    public enum CrewBackgroundsFault { LossOfPower, BrokenIntakeLinkage }

    public readonly struct CrewBackgroundsPlaytestSetup
    {
        public const string ScenePath = "Assets/_Project/Scenes/Demos/CrewBackgroundsMaintenance.unity";

        public CrewBackground Background { get; }
        public CrewBackgroundsFault Fault { get; }

        public CrewBackgroundsPlaytestSetup(CrewBackground background, CrewBackgroundsFault fault)
        {
            if (!Enum.IsDefined(typeof(CrewBackground), background))
                throw new ArgumentOutOfRangeException(nameof(background), background, "Unknown crew background.");
            if (!Enum.IsDefined(typeof(CrewBackgroundsFault), fault))
                throw new ArgumentOutOfRangeException(nameof(fault), fault, "Unknown scrubber fault.");

            Background = background;
            Fault = fault;
        }

        public CrewProficiencyRank Rank(CrewProficiency skill)
        {
            switch (skill)
            {
                case CrewProficiency.Electrical:
                    return Background == CrewBackground.Engineer ? CrewProficiencyRank.Specialist : CrewProficiencyRank.Trained;
                case CrewProficiency.FieldRepair:
                    return Background == CrewBackground.Technician ? CrewProficiencyRank.Specialist : CrewProficiencyRank.Trained;
                default:
                    return CrewProficiencyRank.Basic;
            }
        }

#if UNITY_EDITOR
        public static Func<string, CrewBackgroundsPlaytestSetup?> ConsumeEditorLaunch { private get; set; }
#endif

        internal static CrewBackgroundsPlaytestSetup Resolve(string scenePath, CrewBackgroundsPlaytestSetup authored)
        {
#if UNITY_EDITOR
            return ConsumeEditorLaunch?.Invoke(scenePath) ?? authored;
#else
            return authored;
#endif
        }
    }
}
