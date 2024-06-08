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
        public static MenuItem FromGameItem(Item item, int toolIndex)
        {
            var data = ItemRegistry.GetData(item.QualifiedItemId);
            var texture = data.GetTexture();
            var sourceRect = data.GetSourceRect();
            return new(
                data.DisplayName,
                data.Description,
                texture,
                sourceRect,
                () => ActivateTool(toolIndex));
        }

        private static void ActivateTool(int toolIndex)
        {
            if (toolIndex >= 0 && toolIndex < Game1.player.Items.Count)
            {
                Game1.player.CurrentToolIndex = toolIndex;
                if (Game1.player.CurrentTool is not null)
                {
                    Game1.playSound("toolSwap");
                }
            }
        }
    }
}
