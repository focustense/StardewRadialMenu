using StardewValley;
using StardewValley.Locations;
using SObject = StardewValley.Object;

namespace RadialMenu.Menus;

/// <summary>
/// Utility class for dealing with the vagaries of vanilla item activation.
/// </summary>
internal static class FuzzyActivation
{
    /// <summary>
    /// Attempts to either consume/use or select/hold an item depending on both the requested action and the item's own
    /// in-game behavior.
    /// </summary>
    /// <remarks>
    /// Determining the correct path, particularly while taking into account desired menu delays, can be slippery because
    /// we don't know what an item will do until we actually try to do it (<see cref="SObject.performUseAction"/>, plus our
    /// various custom quick-actions). This abstracts most of the messiness away so that it fits somewhat neatly into the
    /// activation API.
    /// </remarks>
    /// <param name="who">Player who has the item in inventory and wants to use/select it.</param>
    /// <param name="item">The item to use/select.</param>
    /// <param name="delayedActions">Current activation delay settings.</param>
    /// <param name="preferredAction">The desired action, based on which button pressed, and assuming that action is
    /// possible for the given item.</param>
    /// <returns>The actual action that was performed.</returns>
    public static MenuItemActivationResult ConsumeOrSelect(
        Farmer who,
        Item item,
        DelayedActions? delayedActions = null,
        MenuItemAction preferredAction = MenuItemAction.Use)
    {
        if (delayedActions == DelayedActions.All)
        {
            return MenuItemActivationResult.Delayed;
        }
        if (item is null || preferredAction == MenuItemAction.Use && TryConsume(item))
        {
            return MenuItemActivationResult.Used;
        }
        if (delayedActions == DelayedActions.ToolSwitch)
        {
            return MenuItemActivationResult.Delayed;
        }
        Game1.player.CurrentToolIndex = who.Items.IndexOf(item);
        if (Game1.player.CurrentTool is not null)
        {
            Game1.playSound("toolSwap");
        }
        return MenuItemActivationResult.Selected;
    }

    private static bool TryConsume(Item item)
    {
        if (item.Name == "Staircase" && Game1.currentLocation is MineShaft mineShaft)
        {
            ReduceStack(item);
            Game1.enterMine(mineShaft.mineLevel + 1);
            Game1.playSound("stairsdown");
            return true;
        }
        if (item is not SObject obj)
        {
            return false;
        }
        if (obj.Edibility > 0)
        {
            ReduceStack(obj);
            Game1.player.eatObject(obj);
            return true;
        }
        if (obj.performUseAction(Game1.currentLocation))
        {
            ReduceStack(obj);
            return true;
        }
        return false;
    }

    private static void ReduceStack(Item item)
    {
        if (item.Stack > 1)
        {
            item.Stack--;
        }
        else
        {
            Game1.player.Items.RemoveButKeepEmptySlot(item);
        }
    }
}
