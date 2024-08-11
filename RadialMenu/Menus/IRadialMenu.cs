namespace RadialMenu.Menus;

/// <summary>
/// Definition of a menu that can be displayed using one of the trigger buttons.
/// </summary>
public interface IRadialMenu
{
    /// <summary>
    /// The pages in this menu, which can be navigated using shoulder buttons.
    /// </summary>
    IReadOnlyList<IRadialMenuPage> Pages { get; }

    /// <summary>
    /// Index of the currently-selected page in <see cref="Pages"/>.
    /// </summary>
    int SelectedPageIndex { get; set; }

    /// <summary>
    /// Gets the page at the <see cref="SelectedPageIndex"/>.
    /// </summary>
    /// <returns>The selected page, or <c>null</c> if there are no pages or if the <see cref="SelectedPageIndex"/> is
    /// invalid.</returns>
    IRadialMenuPage? GetSelectedPage()
    {
        return (SelectedPageIndex >= 0) && (SelectedPageIndex < Pages.Count) ? Pages[SelectedPageIndex] : null;
    }

    /// <summary>
    /// Moves the <see cref="SelectedPageIndex"/> to the next page, wrapping back to <c>0</c> if the end is reached.
    /// </summary>
    /// <returns><c>true</c> if the selection changed; otherwise <c>false</c>.</returns>
    bool NextPage()
    {
        var previousIndex = SelectedPageIndex;
        do
        {
            SelectedPageIndex++;
            if (SelectedPageIndex >= Pages.Count)
            {
                SelectedPageIndex = 0;
            }
        } while (SelectedPageIndex != previousIndex && GetSelectedPage()?.Items.Count == 0);
        return SelectedPageIndex != previousIndex;
    }

    /// <summary>
    /// Moves the <see cref="SelectedPageIndex"/> to the previous page, wrapping to the last page if the beginning is
    /// reached.
    /// </summary>
    /// <returns><c>true</c> if the selection changed; otherwise <c>false</c>.</returns>
    bool PreviousPage()
    {
        var previousIndex = SelectedPageIndex;
        do
        {
            SelectedPageIndex--;
            if (SelectedPageIndex < 0)
            {
                SelectedPageIndex = Pages.Count - 1;
            }
        } while (SelectedPageIndex != previousIndex && GetSelectedPage()?.Items.Count == 0);
        return SelectedPageIndex != previousIndex;
    }

    /// <summary>
    /// Resets the <see cref="SelectedPageIndex"/> to the "default" for that menu. The precise meaning depends on the
    /// specific implementation.
    /// </summary>
    void ResetSelectedPage();
}
