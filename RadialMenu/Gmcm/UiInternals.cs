using GenericModConfigMenu.Framework;
using StardewValley.Menus;
using StardewValley;
using System.Diagnostics.CodeAnalysis;
using GenericModConfigMenu.Framework.ModOption;
using SpaceShared.UI;

namespace RadialMenu.Gmcm;

// Collection of scary, fragile methods that depend on GMCM/SpaceCore internals and are at severe
// risk of breakage due to mod updates, but that we nevertheless can't live without.
internal static class UiInternals
{
    public static BaseModOption? ForceResetOption(this ModConfigPage page, string fieldId)
    {
        var option = page.Options.FirstOrDefault(opt => opt.FieldId == fieldId);
        option?.AfterReset();
        return option;
    }

    public static void ForceSaveOption(this ModConfigPage page, string fieldId)
    {
        page.Options
            .FirstOrDefault(opt => opt.FieldId == fieldId)?
            .BeforeSave();
    }
    public static void ForceUpdateElement<T>(
        this SpecificModConfigMenu menu, int childIndex, Action<T> update)
        where T : Element
    {
        ForceUpdateElement(menu.Table, childIndex, update);
    }

    public static void ForceUpdateElement<T>(this Table table, int childIndex, Action<T> update)
        where T : Element
    {
        var element = table.Children.Length > childIndex ? table.Children[childIndex] : null;
        if (element is T typedElement)
        {
            update(typedElement);
        }
    }

    public static bool TryGetModConfigMenu([MaybeNullWhen(false)] out SpecificModConfigMenu menu)
    {
        // GMCM uses this to get the SpecificModConfigMenu, which is where the options are stored.
        var activeMenu = Game1.activeClickableMenu is TitleMenu
            ? TitleMenu.subMenu
            : Game1.activeClickableMenu;
        if (activeMenu is SpecificModConfigMenu configMenu)
        {
            menu = configMenu;
            return true;
        }
        menu = null;
        return false;
    }

    public static bool TryGetModConfigPage(
        [MaybeNullWhen(false)] out ModConfigPage page,
        string? expectedPageId = null)
    {
        if (TryGetModConfigMenu(out var menu))
        {
            page = menu.ModConfig.ActiveDisplayPage;
            return expectedPageId is null || page.PageId == expectedPageId;
        }
        page = null;
        return false;
    }

    public static bool TryGetModConfigMenuAndPage(
        [MaybeNullWhen(false)] out SpecificModConfigMenu menu,
        [MaybeNullWhen(false)] out ModConfigPage page,
        string? expectedPageId = null)
    {
        menu = null;
        page = null;
        
        var activeMenu = Game1.activeClickableMenu is TitleMenu
            ? TitleMenu.subMenu
            : Game1.activeClickableMenu;
        if (activeMenu is not SpecificModConfigMenu configMenu)
        {
            return false;
        }
        menu = configMenu;
        page = menu.ModConfig.ActiveDisplayPage;
        return expectedPageId is null || page.PageId == expectedPageId;
    }
}
