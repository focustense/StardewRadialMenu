using RadialMenu.Config;
using RadialMenu.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace RadialMenu.Gmcm;

internal class ConfigMenu(
    IGenericModMenuConfigApi gmcm,
    IGMCMOptionsAPI? gmcmOptions,
    GenericModConfigKeybindings? gmcmBindings,
    GenericModConfigSync? gmcmSync,
    IManifest mod,
    ITranslationHelper translations,
    IModContentHelper modContent,
    TextureHelper textureHelper,
    IGameLoopEvents gameLoopEvents,
    Func<Configuration> getConfig)
{
    protected Configuration Config => getConfig();

    // Primary ctor properties can't be read-only and we're OCD.
    private readonly IGenericModMenuConfigApi gmcm = gmcm;
    private readonly IGMCMOptionsAPI? gmcmOptions = gmcmOptions;
    private readonly IManifest mod = mod;
    private readonly ITranslationHelper translations = translations;
    private readonly Func<Configuration> getConfig = getConfig;

    // Sub-pages
    private readonly CustomMenuPage customMenuPage = new(
        gmcm, gmcmBindings, gmcmSync, mod, translations, textureHelper, gameLoopEvents,
        getConfig);
    private readonly StylePage stylePage =
        new(gmcm, gmcmOptions, mod, modContent, translations, () => getConfig().Styles);

    public void Setup()
    {
        AddMainOptions();
        customMenuPage.Setup();
        stylePage.Setup();
    }

    private void AddMainOptions()
    {
        gmcm.AddSectionTitle(mod, I18n.Gmcm_Controls);
        gmcm.AddNumberOption(
            mod,
            name: I18n.Gmcm_Controls_Trigger_Deadzone,
            tooltip: I18n.Gmcm_Controls_Trigger_Deadzone_Tooltip,
            getValue: () => Config.TriggerDeadZone,
            setValue: value => Config.TriggerDeadZone = value,
            min: 0.0f,
            max: 1.0f);
        gmcm.AddBoolOption(
            mod,
            name: I18n.Gmcm_Controls_Trigger_Swap,
            tooltip: I18n.Gmcm_Controls_Trigger_Swap_Tooltip,
            getValue: () => Config.SwapTriggers,
            setValue: value => Config.SwapTriggers = value);
        AddEnumOption(
            "gmcm.controls.thumbstick.preference",
            getValue: () => Config.ThumbStickPreference,
            setValue: value => Config.ThumbStickPreference = value);
        gmcm.AddNumberOption(
            mod,
            name: I18n.Gmcm_Controls_Thumbstick_Deadzone,
            tooltip: I18n.Gmcm_Controls_Thumbstick_Deadzone_Tooltip,
            getValue: () => Config.ThumbStickDeadZone,
            setValue: value => Config.ThumbStickDeadZone = value,
            min: 0.0f,
            max: 1.0f);
        AddEnumOption(
            "gmcm.controls.activation",
            getValue: () => Config.PrimaryActivation,
            setValue: value => Config.PrimaryActivation = value);
        AddEnumOption(
            "gmcm.controls.action.primary",
            "gmcm.controls.action.type",
            () => Config.PrimaryAction,
            value => Config.PrimaryAction = value);
        gmcm.AddKeybind(
            mod,
            name: I18n.Gmcm_Controls_Action_Secondary_Button,
            tooltip: I18n.Gmcm_Controls_Action_Secondary_Button_Tooltip,
            getValue: () => Config.SecondaryActionButton,
            setValue: value => Config.SecondaryActionButton = value);
        AddEnumOption(
            "gmcm.controls.action.secondary",
            "gmcm.controls.action.type",
            () => Config.SecondaryAction,
            value => Config.SecondaryAction = value);
        gmcm.AddNumberOption(
            mod,
            name: I18n.Gmcm_Controls_Activation_Delay,
            tooltip: I18n.Gmcm_Controls_Activation_Delay_Tooltip,
            getValue: () => Config.ActivationDelayMs,
            setValue: value => Config.ActivationDelayMs = value,
            formatValue: value => I18n.Gmcm_Controls_Activation_Delay_Value(value),
            min: 0,
            max: 500);
        AddEnumOption(
            "gmcm.controls.activation.delay.actions",
            getValue: () => Config.DelayedActions,
            setValue: value => Config.DelayedActions = value);
        gmcm.AddBoolOption(
            mod,
            name: I18n.Gmcm_Controls_Rememberselection,
            tooltip: I18n.Gmcm_Controls_Rememberselection_Tooltip,
            getValue: () => Config.RememberSelection,
            setValue: value => Config.RememberSelection = value);

        gmcm.AddSectionTitle(mod, I18n.Gmcm_Inventory);
        gmcm.AddNumberOption(
            mod,
            name: I18n.Gmcm_Inventory_Max,
            tooltip: I18n.Gmcm_Inventory_Max_Tooltip,
            getValue: () => Config.MaxInventoryItems,
            setValue: value => Config.MaxInventoryItems = value,
            // Any less than the size of a single backpack row (12) and some items become
            // effectively inaccessible without rearranging the inventory. We don't want that.
            min: 12,
            // Limiting this is less about balance and more about preventing overlap or crashes due
            // to the math not working out. If players really want to exceed the limit, they can
            // edit the config.json, but we won't encourage that in the CM.
            max: 24);

        gmcm.AddPageLink(
            mod,
            pageId: CustomMenuPage.ID,
            text: I18n.Gmcm_Custom_Link,
            tooltip: I18n.Gmcm_Custom_Link_Tooltip);
        gmcm.AddPageLink(
            mod,
            pageId: StylePage.ID,
            text: I18n.Gmcm_Style_Link,
            tooltip: I18n.Gmcm_Style_Link_Tooltip);
    }

    private void AddEnumOption<T>(
        string messageId,
        Func<T> getValue,
        Action<T> setValue)
    where T : struct, Enum
    {
        AddEnumOption(messageId, messageId, getValue, setValue);
    }

    private void AddEnumOption<T>(
        string messageId,
        string choiceIdPrefix,
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
            formatAllowedValue: value => translations.Get($"{choiceIdPrefix}.{value}"));
    }
}