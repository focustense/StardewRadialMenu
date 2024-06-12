using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using RadialMenu.Config;
using StardewModdingAPI;
using StardewValley;
using System.Diagnostics.CodeAnalysis;

namespace RadialMenu;

internal record TextureSegment(Texture2D Texture, Rectangle? SourceRect);

internal class TextureHelper(IGameContentHelper contentHelper, IMonitor monitor)
{
    public TextureSegment? GetSprite(SpriteSourceFormat format, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }
        switch (format)
        {
            case SpriteSourceFormat.ItemIcon:
                var itemData = ItemRegistry.GetData(path);
                if (itemData is not null)
                {
                    return new(itemData.GetTexture(), itemData.GetSourceRect());
                }
                else
                {
                    monitor.Log($"Couldn't get item data for item {path}", LogLevel.Warn);
                }
                break;
            case SpriteSourceFormat.TextureRect:
                if (TextureSegmentPath.TryParse(path, out var textureRect))
                {
                    try
                    {
                        var texture = contentHelper.Load<Texture2D>(textureRect.AssetPath);
                        return new(texture, textureRect.SourceRect);
                    }
                    catch (Exception ex)
                    when (ex is ArgumentException || ex is ContentLoadException)
                    {
                        monitor.Log(
                            $"Failed to load texture asset: {textureRect.AssetPath}\n" +
                            $"{ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}");
                    }
                }
                break;
        }
        return null;
    }
}

internal record TextureSegmentPath(string AssetPath, Rectangle SourceRect)
{
    public static bool TryParse(
        string formattedPath,
        [MaybeNullWhen(false)] out TextureSegmentPath parsed)
    {
        parsed = null;
        var parts = formattedPath.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }
        var assetPath = parts[0].Trim();
        var formattedRect = parts[1].Trim();
        if (!formattedRect.StartsWith("(") || !formattedRect.EndsWith(")"))
        {
            return false;
        }
        var coords = formattedRect[1..^1].Split(',');
        if (coords.Length != 4)
        {
            return false;
        }
        if (int.TryParse(coords[0], out int x)
            && int.TryParse(coords[1], out int y)
            && int.TryParse(coords[2], out int width)
            && int.TryParse(coords[3], out int height))
        {
            parsed = new(assetPath, new Rectangle(x, y, width, height));
            return true;
        }
        return false;
    }
}
