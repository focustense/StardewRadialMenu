using StardewValley;

namespace RadialMenu.Menus;

/// <summary>
/// An inventory radial menu for a single player.
/// </summary>
/// <param name="who">The player whose inventory will be displayed.</param>
/// <param name="getPageSize">Function to get the desired page size, i.e. pointing to current mod config.</param>
internal class InventoryMenu(Farmer who, Func<int> getPageSize) : IRadialMenu
{
    public IReadOnlyList<IRadialMenuPage> Pages
    {
        get
        {
            RefreshIfDirty();
            return pages;
        }
    }

    public int SelectedPageIndex { get; set; }

    private readonly List<IRadialMenuPage> pages = [];

    private bool isDirty = true;

    /// <summary>
    /// Marks the menu invalid, so that its pages get refreshed the next time it is about to be displayed.
    /// </summary>
    public void Invalidate()
    {
        isDirty = true;
    }

    public void ResetSelectedPage()
    {
        var currentItem = who.CurrentItem;
        for (int i = 0; i < pages.Count; i++)
        {
            if (pages[i].Items.Any(i => i is InventoryMenuItem menuItem && menuItem.Item == currentItem))
            {
                SelectedPageIndex = i;
                return;
            }
        }
    }

    private void RefreshIfDirty()
    {
        if (!isDirty)
        {
            return;
        }
        pages.Clear();
        var pageSize = getPageSize();
        for (int i = 0; i < who.Items.Count; i += pageSize)
        {
            var actualCount = Math.Min(pageSize, who.Items.Count - i);
            pages.Add(MenuPage.FromFarmerInventory(who, i, actualCount));
        }
        isDirty = false;
    }
}
