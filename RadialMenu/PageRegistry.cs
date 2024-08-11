using RadialMenu.Menus;
using StardewValley;
using System.Collections;

namespace RadialMenu;

/// <summary>
/// Helper for mod-registered pages; manages per-player page lists and handles invalidation.
/// </summary>
internal class PageRegistry
{
    record PageRegistration(string Key, IRadialMenuPageFactory Factory);

    public int Count => registrations.Count;

    // The primary data structure should be an indexed collection so that it can be composed into other lists.
    private readonly List<PageRegistration> registrations = [];
    // Re-registrations will happen by key, so we need a way to refer back to the primary data.
    private readonly Dictionary<string, int> registrationIndices = [];
    // Pages have to be tracked in order to be able to invalidate them. We can't do both in the same place because
    // registrations are global and page lists are per-player. Use weak references to prevent memory leaks.
    private readonly List<WeakReference<PageList>> trackedPageLists = [];

    public IRadialMenuPage CreatePage(int index, Farmer who)
    {
        return registrations[index].Factory.CreatePage(who);
    }

    public IInvalidatableList<IRadialMenuPage> CreatePageList(Farmer who)
    {
        var pageList = new PageList(this, who);
        trackedPageLists.Add(new(pageList));
        return pageList;
    }

    public bool InvalidatePage(string key)
    {
        if (!registrationIndices.TryGetValue(key, out var index))
        {
            return false;
        }
        InvalidateIndex(index);
        return true;
    }

    public void RegisterPage(string key, IRadialMenuPageFactory factory)
    {
        if (registrationIndices.TryGetValue(key, out var index))
        {
            InvalidateIndex(index);
            registrations[index] = new(key, factory);
        }
        else
        {
            registrations.Add(new(key, factory));
            registrationIndices.Add(key, registrations.Count - 1);
        }
    }

    private void InvalidateIndex(int index)
    {
        for (int i = trackedPageLists.Count - 1; i > 0; i--)
        {
            if (trackedPageLists[i].TryGetTarget(out var pageList))
            {
                pageList.InvalidateAt(index);
            }
            else
            {
                trackedPageLists.RemoveAt(i);
            }
        }
    }
}

internal class PageList(PageRegistry registry, Farmer who) : IInvalidatableList<IRadialMenuPage>
{
    public IRadialMenuPage this[int index] => GetPageAt(index);

    public int Count => registry.Count;

    private readonly List<IRadialMenuPage?> pages = [];

    public IEnumerator<IRadialMenuPage> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
        {
            yield return GetPageAt(i);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Invalidate()
    {
        for (int i = 0; i < pages.Count; i++)
        {
            pages[i] = null;
        }
    }

    public void InvalidateAt(int index)
    {
        pages[index] = null;
    }

    private IRadialMenuPage GetPageAt(int index)
    {
        while (pages.Count <= index)
        {
            pages.Add(null);
        }
        return pages[index] ??= registry.CreatePage(index, who);
    }
}