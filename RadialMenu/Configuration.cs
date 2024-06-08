namespace RadialMenu
{
    public enum ThumbStickPreference { AlwaysLeft, AlwaysRight, SameAsTrigger };

    public class Configuration
    {
        public float TriggerDeadZone { get; set; } = 0.2f;
        public ThumbStickPreference ThumbStickPreference { get; set; }
        public float ThumbStickDeadZone { get; set; } = 0.2f;
    }
}
