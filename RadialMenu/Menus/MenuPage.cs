using RadialMenu.Config;
using StardewValley;

namespace RadialMenu.Menus;

/// <summary>
/// Helpers for creating standard menu pages.
/// </summary>
internal static class MenuPage
{
    /// <summary>
    /// Creates an <see cref="IRadialMenuPage"/> from the configured list of custom (keybind) shortcuts.
    /// </summary>
    /// <param name="itemConfigs">List of custom item configurations specifying names, keybinds, etc.</param>
    /// <param name="activator">Callback for when a custom item is activated.</param>
    /// <param name="textures">Utility for parsing configured asset fields as texture settings.</param>
    public static IRadialMenuPage FromCustomItemConfiguration(
        IEnumerable<CustomMenuItemConfiguration> itemConfigs,
        Action<CustomMenuItemConfiguration> activator,
        TextureHelper textures)
    {
        var items = itemConfigs.Select(config => CreateCustomMenuItem(config, activator, textures)).ToList();
        return new MenuPage<CustomMenuItem>(items, _ => false);
    }

    /// <summary>
    /// Creates an <see cref="IRadialMenuPage"/> based off a player's inventory and paging parameters.
    /// </summary>
    /// <param name="who">The player whose inventory should be displayed.</param>
    /// <param name="startIndex">First index of the player's <see cref="Farmer.Items"/> to display.</param>
    /// <param name="count">Number of items to include on this page.</param>
    public static IRadialMenuPage FromFarmerInventory(Farmer who, int startIndex, int count)
    {
        var items = Enumerable.Range(startIndex, count)
            .Select(i => who.Items[i])
            .Where(item => item is not null)
            .Select(item => new InventoryMenuItem(item))
            .ToList();
        bool isSelected(InventoryMenuItem menuItem) => menuItem.Item == who.Items[who.CurrentToolIndex];
        return new MenuPage<InventoryMenuItem>(items, isSelected);
    }

    private static CustomMenuItem CreateCustomMenuItem(
        CustomMenuItemConfiguration config,
        Action<CustomMenuItemConfiguration> activator,
        TextureHelper textures)
    {
        var sprite = textures.GetSprite(config.SpriteSourceFormat, config.SpriteSourcePath);
        return new(
            title: config.Name,
            description: config.Description,
            texture: sprite?.Texture,
            sourceRectangle: sprite?.SourceRect,
            activate: (who, delayedActions, _) =>
            {
                if (delayedActions == DelayedActions.All
                    || (delayedActions != DelayedActions.None && config.EnableActivationDelay))
                {
                    return MenuItemActivationResult.Delayed;
                }
                activator.Invoke(config);
                return MenuItemActivationResult.Custom;
            });
    }
}

/// <summary>
/// Generic implementation of an <see cref="IRadialMenuPage"/>.
/// </summary>
/// <param name="items">The items on this page.</param>
/// <param name="isSelected">Predicate function to check whether a given item is selected.</param>
internal class MenuPage<T>(IReadOnlyList<T> items, Predicate<T> isSelected) : IRadialMenuPage
    where T : class, IRadialMenuItem
{
    public IReadOnlyList<IRadialMenuItem> Items => items;

    public int SelectedItemIndex => GetSelectedIndex();

    private int GetSelectedIndex()
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (isSelected(items[i]))
            {
                return i;
            }
        }
        return -1;
    }
}
