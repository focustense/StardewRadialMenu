using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RadialMenu.Config;
using StardewValley;
using StardewValley.Menus;

namespace RadialMenu;

internal class CustomItemListWidget(TextureHelper textureHelper)
{
    private record ItemLayout(Texture2D Texture, Rectangle? SourceRect, Rectangle DestinationRect);

    private const int ITEM_HEIGHT = 64;
    private const int ITEM_VERTICAL_SPACING = 32;
    private const int MAX_COLUMNS = 6;
    private const int VERTICAL_OFFSET = 16;

    private readonly List<CustomMenuItemConfiguration> items = [];
    // Item count is tracked separately from customItems.Count, so we can "remove" items without
    // losing their data. This way, if the player lowers the count and raises it again, the old
    // items are still there and don't have to be tediously set up all over again.
    private int itemCount;

    private (int, double) animatedItemIndexAndStartTime = (-1, 0);

    public void Draw(SpriteBatch spriteBatch, Vector2 startPosition)
    {
        var mousePos = Game1.getMousePosition();
        var labelHeight = (int)Game1.dialogueFont.MeasureString("A").Y;
        startPosition.Y += labelHeight + VERTICAL_OFFSET;
        // From SpecificModConfigMenu.cs
        var tableWidth = Math.Min(1200, Game1.uiViewport.Width - 200);
        startPosition.X = (Game1.uiViewport.Width - tableWidth) / 2;
        var maxItemWidth = tableWidth / MAX_COLUMNS;
        var position = startPosition;
        int col = 0;
        bool hadMouseOver = false;
        for (int i = 0; i < itemCount; i++)
        {
            if (col == MAX_COLUMNS)
            {
                col = 0;
                position.X = startPosition.X;
                position.Y += ITEM_HEIGHT + ITEM_VERTICAL_SPACING;
            }
            var item = items[i];
            var centerX = (int)position.X + maxItemWidth / 2;
            var (texture, sourceRect, destinationRect) = LayoutItem(item, centerX, position.Y);
            if (destinationRect.Contains(mousePos))
            {
                var animationScale = GetAnimationScale(i);
                destinationRect.Inflate(
                    destinationRect.Width * animationScale * 0.1f,
                    destinationRect.Height * animationScale * 0.1f);
                hadMouseOver = true;
            }
            spriteBatch.Draw(texture, destinationRect, sourceRect, Color.White);
            col++;
            position.X += maxItemWidth;
        }
        // If the mouse wasn't over any item in this frame, then it means whatever was
        // previously being animated should be reset, otherwise it may "pop" to a random scale
        // if the player returns to that previous item.
        if (!hadMouseOver)
        {
            animatedItemIndexAndStartTime = (-1, 0);
        }
    }

    private float GetAnimationScale(int index)
    {
        var gameTime = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
        var (previousIndex, startTime) = animatedItemIndexAndStartTime;
        if (index != previousIndex)
        {
            startTime = gameTime;
            animatedItemIndexAndStartTime = (index, startTime);
        }
        return (float)(Math.Sin((gameTime - startTime) * Math.PI / 512) + 1.0f) / 2.0f;
    }

    private ItemLayout LayoutItem(CustomMenuItemConfiguration item, float centerX, float topY)
    {
        var sprite = textureHelper.GetSprite(item.SpriteSourceFormat, item.SpriteSourcePath)
            ?? new(Game1.mouseCursors, /* Question Mark */ new(176, 425, 9, 12));
        var sourceSize = sprite.SourceRect?.Size ?? sprite.Texture.Bounds.Size;
        var aspectRatio = sourceSize.X / (float)sourceSize.Y;
        var itemWidth = aspectRatio * ITEM_HEIGHT;
        var destinationRect = new Rectangle(
            (int)MathF.Round(centerX - itemWidth / 2), (int)topY, (int)itemWidth, ITEM_HEIGHT);
        return new(sprite.Texture, sprite.SourceRect, destinationRect);
    }

    public int GetHeight()
    {
        var labelHeight = (int)Game1.dialogueFont.MeasureString("A").Y;
        var rowCount = (int)MathF.Ceiling((float)itemCount / MAX_COLUMNS);
        return labelHeight
            + VERTICAL_OFFSET
            + ITEM_HEIGHT * rowCount + ITEM_VERTICAL_SPACING * (rowCount - 1);
    }

    public void Load(IReadOnlyList<CustomMenuItemConfiguration> items)
    {
        this.items.Clear();
        this.items.AddRange(items);
        itemCount = items.Count;
    }

    public void Save()
    {
    }

    public void SetCount(int count)
    {
        var menu = Game1.activeClickableMenu is TitleMenu ? TitleMenu.subMenu : Game1.activeClickableMenu;
        itemCount = count;
        while (items.Count < itemCount)
        {
            items.Add(new());
        }
    }
}
