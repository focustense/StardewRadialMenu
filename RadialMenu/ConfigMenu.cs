using RadialMenu.Config;
using StardewModdingAPI;

namespace RadialMenu;

internal class ConfigMenu(
    IGenericModMenuConfigApi gmcm,
    IManifest mod,
    ITranslationHelper translations,
    Func<Configuration> getConfig)
{
    protected Configuration Config => getConfig();

    public void Setup()
    {
        gmcm.AddSectionTitle(
            mod,
            text: () => translations.Get("gmcm.controls"));
        gmcm.AddNumberOption(
            mod,
            name: () => translations.Get("gmcm.controls.trigger.deadzone"),
            tooltip: () => translations.Get("gmcm.controls.trigger.deadzone.tooltip"),
            getValue: () => Config.TriggerDeadZone,
            setValue: value => Config.TriggerDeadZone = value,
            min: 0.0f,
            max: 1.0f);
        AddEnumOption(
            "gmcm.controls.thumbstick.preference",
            getValue: () => Config.ThumbStickPreference,
            setValue: value => Config.ThumbStickPreference = value);
        gmcm.AddNumberOption(
            mod,
            name: () => translations.Get("gmcm.controls.thumbstick.deadzone"),
            tooltip: () => translations.Get("gmcm.controls.thumbstick.deadzone.tooltip"),
            getValue: () => Config.ThumbStickDeadZone,
            setValue: value => Config.ThumbStickDeadZone = value,
            min: 0.0f,
            max: 1.0f);
        AddEnumOption(
            "gmcm.controls.activation",
            getValue: () => Config.Activation,
            setValue: value => Config.Activation = value);

        gmcm.AddSectionTitle(
            mod,
            text: () => translations.Get("gmcm.inventory"));
        gmcm.AddNumberOption(
            mod,
            name: () => translations.Get("gmcm.inventory.max"),
            tooltip: () => translations.Get("gmcm.inventory.max.tooltip"),
            getValue: () => Config.MaxInventoryItems,
            setValue: value => Config.MaxInventoryItems = value,
            // Any less than the size of a single backpack row (12) and some items become
            // effectively inaccessible without rearranging the inventory. We don't want that.
            min: 12,
            // Limiting this is less about balance and more about preventing overlap or crashes due
            // to the math not working out. If players really want to exceed the limit, they can
            // edit the config.json, but we won't encourage that in the CM.
            max: 24);
    }

    private void AddEnumOption<T>(
        string messageId,
        Func<T> getValue,
        Action<T> setValue)
    where T : struct, Enum
    {
        gmcm.AddTextOption(
            mod,
            name: () => translations.Get(messageId),
            tooltip: () => translations.Get($"{messageId}.tooltip"),
            getValue: () => getValue().ToString().ToLowerInvariant(),
            setValue: value => setValue(Enum.Parse<T>(value, true)),
            allowedValues: Enum.GetValues<T>()
                .Select(e => e.ToString().ToLowerInvariant())
                .ToArray(),
            formatAllowedValue: value => translations.Get($"{messageId}.{value}"));
    }
}
