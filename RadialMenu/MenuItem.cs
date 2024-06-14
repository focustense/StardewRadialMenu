using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RadialMenu.Config;
using StardewValley;

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
        TextureHelper textureHelper,
        Action<CustomMenuItemConfiguration> customItemActivator)
    {
        private readonly Dictionary<(SpriteSourceFormat, string), TextureSegment> spriteCache = [];

        public MenuItem CustomItem(CustomMenuItemConfiguration item)
        {
            var cacheKey = (item.SpriteSourceFormat, item.SpriteSourcePath);
            if (!spriteCache.TryGetValue(cacheKey, out var sprite))
            {
                sprite = textureHelper.GetSprite(item.SpriteSourceFormat, item.SpriteSourcePath);
                // TODO: Sprite can actually be null here and it will cause an exception if it is.
                // Actual spec should be that TextureHelper never returns null - it should fall back
                // to monogram and then maybe to some empty 1x1 texture or blank spot on one of the
                // built-in sprite sheets.
                spriteCache.TryAdd(cacheKey, sprite);
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
    }
}
