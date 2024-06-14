using GenericModConfigMenu;
using GenericModConfigMenu.Framework;
using GenericModConfigMenu.Framework.ModOption;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RadialMenu.Config;
using RadialMenu.Gmcm;
using SpaceShared.UI;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RadialMenu;

internal class ConfigMenu(
    IGenericModMenuConfigApi gmcm,
    GenericModConfigKeyBindings? gmcmBindings,
    IManifest mod,
    ITranslationHelper translations,
    IModContentHelper modContent,
    TextureHelper textureHelper,
    Func<Configuration> getConfig)
{
    private const string CUSTOM_MENU_PAGE_ID = "CustomMenu";
    private const string STYLE_PAGE_ID = "MenuStyle";
    private const string FIELD_ID_PREFIX = "focustense.RadialMenu";
    // Fields related to the custom menu; these update various other parts of the UI.
    private const string FIELD_ID_CUSTOM_COUNT = $"{FIELD_ID_PREFIX}.Custom.ItemCount";
    private const string FIELD_ID_CUSTOM_NAME = $"{FIELD_ID_PREFIX}.Custom.ItemName";
    private const string FIELD_ID_CUSTOM_DESCRIPTION = $"{FIELD_ID_PREFIX}.Custom.ItemDescription";
    private const string FIELD_ID_CUSTOM_KEYBIND = $"{FIELD_ID_PREFIX}.Custom.Keybind";
    private const string FIELD_ID_CUSTOM_GMCM_MOD = $"{FIELD_ID_PREFIX}.Custom.Gmcm.Mod";
    private const string FIELD_ID_CUSTOM_GMCM_KEYBIND = $"{FIELD_ID_PREFIX}.Custom.Gmcm.Keybind";
    private const string FIELD_ID_CUSTOM_GMCM_OVERRIDE = $"{FIELD_ID_PREFIX}.Custom.Gmcm.Override";

    protected Configuration Config => getConfig();

    // Primary ctor properties can't be read-only and we're OCD.
    private readonly IGenericModMenuConfigApi gmcm = gmcm;
    private readonly GenericModConfigKeyBindings? gmcmBindings = gmcmBindings;
    private readonly IManifest mod = mod;
    private readonly ITranslationHelper translations = translations;
    private readonly IModContentHelper modContent = modContent;
    private readonly TextureHelper textureHelper = textureHelper;
    private readonly Func<Configuration> getConfig = getConfig;

    // Individual pages
    private readonly StylePage stylePage =
        new(gmcm, mod, modContent, translations, () => getConfig().Styles);

    // GMCM won't update the actual configuration until it's saved, so we have to store transient
    // values (while editing) from OnFieldChanged in a local lookup.
    private readonly CustomItemListWidget customItems = new(textureHelper);

    public void Setup()
    {
        AddMainOptions();
        AddCustomMenuPage();
        stylePage.Setup();

        gmcm.OnFieldChanged(mod, (fieldId, value) =>
        {
            switch (fieldId)
            {
                case FIELD_ID_CUSTOM_COUNT:
                    customItems.SetCount((int)value);
                    break;
                case FIELD_ID_CUSTOM_GMCM_MOD:
                    gmcmFilterModId = (string)value;
                    if (UiInternals.TryGetModConfigMenu(out var menu))
                    {
                        ForceUpdateGmcmActionDropdownChoices(menu);
                    }
                    break;
                case FIELD_ID_CUSTOM_GMCM_OVERRIDE:
                    if (customItems.SelectedItem?.Gmcm is GmcmAssociation association)
                    {
                        association.UseCustomName = (bool)value;
                    }
                    ForceUpdateGmcmSyncDescription();
                    break;
            }
        });

        customItems.SelectedIndexChanged += CustomItems_SelectedIndexChanged;
        customItems.SelectedIndexChanging += CustomItems_SelectedIndexChanging;
    }

    private void AddMainOptions()
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

        gmcm.AddPageLink(
            mod,
            pageId: CUSTOM_MENU_PAGE_ID,
            text: () => translations.Get("gmcm.custom.link"),
            tooltip: () => translations.Get("gmcm.custom.link.tooltip"));

        gmcm.AddPageLink(
            mod,
            pageId: STYLE_PAGE_ID,
            text: () => translations.Get("gmcm.style.link"),
            tooltip: () => translations.Get("gmcm.style.link.tooltip"));
    }

    private void AddCustomMenuPage()
    {
        customItems.Load(Config.CustomMenuItems);

        gmcm.AddPage(mod, CUSTOM_MENU_PAGE_ID, () => translations.Get("gmcm.custom"));
        gmcm.AddParagraph(
            mod,
            text: () => translations.Get("gmcm.custom.intro"));
        gmcm.AddNumberOption(
            mod,
            name: () => translations.Get("gmcm.custom.count"),
            tooltip: () => translations.Get("gmcm.custom.count.tooltip"),
            fieldId: FIELD_ID_CUSTOM_COUNT,
            getValue: () => Config.MaxInventoryItems,
            setValue: value => Config.MaxInventoryItems = value,
            // Technically the menu could have 0 items, but then we'd need a bunch of special-case
            // rendering for our hacked-up custom GMCM UI. Requiring at least one item means we can
            // always display the edit fields without inconsistency.
            min: 1,
            // Keep this the same as the Inventory max size.
            max: 24);
        gmcm.AddComplexOption(
            mod,
            name: () => translations.Get("gmcm.custom.items"),
            draw: customItems.Draw,
            beforeMenuOpened: () => customItems.Load(Config.CustomMenuItems),
            afterReset: () => customItems.Load(Config.CustomMenuItems),
            beforeSave: customItems.Save,
            height: customItems.GetHeight);
        gmcm.AddSectionTitle(
            mod,
            text: () => string.Format(
                translations.Get(
                    "gmcm.custom.item.properties",
                    new { index = customItems.SelectedIndex + 1 })));
        gmcm.AddTextOption(
            mod,
            name: () => translations.Get("gmcm.custom.item.name"),
            tooltip: () => translations.Get("gmcm.custom.item.name.tooltip"),
            fieldId: FIELD_ID_CUSTOM_NAME,
            getValue: () => customItems.SelectedItem.Name,
            setValue: value => customItems.SelectedItem.Name = value);
        gmcm.AddTextOption(
            mod,
            name: () => translations.Get("gmcm.custom.item.description"),
            tooltip: () => translations.Get("gmcm.custom.item.description.tooltip"),
            fieldId: FIELD_ID_CUSTOM_DESCRIPTION,
            getValue: () => customItems.SelectedItem.Description,
            setValue: value => customItems.SelectedItem.Description = value);
        gmcm.AddKeybindList(
            mod,
            name: () => translations.Get("gmcm.custom.item.keybind"),
            tooltip: () => translations.Get("gmcm.custom.item.keybind.tooltip"),
            fieldId: FIELD_ID_CUSTOM_KEYBIND,
            getValue: () => new(customItems.SelectedItem.Keybind),
            setValue: value =>
                customItems.SelectedItem.Keybind = value.Keybinds.FirstOrDefault() ?? new());
        // The paragraph after keybind holds the note explaining what is (or is not) synced.
        // Since the text is dynamic, and doesn't necessarily show at all, there's no point in
        // trying to set up an initial value, as GMCM paragraphs are read-only and won't track an
        // updated value anyway (we have to control the widget directly).
        if (gmcmBindings is not null)
        {
            gmcm.AddParagraph(mod, () => "");
        }
        if (gmcmBindings is not null)
        {
            gmcm.AddSectionTitle(
                mod,
                text: () => translations.Get("gmcm.custom.item.gmcm"));
            gmcm.AddParagraph(
                mod,
                text: () => translations.Get("gmcm.custom.item.gmcm.note"));
            gmcmFilterModId = customItems.SelectedItem?.Gmcm?.ModId ?? "";
            gmcm.AddTextOption(
                mod,
                name: () => translations.Get("gmcm.custom.item.gmcm.mod"),
                tooltip: () => translations.Get("gmcm.custom.item.gmcm.mod.tooltip"),
                fieldId: FIELD_ID_CUSTOM_GMCM_MOD,
                getValue: () => gmcmFilterModId,
                setValue: value => gmcmFilterModId = value,
                allowedValues: gmcmBindings.AllMods.Keys
                    .Prepend("")
                    .ToArray(),
                formatAllowedValue: modId => !string.IsNullOrEmpty(modId)
                    ? gmcmBindings.AllMods[modId].Name
                    : "--- NONE ---");
            gmcm.AddTextOption(
                mod,
                name: () => translations.Get("gmcm.custom.item.gmcm.action"),
                tooltip: () => translations.Get("gmcm.custom.item.gmcm.action.tooltip"),
                fieldId: FIELD_ID_CUSTOM_GMCM_KEYBIND,
                getValue: () => GetGmcmAssociationId(customItems.SelectedItem?.Gmcm),
                setValue: value => customItems.SelectedItem!.Gmcm = ResolveGmcmAssociation(
                    value,
                    customItems.SelectedItem.Gmcm?.UseCustomName ?? false),
                allowedValues: GetFilteredGmcmBindingChoices().ToArray(),
                formatAllowedValue: id => ResolveGmcmAssociation(id, false)?.FieldName ?? id);
            gmcm.AddBoolOption(
                mod,
                name: () => translations.Get("gmcm.custom.item.gmcm.override"),
                tooltip: () => translations.Get("gmcm.custom.item.gmcm.override.tooltip"),
                fieldId: FIELD_ID_CUSTOM_GMCM_OVERRIDE,
                getValue: () => customItems.SelectedItem?.Gmcm?.UseCustomName ?? false,
                setValue: value =>
                {
                    if (customItems.SelectedItem?.Gmcm is GmcmAssociation association)
                    {
                        association.UseCustomName = value;
                    }
                });
        }
    }

    private string gmcmFilterModId = "";

    private IEnumerable<string> GetFilteredGmcmBindingChoices()
    {
        return gmcmBindings!.AllOptions
            .Where(opt => opt.ModManifest.UniqueID == gmcmFilterModId)
            .Select(GetGmcmAssociationId)
            .DefaultIfEmpty("");
    }

    private static string GetGmcmAssociationId(GmcmAssociation? association)
    {
        return association is not null
            ? $"{association.ModId}:{association.FieldId}"
            : "";
    }

    private static string GetGmcmAssociationId(GenericModConfigKeybindOption option)
    {
        return $"{option.ModManifest.UniqueID}:{option.FieldId}";
    }

    private GmcmAssociation? ResolveGmcmAssociation(string id, bool useCustomName)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }
        var parts = id.Split(':');
        if (parts.Length != 2)
        {
            return null;
        }
        var modId = parts[0];
        var fieldId = parts[1];
        var option = gmcmBindings!.Find(modId, fieldId);
        return option is not null
            ? new()
            {
                ModId = modId,
                FieldId = fieldId,
                FieldName = option.UniqueFieldName,
                UseCustomName = useCustomName,
            }
            : null;
    }

    private void ForceSaveSelectedItemProperties()
    {
        if (!UiInternals.TryGetModConfigPage(out var page, CUSTOM_MENU_PAGE_ID))
        {
            return;
        }
        ForceSaveOption(page, FIELD_ID_CUSTOM_NAME);
        ForceSaveOption(page, FIELD_ID_CUSTOM_DESCRIPTION);
        ForceSaveOption(page, FIELD_ID_CUSTOM_KEYBIND);
        // We don't need to save the CUSTOM_GMCM_MOD here, it's just local state.
        ForceSaveOption(page, FIELD_ID_CUSTOM_GMCM_KEYBIND);
        ForceSaveOption(page, FIELD_ID_CUSTOM_GMCM_OVERRIDE);
    }

    private static void ForceSaveOption(ModConfigPage page, string fieldId)
    {
        page.Options
            .FirstOrDefault(opt => opt.FieldId == fieldId)?
            .BeforeSave();
    }

    private static readonly Dictionary<string, int> ExpectedTablePositions = new()
    {
        { FIELD_ID_CUSTOM_NAME, 8 },
        { FIELD_ID_CUSTOM_DESCRIPTION, 10 },
        { FIELD_ID_CUSTOM_KEYBIND, 12 },
        { FIELD_ID_CUSTOM_GMCM_MOD, 17 },
        { FIELD_ID_CUSTOM_GMCM_KEYBIND, 19 },
        { FIELD_ID_CUSTOM_GMCM_OVERRIDE, 21 },
    };

    private void ForceUpdateSelectedItemProperties()
    {
        if (!UiInternals.TryGetModConfigMenuAndPage(
            out var menu, out var page, CUSTOM_MENU_PAGE_ID))
        {
            return;
        }
        ForceUpdateTableElement<Label>(
            menu.Table,
            ExpectedTablePositions[FIELD_ID_CUSTOM_NAME] - 1,
            label => label.String = translations.Get(
                "gmcm.custom.item.properties",
                new { index = customItems.SelectedIndex + 1 }));
        if (ForceResetOption(page, FIELD_ID_CUSTOM_NAME) is SimpleModOption<string> nameOption)
        {
            ForceUpdateTableElement<Textbox>(
                menu.Table,
                ExpectedTablePositions[FIELD_ID_CUSTOM_NAME] + 1,
                textBox => textBox.String = nameOption.Value);
        }
        if (ForceResetOption(page, FIELD_ID_CUSTOM_DESCRIPTION) is 
            SimpleModOption<string> descriptionOption)
        {
            ForceUpdateTableElement<Textbox>(
                menu.Table,
                ExpectedTablePositions[FIELD_ID_CUSTOM_DESCRIPTION] + 1,
                textBox => textBox.String = descriptionOption.Value);
        }
        if (ForceResetOption(page, FIELD_ID_CUSTOM_KEYBIND) is
            SimpleModOption<KeybindList> keybindOption)
        {
            ForceUpdateTableElement<Label>(
                menu.Table,
                ExpectedTablePositions[FIELD_ID_CUSTOM_KEYBIND] + 1,
                label => label.String = keybindOption.Value.ToString());
        }
        if (gmcmBindings is not null)
        {
            gmcmFilterModId = customItems.SelectedItem?.Gmcm?.ModId ?? "";
            if (ForceResetOption(page, FIELD_ID_CUSTOM_GMCM_MOD) is
                ChoiceModOption<string> gmcmModOption)
            {
                ForceUpdateTableElement<Dropdown>(
                    menu.Table,
                    ExpectedTablePositions[FIELD_ID_CUSTOM_GMCM_MOD] + 1,
                    dropdown => dropdown.Value = gmcmModOption.Value);
                ForceUpdateGmcmActionDropdownChoices(menu);
            }
            if (ForceResetOption(page, FIELD_ID_CUSTOM_GMCM_KEYBIND)
                is ChoiceModOption<string> gmcmBindingOption)
            {
                ForceUpdateTableElement<Dropdown>(
                    menu.Table,
                    ExpectedTablePositions[FIELD_ID_CUSTOM_GMCM_KEYBIND] + 1,
                    dropdown => dropdown.Value = gmcmBindingOption.Value);
            }
            if (ForceResetOption(page, FIELD_ID_CUSTOM_GMCM_OVERRIDE) is
                SimpleModOption<bool> gmcmOverrideOption)
            {
                ForceUpdateTableElement<Checkbox>(
                    menu.Table,
                    ExpectedTablePositions[FIELD_ID_CUSTOM_GMCM_OVERRIDE] + 1,
                    checkbox => checkbox.Checked = gmcmOverrideOption.Value);
                ForceUpdateGmcmSyncDescription();
            }
        }
    }

    private void ForceUpdateGmcmActionDropdownChoices(SpecificModConfigMenu menu)
    {
        ForceUpdateTableElement<Dropdown>(
            menu.Table,
            ExpectedTablePositions[FIELD_ID_CUSTOM_GMCM_KEYBIND] + 1,
            dropdown =>
            {
                dropdown.Choices = GetFilteredGmcmBindingChoices().ToArray();
                dropdown.Labels = dropdown.Choices
                    .Select(id => ResolveGmcmAssociation(id, false)?.FieldName ?? "")
                    .ToArray();
                dropdown.MaxValuesAtOnce = Math.Min(dropdown.Choices.Length, 5);
                dropdown.ActivePosition = 0;
            });
    }

    private void ForceUpdateGmcmSyncDescription()
    {
        if (!UiInternals.TryGetModConfigMenu(out var menu))
        {
            return;
        }
        ForceUpdateTableElement<Label>(
            menu.Table,
            ExpectedTablePositions[FIELD_ID_CUSTOM_GMCM_MOD] - 3,
            label => label.String = GetGmcmSyncDescriptionText(
                customItems.SelectedItem?.Gmcm,
                (int)menu.Table.Size.X));
    }

    private string GetGmcmSyncDescriptionText(GmcmAssociation? association, int maxWidth)
    {
        if (association is null)
        {
            return "";
        }
        var modName = gmcmBindings!.AllMods.TryGetValue(association.ModId, out var mod)
            ? mod.Name
            : "(???)";
        var formatArgs = new
        {
            modName,
            overrideOptionName = translations.Get("gmcm.custom.item.gmcm.override")
        };
        var unbrokenText = association.UseCustomName
            ? translations.Get("gmcm.custom.item.synced.onlykeybind", formatArgs)
            : translations.Get("gmcm.custom.item.synced.all", formatArgs);
        return BreakParagraph(unbrokenText, maxWidth);
}

    private static BaseModOption? ForceResetOption(ModConfigPage page, string fieldId)
    {
        var option = page.Options.FirstOrDefault(opt => opt.FieldId == fieldId);
        option?.AfterReset();
        return option;
    }

    private static void ForceUpdateTableElement<T>(Table table, int childIndex, Action<T> update)
        where T: Element
    {
        var element = table.Children.Length > childIndex ? table.Children[childIndex] : null;
        if (element is T typedElement)
        {
            update(typedElement);
        }
    }

    private static string BreakParagraph(string text, int maxWidth)
    {
        // Copied almost verbatim from SpecificModConfigMenu.cs. We want it to look the same.
        var sb = new StringBuilder(text.Length + 50);
        string nextLine = "";
        foreach (string word in text.Split(' '))
        {
            if (word == "\n")
            {
                sb.AppendLine(nextLine);
                nextLine = "";
                continue;
            }
            if (nextLine == "")
            {
                nextLine = word;
                continue;
            }
            string possibleLine = $"{nextLine} {word}".Trim();
            if (Label.MeasureString(possibleLine, font: Game1.smallFont).X <= maxWidth)
            {
                nextLine = possibleLine;
                continue;
            }
            sb.AppendLine(nextLine);
            nextLine = word;
        }
        if (nextLine != "")
            sb.AppendLine(nextLine);
        return sb.ToString();
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

    private void CustomItems_SelectedIndexChanged(object? sender, EventArgs e)
    {
        ForceUpdateSelectedItemProperties();
    }

    private void CustomItems_SelectedIndexChanging(object? sender, EventArgs e)
    {
        ForceSaveSelectedItemProperties();
    }
}