// Holds mod state that needs to be instanced per player.

using RadialMenu.Menus;
using StardewValley;

namespace RadialMenu;

internal record PreMenuState(bool WasFrozen);

internal class PlayerState(Cursor cursor, InventoryMenu inventoryMenu, CustomMenu customMenu)
{
    private readonly Cursor cursor = cursor;
    private readonly InventoryMenu inventoryMenu = inventoryMenu;
    private readonly CustomMenu customMenu = customMenu;

    public Cursor Cursor => cursor;
    public PreMenuState PreMenuState { get; set; } = new(Game1.freezeControls);
    public IRadialMenu? ActiveMenu { get; set; }
    public IRadialMenuPage? ActivePage => ActiveMenu?.GetSelectedPage();
    public int MenuOffset { get; set; }
    public Func<DelayedActions, MenuItemActivationResult>? PendingActivation { get; set; }
    // Track delay state so we don't keep trying to activate the item.
    public bool IsActivationDelayed { get; set; }
    public double RemainingActivationDelayMs { get; set; }

    public void InvalidateConfiguration()
    {
        customMenu.RebuildShortcutPage();
        // We consider inventory to be invalidated as well because the page size may have changed.
        InvalidateInventory();
    }

    public void InvalidateInventory()
    {
        inventoryMenu.Invalidate();
    }

    public void SetActiveMenu(MenuKind? kind, bool keepPreviousPage)
    {
        ActiveMenu = kind switch
        {
            MenuKind.Inventory => inventoryMenu,
            MenuKind.Custom => customMenu,
            _ => null,
        };
        if (!keepPreviousPage)
        {
            ActiveMenu?.ResetSelectedPage();
        }
    }
}
