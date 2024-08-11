using RadialMenu.Config;
using RadialMenu.Graphics;

namespace RadialMenu.Menus;

/// <summary>
/// Radial menu displaying the shortcuts set up in the <see cref="Config.Configuration.CustomMenuItems"/>, as well as
/// any mod-added pages.
/// </summary>
internal class CustomMenu : IRadialMenu
{
    public IReadOnlyList<IRadialMenuPage> Pages => pages;

    public int SelectedPageIndex { get; set; }

    private readonly Func<IReadOnlyList<CustomMenuItemConfiguration>> getShortcuts;
    private readonly List<IRadialMenuPage> pages = [];
    private readonly Action<CustomMenuItemConfiguration> shortcutActivator;
    private readonly TextureHelper textureHelper;

    public CustomMenu(
        Func<IReadOnlyList<CustomMenuItemConfiguration>> getShortcuts,
        Action<CustomMenuItemConfiguration> shortcutActivator,
        TextureHelper textureHelper)
    {
        this.getShortcuts = getShortcuts;
        this.shortcutActivator = shortcutActivator;
        this.textureHelper = textureHelper;
        pages.Add(CreateShortcutPage());
    }

    /// <summary>
    /// Recreates the items on the shortcut page (first page of this menu).
    /// </summary>
    /// <remarks>
    /// Use when shortcuts have changed or may have changed, e.g. after the configuration was edited or upstream mod
    /// keybindings were changed.
    /// </remarks>
    public void RebuildShortcutPage()
    {
        pages[0] = CreateShortcutPage();
    }

    public void ResetSelectedPage()
    {
        SelectedPageIndex = 0;
    }

    private IRadialMenuPage CreateShortcutPage()
    {
        var shortcuts = getShortcuts();
        return MenuPage.FromCustomItemConfiguration(shortcuts, shortcutActivator, textureHelper);
    }
}
