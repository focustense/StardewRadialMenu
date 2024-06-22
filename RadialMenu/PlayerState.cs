// Holds mod state that needs to be instanced per player.

using RadialMenu.Config;
using StardewValley;

namespace RadialMenu;

internal record PreMenuState(bool WasFrozen);

internal class PlayerState(Cursor cursor)
{
    private readonly Cursor cursor = cursor;

    public Cursor Cursor => cursor;
    public PreMenuState PreMenuState { get; set; } = new(Game1.freezeControls);
    public IReadOnlyList<MenuItem> ActiveMenuItems { get; set; } = [];
    public Func<DelayedActions?, ItemActivationResult>? PendingActivation { get; set; }
    // Track delay state so we don't keep trying to activate the item.
    public bool IsActivationDelayed { get; set; }
    public double RemainingActivationDelayMs { get; set; }
}
