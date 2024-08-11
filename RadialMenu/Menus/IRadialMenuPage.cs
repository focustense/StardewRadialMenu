namespace RadialMenu.Menus;

/// <summary>
/// A single page in one of the radial menus.
/// </summary>
/// <remarks>
/// Pages can be navigated using left/right shoulder buttons while a menu is open. Only the items on the
/// currently-active page are visible at any given time.
/// </remarks>
public interface IRadialMenuPage
{
    /// <summary>
    /// The items on this page.
    /// </summary>
    IReadOnlyList<IRadialMenuItem> Items { get; }

    /// <summary>
    /// Index of the selected <see cref="IRadialMenuItem"/> in the <see cref="Items"/> list, or <c>-1</c> if no selection.
    /// </summary>
    int SelectedItemIndex { get; }
}
