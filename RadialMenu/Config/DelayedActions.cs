namespace RadialMenu.Config;

/// <summary>
/// The types of actions that can be delayed when selecting from the radial menu.
/// </summary>
public enum DelayedActions
{
    /// <summary>
    /// Delay for all items activated via the menu.
    /// </summary>
    All,
    /// <summary>
    /// Only delay when switching tools, which has no in-game animation.
    /// </summary>
    ToolSwitch,
}
