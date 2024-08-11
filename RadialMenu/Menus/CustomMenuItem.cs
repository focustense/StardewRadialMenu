using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace RadialMenu.Menus;

/// <summary>
/// An immutable menu item with user-defined properties.
/// </summary>
/// <param name="title">The <see cref="IRadialMenuItem.Title"/>.</param>
/// <param name="activate">A delegate for the <see cref="IRadialMenuItem.Activate"/> method.</param>
/// <param name="description">The <see cref="IRadialMenuItem.Description"/>.</param>
/// <param name="stackSize">The <see cref="IRadialMenuItem.StackSize"/>.</param>
/// <param name="quality">The <see cref="IRadialMenuItem.Quality"/>.</param>
/// <param name="texture">The <see cref="IRadialMenuItem.Texture"/>.</param>
/// <param name="sourceRectangle">The <see cref="IRadialMenuItem.SourceRectangle"/>.</param>
/// <param name="tintRectangle">The <see cref="IRadialMenuItem.TintRectangle"/>.</param>
/// <param name="tintColor">The <see cref="IRadialMenuItem.TintColor"/>.</param>
internal class CustomMenuItem(
    string title,
    Func<Farmer, DelayedActions, MenuItemAction, MenuItemActivationResult> activate,
    string? description = null,
    int? stackSize = null,
    int? quality = null,
    Texture2D? texture = null,
    Rectangle? sourceRectangle = null,
    Rectangle? tintRectangle = null,
    Color? tintColor = null) : IRadialMenuItem
{
    public string Title { get; } = title;

    public string Description { get; } = description ?? "";

    public int? StackSize { get; } = stackSize;

    public int? Quality { get; } = quality;

    public Texture2D? Texture { get; } = texture;

    public Rectangle? SourceRectangle { get; } = sourceRectangle;

    public Rectangle? TintRectangle { get; } = tintRectangle;

    public Color? TintColor { get; } = tintColor;

    public MenuItemActivationResult Activate(Farmer who, DelayedActions delayedActions, MenuItemAction requestedAction)
    {
        return activate(who, delayedActions, requestedAction);
    }
}