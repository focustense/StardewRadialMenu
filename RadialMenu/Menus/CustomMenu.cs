using RadialMenu.Config;
using RadialMenu.Graphics;
using System.Collections;

namespace RadialMenu.Menus;

/// <summary>
/// Radial menu displaying the shortcuts set up in the <see cref="Config.Configuration.CustomMenuItems"/>, as well as
/// any mod-added pages.
/// </summary>
internal class CustomMenu : IRadialMenu
{
    public IReadOnlyList<IRadialMenuPage> Pages => combinedPages;

    public int SelectedPageIndex { get; set; }

    private readonly CombinedPageList combinedPages;
    private readonly Func<IReadOnlyList<CustomMenuItemConfiguration>> getShortcuts;
    private readonly Action<CustomMenuItemConfiguration> shortcutActivator;
    private readonly TextureHelper textureHelper;

    public CustomMenu(
        Func<IReadOnlyList<CustomMenuItemConfiguration>> getShortcuts,
        Action<CustomMenuItemConfiguration> shortcutActivator,
        TextureHelper textureHelper,
        IInvalidatableList<IRadialMenuPage> additionalPages)
    {
        this.getShortcuts = getShortcuts;
        this.shortcutActivator = shortcutActivator;
        this.textureHelper = textureHelper;
        combinedPages = new CombinedPageList(CreateShortcutPage, additionalPages);
        
    }

    /// <summary>
    /// Recreates the items on the shortcut page (first page of this menu) and marks all other (mod) pages invalid,
    /// causing them to be recreated when next accessed.
    /// </summary>
    /// <remarks>
    /// Use when shortcuts have changed or may have changed, e.g. after the configuration was edited or upstream mod
    /// keybindings were changed.
    /// </remarks>
    public void Invalidate()
    {
        combinedPages.Invalidate();
        combinedPages.ShortcutPage = CreateShortcutPage();
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

    class CombinedPageList(Func<IRadialMenuPage> getShortcutPage, IInvalidatableList<IRadialMenuPage> additionalPages)
        : IReadOnlyList<IRadialMenuPage>
    {
        public IRadialMenuPage this[int index] => index == 0 ? ShortcutPage : additionalPages[index - 1];

        public IRadialMenuPage ShortcutPage { get; set; } = getShortcutPage();

        public int Count => additionalPages.Count + 1;

        public IEnumerator<IRadialMenuPage> GetEnumerator()
        {
            return additionalPages.Prepend(ShortcutPage).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Invalidate()
        {
            ShortcutPage = getShortcutPage();
            additionalPages.Invalidate();
        }
    }
}
