using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System.Diagnostics.CodeAnalysis;

namespace RadialMenu
{
    internal record MenuItem(
        string Title,
        string Description,
        int? StackSize,
        int? Quality,
        Texture2D Texture,
        Rectangle? SourceRectangle,
        Action Activate);

    internal class MenuItemBuilder(
        IGameContentHelper contentHelper,
        Action<CustomMenuItem> customItemActivator,
        IMonitor monitor)
    {
        private readonly Dictionary<(SpriteSourceFormat, string), CachedSprite> cachedSprites = [];

        public MenuItem CustomItem(CustomMenuItem item)
        {
            var cacheKey = (item.SpriteSourceFormat, item.SpriteSourcePath);
            if (!cachedSprites.TryGetValue(cacheKey, out var sprite))
            {
                sprite = GetSprite(item.SpriteSourceFormat, item.SpriteSourcePath);
                cachedSprites.TryAdd(cacheKey, sprite);
            }
            return new(
                item.Name,
                item.Description,
                /* StackSize= */ null,
                /* Quality= */ null,
                sprite.Texture,
                sprite.SourceRect,
                () => customItemActivator.Invoke(item));
        }

        public MenuItem GameItem(Item item, int itemIndex)
        {
            var data = ItemRegistry.GetData(item.QualifiedItemId);
            var texture = data.GetTexture();
            var sourceRect = data.GetSourceRect();
            return new(
                data.DisplayName,
                data.Description,
                item.maximumStackSize() > 1 ? item.Stack : null,
                item.Quality,
                texture,
                sourceRect,
                () => FuzzyActivation.ConsumeOrSelect(itemIndex));
        }

        private CachedSprite GetSprite(SpriteSourceFormat format, string path)
        {
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
                    if (TextureRect.TryParse(path, out var textureRect))
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
            // Didn't find the expected sprite. Use a monogram instead.
            // TODO: Implement the monograms
            throw new NotImplementedException();
        }

        private record CachedSprite(Texture2D Texture, Rectangle? SourceRect);
    }

    record TextureRect(string AssetPath, Rectangle SourceRect)
    {
        public static bool TryParse(
            string formattedPath,
            [MaybeNullWhen(false)] out TextureRect parsed)
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
}
