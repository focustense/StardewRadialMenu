using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RadialMenu;

/// <summary>
/// Public API for mod integrations.
/// </summary>
public interface IRadialMenuApi
{
    /// <summary>
    /// Forces a previously-registered page to be recreated the next time the menu is about to be shown.
    /// </summary>
    /// <remarks>
    /// <para>
    /// While a custom <see cref="IRadialMenuPage"/> can always decide when to update its contents, sometimes this is
    /// not convenient; for example, if the contents of a menu page are generated from some other data (like player
    /// inventory) and cached, or if activation of an item in one page could indirectly affect the contents of a
    /// different page. Explicit invalidation allows each page to cache its data for performance but still remain up to
    /// date when conditions change.
    /// </para>
    /// <para>
    /// The <paramref name="mod"/> and <paramref name="id"/> must already have been registered through
    /// <see cref="RegisterCustomMenuPage"/>, otherwise the request will be ignored.
    /// </para>
    /// </remarks>
    /// <param name="mod">Manifest for the mod that registered the menu.</param>
    /// <param name="id">Unique (per mod) ID for the page to invalidate.</param>
    void InvalidatePage(IManifest mod, string id);

    /// <summary>
    /// Registers a new page to be made available in the Custom Menu (default: right trigger).
    /// </summary>
    /// <param name="mod">Manifest for the mod that will own the menu.</param>
    /// <param name="id">Unique (per mod) ID for the page. Registering a page with a previously-used ID will overwrite
    /// the previous page.</param>
    /// <param name="factory">Factory for creating the page.</param>
    void RegisterCustomMenuPage(IManifest mod, string id, IRadialMenuPageFactory factory);
}

/// <summary>
/// Factory for creating an <see cref="IRadialMenuPage"/> which adds mod-specific menu content.
/// </summary>
public interface IRadialMenuPageFactory
{
    /// <summary>
    /// Creates the page for a given player.
    /// </summary>
    /// <remarks>
    /// This method may be invoked multiple times in case of invalidation, either implicitly due to a config change in
    /// RadialMenu or explicitly by <see cref="IRadialMenuApi.InvalidateCustomMenuPage"/>. If page creation is an
    /// expensive process then callers are allowed to return a cached result, but if doing so, must ensure that the
    /// result is unique per player/screen.
    /// </remarks>
    /// <param name="who">The player for whom the page will be displayed. Callers should use this whenever possible
    /// instead of <see cref="Game1.player"/> in case of co-op play.</param>
    IRadialMenuPage CreatePage(Farmer who);
}

/// <summary>
/// A single page in one of the radial menus.
/// </summary>
/// <remarks>
/// Pages can be navigated using left/right shoulder buttons while a menu is open. Only the items on the
/// currently-active page are visible at any given time.
/// </remarks>
public interface IRadialMenuPage
{
    /// <summary>
    /// The items on this page.
    /// </summary>
    IReadOnlyList<IRadialMenuItem> Items { get; }

    /// <summary>
    /// Index of the selected <see cref="IRadialMenuItem"/> in the <see cref="Items"/> list, or <c>-1</c> if no selection.
    /// </summary>
    int SelectedItemIndex { get; }
}

/// <summary>
/// Describes a single item on an <see cref="IRadialMenuPage"/>.
/// </summary>
public interface IRadialMenuItem
{
    /// <summary>
    /// The item title, displayed in large text at the top of the info area when pointing to this item.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Additional description text displayed underneath the <see cref="Title"/>.
    /// </summary>
    /// <remarks>
    /// Can be used to display item stats, effect info or simply flavor text.
    /// </remarks>
    string Description { get; }

    /// <summary>
    /// The amount available.
    /// </summary>
    /// <remarks>
    /// For inventory, this is the actual <see cref="StardewValley.Item.Stack"/>. For other types of menu items, it can
    /// be used to indicate any "number of uses available". Any non-<c>null</c> value will render as digits at the
    /// bottom-right of the item icon/sprite in the menu.
    /// </remarks>
    int? StackSize => null;

    /// <summary>
    /// The item's quality, from 0 (base) to 3 (iridium).
    /// </summary>
    /// <remarks>
    /// For non-<c>null</c> values, the corresponding star will be drawn to the bottom-left of the icon.
    /// </remarks>
    int? Quality => null;

    /// <summary>
    /// The texture (sprite sheet) containing the item's icon to display in the menu.
    /// </summary>
    /// <remarks>
    /// If not specified, the icon area will instead display monogram text based on the <see cref="Title"/>.
    /// </remarks>
    Texture2D? Texture => null;

    /// <summary>
    /// The area within the <see cref="Texture"/> containing this specific item's icon/sprite that should be displayed
    /// in the menu.
    /// </summary>
    /// <remarks>
    /// If not specified, the entire <see cref="Texture"/> will be used.
    /// </remarks>
    Rectangle? SourceRectangle => null;

    /// <summary>
    /// Optional separate area within the <see cref="Texture"/> providing an overlay sprite to render with
    /// <see cref="TintColor"/>.
    /// </summary>
    /// <remarks>
    /// Some "colored items" define both a base sprite and a sparser, mostly-transparent tint/overlay sprite so that
    /// the tint can be applied to only specific regions. If this is set, then any <see cref="TintColor"/> will apply
    /// only to the overlay and <em>not</em> the base sprite contained in <see cref="SourceRectangle"/>.
    /// </remarks>
    Rectangle? TintRectangle => null;

    /// <summary>
    /// Tint color, if the item icon/sprite should be drawn in a specific color.
    /// </summary>
    /// <remarks>
    /// If <see cref="TintRectangle"/> is specified, this applies to the tintable region; otherwise, it applies directly
    /// to the base sprite in <see cref="SourceRectangle"/>.
    /// </remarks>
    Color? TintColor => null;

    /// <summary>
    /// Attempts to activate the menu item, i.e. perform its associated action.
    /// </summary>
    /// <param name="who">The player who activated the item; generally, <see cref="Game1.player"/>.</param>
    /// <param name="delayedActions">The types of actions which should result in a
    /// <see cref="MenuItemActivationResult.Delayed"/> outcome and the actual action being skipped.</param>
    /// <param name="requestedAction">The action type requested by the player, i.e. which button was pressed.</param>
    /// <returns>A result that describes what action, if any, was performed.</returns>
    MenuItemActivationResult Activate(Farmer who, DelayedActions delayedActions, MenuItemAction requestedAction);
}

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

    /// <summary>
    /// Never delay any menu item.
    /// </summary>
    /// <remarks>
    /// Typically only used as a transient state to indicate that the delay is done.
    /// </remarks>
    None,
}

/// <summary>
/// Types of actions that can be performed when activating an item from the menu.
/// </summary>
public enum MenuItemAction
{
    /// <summary>
    /// Always select the item, regardless of whether it can be consumed or used. Used for gifting items, placing them
    /// into machines, etc.
    /// </summary>
    Select,

    /// <summary>
    /// Try to consume or use the item, if it has that ability; otherwise, <see cref="Select"/> it.
    /// </summary>
    Use,
}

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
