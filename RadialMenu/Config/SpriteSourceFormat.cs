namespace RadialMenu.Config;

/// <summary>
/// Sources from which the sprite for a custom menu item can be taken.
/// </summary>
public enum SpriteSourceFormat
{
    /// <summary>
    /// Use the icon of an existing in-game item.
    /// </summary>
    ItemIcon,
    /// <summary>
    /// Draw an arbitrary area from any texture, tile sheet, etc.
    /// </summary>
    TextureRect,
}
