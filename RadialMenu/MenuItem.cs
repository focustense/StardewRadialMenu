using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace RadialMenu
{
    internal record MenuItem(
        string Title,
        string Description,
        Texture2D Texture,
        Rectangle? SourceRectangle,
        Action Activate)
    {
        public static MenuItem FromGameItem(Item item, int itemIndex)
        {
            var data = ItemRegistry.GetData(item.QualifiedItemId);
            var texture = data.GetTexture();
            var sourceRect = data.GetSourceRect();
            return new(
                data.DisplayName,
                data.Description,
                texture,
                sourceRect,
                () => FuzzyActivation.ConsumeOrSelect(itemIndex));
        }

        
    }
}
