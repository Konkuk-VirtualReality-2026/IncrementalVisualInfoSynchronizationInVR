namespace VRAdaptation.Experiment
{
    public enum ExperimentGroup { NotSelected, Control, Adaptation }

    public static class ExperimentCondition
    {
        public static ExperimentGroup SelectedGroup = ExperimentGroup.NotSelected;
        public static string ParticipantID = "";

        public static string GetConditionName() =>
            SelectedGroup == ExperimentGroup.Control ? "Control" : "PostAdaptation";
    }
}
