using Microsoft.Xna.Framework.Graphics;
using RadialMenu;

namespace RadialMenuApiTestMod;

// Example of a page with enhanced features: item selection and animated sprites.
internal class CharacterPage : IRadialMenuPage
{
    // Does not have to be immutable; the items in the list can be changed at any time. If modifying the list, be
    // careful to use a SelectedItemIndex implementation that maintains consistency.
    public IReadOnlyList<IRadialMenuItem> Items { get; }

    // In a real-world implementation, the selection action would probably manipulate some actual mod data and this
    // property would be a reference to (or indexOf) the actual selection.
    public int SelectedItemIndex { get; private set; }

    private readonly Texture2D atlasTexture;

    public CharacterPage(Texture2D atlasTexture)
    {
        this.atlasTexture = atlasTexture;
        Items = [
            CreateItem(0, 2, I18n.Character_Soldier_Title, I18n.Character_Soldier_Description),
            CreateItem(1, 0, I18n.Character_BlackMage_Title, I18n.Character_BlackMage_Description),
            CreateItem(2, 7, I18n.Character_WhiteMage_Title, I18n.Character_WhiteMage_Description),
            CreateItem(3, 1, I18n.Character_Fighter_Title, I18n.Character_Fighter_Description),
            CreateItem(4, 5, I18n.Character_Thief_Title, I18n.Character_Thief_Description),
            CreateItem(5, 4, I18n.Character_Spellsword_Title, I18n.Character_Spellsword_Description),
            CreateItem(6, 3, I18n.Character_Royal_Title, I18n.Character_Royal_Description),
            CreateItem(7, 6, I18n.Character_Priest_Title, I18n.Character_Priest_Description),
        ];
    }

    private CharacterMenuItem CreateItem(int menuIndex, int spriteIndex, Func<string> name, Func<string> description)
    {
        var x = (spriteIndex % 2) * 64;
        var y = spriteIndex / 2 * 32;
        var sourceRect = new Rectangle(x, y, 32, 32);
        return new(name, description, atlasTexture, sourceRect, () => SelectedItemIndex = menuIndex);
    }
}

internal class CharacterMenuItem(
    Func<string> name,
    Func<string> description,
    Texture2D texture,
    Rectangle baseSourceRect,
    Action onSelect)
    : IRadialMenuItem
{
    private static readonly TimeSpan AnimationInterval = TimeSpan.FromMilliseconds(250);

    public string Title => name();

    public string Description => description();

    public Texture2D Texture => texture;

    public Rectangle? SourceRectangle => GetAnimatedSourceRect();

    public MenuItemActivationResult Activate(Farmer who, DelayedActions delayedActions, MenuItemAction requestedAction)
    {
        if (delayedActions != DelayedActions.None)
        {
            return MenuItemActivationResult.Delayed;
        }
        onSelect();
        return MenuItemActivationResult.Selected;
    }

    private Rectangle GetAnimatedSourceRect()
    {
        var frameIndex = (int)(Game1.currentGameTime.TotalGameTime / AnimationInterval) % 2;
        var rect = baseSourceRect;
        if (frameIndex > 0)
        {
            rect.Offset(frameIndex * baseSourceRect.Width, 0);
        }
        return rect;
    }
}
