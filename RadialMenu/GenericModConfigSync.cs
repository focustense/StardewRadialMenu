using RadialMenu.Config;
using StardewModdingAPI;

namespace RadialMenu
{
    internal class GenericModConfigSync(
        Func<Configuration> getConfig,
        GenericModConfigKeyBindings bindings,
        IMonitor monitor)
    {
        public void Sync(CustomMenuItemConfiguration item)
        {
            if (item.Gmcm is not GmcmAssociation gmcm)
            {
                return;
            }
            var keybindOption =
                bindings.Find(gmcm.ModId, gmcm.FieldId, gmcm.FieldName, item.Keybind);
            if (keybindOption is null)
            {
                monitor.Log(
                    $"Couldn't sync key binding information for item named '{item.Name}'. " +
                    $"No keybinding field in {gmcm.ModId} for field name '{gmcm.FieldName}' or " +
                    $"field ID {gmcm.FieldId}.",
                    LogLevel.Warn);
                return;
            }
            if (!gmcm.UseCustomName)
            {
                item.Name = keybindOption.ModManifest.Name;
                // Some mod names can be quite long, the most obvious being "Generic Mod Config
                // Menu" itself. Since the title uses large font and there is limited space, it's
                // usually a better idea to combine both the field name and tooltip into the
                // description, instead of making the field name part of the title as it might be
                // shown in the GMCM select box.
                var fieldName = keybindOption.GetFieldName();
                var tooltip = keybindOption.GetTooltip();
                item.Description = !string.IsNullOrWhiteSpace(tooltip)
                    ? $"{fieldName} - {tooltip}"
                    : fieldName;
            }
            gmcm.FieldId = keybindOption.FieldId;
            gmcm.FieldName = keybindOption.UniqueFieldName;
            item.Keybind = new(keybindOption.GetCurrentBinding().ToArray());
            monitor.Log($"Synced GMCM keybinding for item '{item.Name}'.", LogLevel.Info);
        }

        public void SyncAll()
        {
            var config = getConfig();
            foreach (var item in config.CustomMenuItems)
            {
                Sync(item);
            }
        }
    }
}
