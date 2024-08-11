using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using System.Text;

namespace RadialMenu.Menus;

/// <summary>
/// A menu item that corresponds to an item in the player's inventory.
/// </summary>
/// <param name="item">The inventory item.</param>
internal class InventoryMenuItem : IRadialMenuItem
{
    /// <summary>
    /// The underlying inventory item.
    /// </summary>
    public Item Item { get; }

    public string Title { get; }

    public string Description { get; }

    public int? StackSize => Item.maximumStackSize() > 1 ? Item.Stack : null;

    public int? Quality => Item.Quality;

    public Texture2D? Texture { get; }

    public Rectangle? SourceRectangle { get; }

    public Rectangle? TintRectangle { get; }

    public Color? TintColor { get; }

    public InventoryMenuItem(Item item)
    {
        Item = item;
        Title = item.DisplayName;
        Description = UnparseText(item.getDescription());

        var data = ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
        var textureData = GetTextureRedirect(item);
        Texture = textureData?.GetTexture() ?? data.GetTexture();
        SourceRectangle = textureData?.GetSourceRect() ?? data.GetSourceRect();
        (TintRectangle, TintColor) = GetTinting(item, textureData ?? data);
    }

    public MenuItemActivationResult Activate(Farmer who, DelayedActions delayedActions, MenuItemAction requestedAction)
    {
        return FuzzyActivation.ConsumeOrSelect(who, Item, delayedActions, requestedAction);
    }

    private static ParsedItemData? GetTextureRedirect(Item item)
    {
        return item is StardewValley.Object obj && item.ItemId == "SmokedFish"
            ? ItemRegistry.GetData(obj.preservedParentSheetIndex.Value)
            : null;
    }

    private static (Rectangle? tintRect, Color? tintColor) GetTinting(
        Item item, ParsedItemData data)
    {
        if (item is not ColoredObject coloredObject)
        {
            return default;
        }
        if (item.ItemId == "SmokedFish")
        {
            // Smoked fish implementation is unique (and private) in ColoredObject.
            // We don't care about the animation here, but should draw it darkened; the quirky
            // way this is implemented is to draw a tinted version of the original item sprite
            // (not an overlay) sprite over top of the original sprite.
            return (data.GetSourceRect(), new Color(80, 30, 10) * 0.6f);
        }
        return !coloredObject.ColorSameIndexAsParentSheetIndex
            ? (data.GetSourceRect(1), coloredObject.color.Value)
            : (null, coloredObject.color.Value);
    }

    // When we call Item.getDescription(), most implementations go through `Game1.parseText`
    // which splits the string itself onto multiple lines. This tries to remove that, so that we
    // can do our own wrapping using our own width.
    //
    // N.B. The reason we don't just use `ParsedItemData.Description` is that, at least in the
    // current version, it's often only a "base description" and includes format placeholders,
    // or is missing suffixes.
    private static string UnparseText(string text)
    {
        var sb = new StringBuilder();
        var isWhitespace = false;
        var newlineCount = 0;
        foreach (var c in text)
        {
            if (c == ' ' || c == '\r' || c == '\n')
            {
                if (!isWhitespace)
                {
                    sb.Append(' ');
                }
                isWhitespace = true;
                if (c == '\n')
                {
                    newlineCount++;
                }
            }
            else
            {
                // If the original text has a "paragraph", the formatted text will often look
                // strange if that is also collapsed into a space. So preserve _multiple_
                // newlines somewhat as a single "paragraph break".
                if (newlineCount > 1)
                {
                    // From implementation above, newlines are counted as whitespace so we know
                    // that the last character will always be a space when hitting here.
                    sb.Length--;
                    sb.Append("\r\n\r\n");
                }
                sb.Append(c);
                isWhitespace = false;
                newlineCount = 0;
            }
        }
        if (isWhitespace)
        {
            sb.Length--;
        }
        return sb.ToString();
    }
}
