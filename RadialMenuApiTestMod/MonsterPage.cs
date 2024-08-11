using Microsoft.Xna.Framework.Graphics;
using RadialMenu;

namespace RadialMenuApiTestMod;

internal class MonsterPage : IRadialMenuPage
{
    public IReadOnlyList<IRadialMenuItem> Items { get; }

    public int SelectedItemIndex => -1;

    private readonly Texture2D atlasTexture;
    private readonly IMonitor monitor;

    public MonsterPage(Texture2D atlasTexture, IMonitor monitor)
    {
        this.atlasTexture = atlasTexture;
        this.monitor = monitor;

        Items = [
            CreateItem(3, I18n.Monster_Blob_Title, I18n.Monster_Blob_Description),
            CreateItem(6, I18n.Monster_WhizKid_Title, I18n.Monster_WhizKid_Description),
            CreateItem(7, I18n.Monster_Basilisk_Title, I18n.Monster_Basilisk_Description),
            CreateItem(1, I18n.Monster_Ghost_Title, I18n.Monster_Ghost_Description),
            CreateItem(0, I18n.Monster_Yeti_Title, I18n.Monster_Yeti_Description),
            CreateItem(5, I18n.Monster_Manticore_Title, I18n.Monster_Manticore_Description),
            CreateItem(4, I18n.Monster_Gargoyle_Title, I18n.Monster_Gargoyle_Description),
            CreateItem(8, I18n.Monster_Vortex_Title, I18n.Monster_Vortex_Description),
            CreateItem(2, I18n.Monster_Dragon_Title, I18n.Monster_Dragon_Description),
        ];
    }

    private MonsterMenuItem CreateItem(int spriteIndex, Func<string> name, Func<string> description)
    {
        var x = (spriteIndex % 3) * 32;
        var y = spriteIndex / 3 * 32;
        var sourceRect = new Rectangle(x, y, 32, 32);
        return new(name, description, atlasTexture, sourceRect, monitor);
    }
}

internal class MonsterMenuItem(
    Func<string> title,
    Func<string> description,
    Texture2D texture,
    Rectangle sourceRect,
    IMonitor monitor)
    : IRadialMenuItem
{
    public string Title => title();

    public string Description => description();

    public Texture2D Texture => texture;

    public Rectangle? SourceRectangle => sourceRect;

    public MenuItemActivationResult Activate(Farmer who, DelayedActions delayedActions, MenuItemAction requestedAction)
    {
        if (delayedActions == DelayedActions.All)
        {
            return MenuItemActivationResult.Delayed;
        }
        monitor.Log($"Monster activated [{requestedAction}]: {Title}", LogLevel.Info);
        return requestedAction == MenuItemAction.Use ? MenuItemActivationResult.Used : MenuItemActivationResult.Custom;
    }
}