using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace RadialMenu.Menus;

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
