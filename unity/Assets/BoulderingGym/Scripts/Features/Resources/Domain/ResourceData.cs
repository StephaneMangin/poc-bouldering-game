namespace Project.Features.Resources.Domain
{
    public enum ClimbingAction
    {
        Grab,
        Reposition,
        Dyno,
    }

    public enum FatigueLevel
    {
        Normal,
        Warning,
        Critical,
        Failure,
    }

    public struct ResourceSnapshot
    {
        public float Endurance;
        public float EnduranceMax;
        public float EnduranceRatio;
        public float Grip;
        public float GripMax;
        public float GripRatio;
        public float LeftArmFatigue;
        public float RightArmFatigue;
        public bool ChalkAvailable;
        public float ChalkCooldownRemaining;
    }
}
