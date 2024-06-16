namespace RadialMenu.Config;

internal interface ICursorConfiguration
{
    public float TriggerDeadZone { get; }
    public bool SwapTriggers { get; }
    public ThumbStickPreference ThumbStickPreference { get; }
    public float ThumbStickDeadZone { get;}
}
