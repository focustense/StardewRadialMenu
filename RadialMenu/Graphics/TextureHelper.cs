using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using RadialMenu.Config;
using StardewModdingAPI;
using StardewValley;

namespace RadialMenu.Graphics;

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
            case SpriteSourceFormat.TextureSegment:
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
                            $"{ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}",
                            LogLevel.Error);
                    }
                }
                break;
        }
        return null;
    }
}
