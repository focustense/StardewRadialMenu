namespace RadialMenu.Menus;

/// <summary>
/// The result of activating a menu item in a radial menu.
/// </summary>
public enum MenuItemActivationResult
{
    /// <summary>
    /// The activation was ignored, i.e. nothing happened.
    /// </summary>
    /// <remarks>
    /// This is normally only used internally to indicate that something went unexpectedly wrong. Actions that were
    /// understood, but had no effect, should use <see cref="Custom"/> instead.
    /// </remarks>
    Ignored = -1,

    /// <summary>
    /// An immediate action/effect was triggered, such as eating a food item, using a totem, etc.
    /// </summary>
    /// <remarks>
    /// This is the normal result when an item is activated with <see cref="MenuItemAction.Use"/>, <b>and</b> the item has
    /// some useful "quick action" that's meant to be triggered from the menu directly. If no such action is possible
    /// (e.g. inedible materials or tools like axe or hoe), or the requested action was <see cref="MenuItemAction.Select"/>,
    /// then one of <see cref="Delayed"/> or <see cref="Selected"/> should be used instead.
    /// </remarks>
    Used,

    /// <summary>
    /// Specifies that the real action, which will yield one of the other result types, should happen after a
    /// confirmation delay.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Confirmation delays normally are - and generally should be - used for any actions that immediately return
    /// control to the player; actions that <b>do not</b> open a menu, play an animation, fade to black, or trigger any
    /// other effect that would cause <see cref="StardewModdingAPI.Context.CanPlayerMove"/> to evaluate to <c>false</c>.
    /// </para>
    /// <para>
    /// Delays provide a brief window for the player to release the buttons that were used to select/activate the menu
    /// item and avoid having them become stray inputs into the game world, possibly making the player face/move in a
    /// different direction or use a held item or tool. During the delay period, the window stays open and the selected
    /// slice blinks, confirming the player's selection before performing the action.
    /// </para>
    /// <para>
    /// Items that opt into delays should check the <see cref="DelayedActions"/> in the request. If it has a value
    /// <b>other than</b> <see cref="DelayedActions.None"/>, and in particular one that is applicable to the item that
    /// was selected, then the item should return this result <em>and not perform the associated action</em>. Once the
    /// delay expires, a second activation request will be sent with <see cref="DelayedActions.None"/> to trigger the
    /// real action.
    /// </para>
    /// </remarks>
    Delayed,

    /// <summary>
    /// Indicates that an item became the selected item in its corresponding menu.
    /// </summary>
    /// <remarks>
    /// This is primarily used in the inventory menu to signal tool selection, so that the active backpack page can be
    /// updated in response. For the secondary/custom menu, the exact behavior depends on user settings, specifically
    /// <see cref="Config.Configuration.RememberSelection"/>. Non-inventory menus/items are <b>not</b> required to
    /// implement their own selection behavior, but if it is used, then the corresponding <see cref="IRadialMenuPage"/> must
    /// have a consistent <see cref="IRadialMenuPage.SelectedItemIndex"/> value.
    /// </remarks>
    Selected,

    /// <summary>
    /// A special kind of action was performed that has no immediate or delayed effect on the player or game world, such
    /// as opening a menu.
    /// </summary>
    /// <remarks>
    /// The behavior for this result is generally the same as <see cref="Used"/>, but mods should distinguish between
    /// them when possible to allow for future enhancements.
    /// </remarks>
    Custom,
}
