namespace RadialMenu;

/// <summary>
/// Extends a read-only list with the ability to invalidate the contents.
/// </summary>
/// <typeparam name="T"></typeparam>
internal interface IInvalidatableList<out T> : IReadOnlyList<T>
{
    /// <summary>
    /// Marks the list as invalid, causing all items to be recreated/re-fetched on next access.
    /// </summary>
    void Invalidate();
}
