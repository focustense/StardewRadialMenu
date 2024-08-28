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
        RefreshIfDirty();
        // In case there's any inconsistency between the menu and the player's inventory, we'll first try to match on
        // the item itself. However, there might be no current item if the player just consumed the last of a stack, or
        // put it into a chest, etc., so if this happens, default to the first item on the current "page", which is
        // better than doing nothing.
        var currentItem = who.CurrentItem ?? who.Items.FirstOrDefault(i => i is not null);
        if (currentItem is not null)
        {
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i].Items.Any(i => i is InventoryMenuItem menuItem && menuItem.Item == currentItem))
                {
                    SelectedPageIndex = i;
                    return;
                }
            }
        }
        // We shouldn't normally reach this, but if we did, then it means we can't make a useful decision. We'd like to
        // display something other than an empty page, so the first choice is to just stay on the current page (if it's
        // non-empty) or, failing that, go to the first non-empty page (if we can).
        if ((this as IRadialMenu).GetSelectedPage()?.Items.Count == 0)
        {
            SelectedPageIndex = Math.Max(pages.FindIndex(page => page.Items.Count > 0), 0);
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
