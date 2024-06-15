using GenericModConfigMenu.Framework.ModOption;
using GenericModConfigMenu.Framework;
using RadialMenu.Config;
using SpaceShared.UI;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using Newtonsoft.Json.Linq;

namespace RadialMenu.Gmcm;

internal class CustomMenuPage(
    IGenericModMenuConfigApi gmcm,
    GenericModConfigKeybindings? gmcmBindings,
    IManifest mod,
    ITranslationHelper translations,
    TextureHelper textureHelper,
    Func<Configuration> getConfig)
{
    public const string ID = "CustomMenu";

    // Fields related to general shortcut settings.
    private const string FIELD_ID_PREFIX = "focustense.RadialMenu.Custom";
    private const string FIELD_ID_COUNT = $"{FIELD_ID_PREFIX}.ItemCount";
    private const string FIELD_ID_NAME = $"{FIELD_ID_PREFIX}.ItemName";
    private const string FIELD_ID_DESCRIPTION = $"{FIELD_ID_PREFIX}.ItemDescription";
    private const string FIELD_ID_KEYBIND = $"{FIELD_ID_PREFIX}.Keybind";
    // Fields related to the selected item image.
    private const string FIELD_ID_IMAGE_TYPE = $"{FIELD_ID_PREFIX}.ImageType";
    private const string FIELD_ID_IMAGE_ITEM_ID = $"{FIELD_ID_PREFIX}.ImageItemId";
    // Fields related specifically to GMCM sync for an item.
    private const string FIELD_ID_GMCM_MOD = $"{FIELD_ID_PREFIX}.Gmcm.Mod";
    private const string FIELD_ID_GMCM_KEYBIND = $"{FIELD_ID_PREFIX}.Gmcm.Keybind";
    private const string FIELD_ID_GMCM_OVERRIDE = $"{FIELD_ID_PREFIX}.Gmcm.Override";

    private readonly IGenericModMenuConfigApi gmcm = gmcm;
    private readonly GenericModConfigKeybindings? gmcmBindings = gmcmBindings;
    private readonly RegistrationHelper reg = new(gmcm, mod);
    private readonly IManifest mod = mod;
    private readonly ITranslationHelper translations = translations;
    private readonly Func<Configuration> getConfig = getConfig;
    private readonly CustomItemListWidget itemList = new(textureHelper);
    private readonly IconSelectorWidget iconSelector = new(translations);

    protected Configuration Config => getConfig();

    // Filter Actions dropdown by this mod ID. Tracks current value in the GMCM Mod Name dropdown.
    // This setting isn't actually stored anywhere, since it's already part of the GmcmAssociation.
    private string gmcmFilterModId = "";
    // Normally, toggling the checkbox will update the actual GMCM association. However, the box can
    // be toggled when no association is set (i.e. when no action has been selected).
    // If the player toggles the box and then selects an action afterward, we want both settings to
    // apply; this requires tracking the checkbox state independently.
    private bool isGmcmTitleOverrideEnabled;

    public void Setup()
    {
        itemList.Load(Config.CustomMenuItems);
        itemList.SelectedIndexChanged += ItemList_SelectedIndexChanged;
        itemList.SelectedIndexChanging += ItemList_SelectedIndexChanging;

        iconSelector.SelectedItemChanged += IconSelector_SelectedItemChanged;

        RegisterPage();
    }

    /* ----- Setup Methods ----- */

    // Refer to LateLoad method for explanation.
    private bool isLateLoadFinished;

    private void RegisterPage()
    {
        gmcm.AddPage(mod, ID, () => translations.Get("gmcm.custom"));

        reg.AddParagraph(() => translations.Get("gmcm.custom.intro"));
        reg.AddNumberOption(
            name: () => translations.Get("gmcm.custom.count"),
            tooltip: () => translations.Get("gmcm.custom.count.tooltip"),
            fieldId: FIELD_ID_COUNT,
            getValue: () => Config.MaxInventoryItems,
            setValue: value => Config.MaxInventoryItems = value,
            // Technically the menu could have 0 items, but then we'd need a bunch of special-case
            // rendering for our hacked-up custom GMCM UI. Requiring at least one item means we can
            // always display the edit fields without inconsistency.
            min: 1,
            // Keep this the same as the Inventory max size.
            max: 24);
        reg.AddComplexOption(
            name: () => translations.Get("gmcm.custom.items"),
            draw: (spriteBatch, startPosition) => {
                LateLoad();
                itemList.Draw(spriteBatch, startPosition);
            },
            beforeMenuOpened: () =>
            {
                isLateLoadFinished = false;
                itemList.Load(Config.CustomMenuItems);
            },
            afterReset: () => itemList.Load(Config.CustomMenuItems),
            beforeSave: () => Config.CustomMenuItems = new(itemList.Items),
            height: itemList.GetHeight);
        reg.AddSectionTitle(
            text: () => string.Format(
                translations.Get(
                    "gmcm.custom.item.properties",
                    new { index = itemList.SelectedIndex + 1 })));
        reg.AddTextOption(
            name: () => translations.Get("gmcm.custom.item.name"),
            tooltip: () => translations.Get("gmcm.custom.item.name.tooltip"),
            fieldId: FIELD_ID_NAME,
            getValue: () => itemList.SelectedItem.Name,
            setValue: value => itemList.SelectedItem.Name = value);
        reg.AddTextOption(
            name: () => translations.Get("gmcm.custom.item.description"),
            tooltip: () => translations.Get("gmcm.custom.item.description.tooltip"),
            fieldId: FIELD_ID_DESCRIPTION,
            getValue: () => itemList.SelectedItem.Description,
            setValue: value => itemList.SelectedItem.Description = value);
        reg.AddKeybindList(
            name: () => translations.Get("gmcm.custom.item.keybind"),
            tooltip: () => translations.Get("gmcm.custom.item.keybind.tooltip"),
            fieldId: FIELD_ID_KEYBIND,
            getValue: () => new(itemList.SelectedItem.Keybind),
            setValue: value =>
                itemList.SelectedItem.Keybind = value.Keybinds.FirstOrDefault() ?? new());
        // The paragraph after keybind holds the note explaining what is (or is not) synced.
        // Since the text is dynamic, and doesn't necessarily show at all, there's no point in
        // trying to set up an initial value, as GMCM paragraphs are read-only and won't track an
        // updated value anyway (we have to control the widget directly).
        if (gmcmBindings is not null)
        {
            reg.AddParagraph(() => "");
        }
        reg.AddTextOption(
            name: () => translations.Get("gmcm.custom.item.image.type"),
            tooltip: () => translations.Get("gmcm.custom.item.image.type.tooltip"),
            fieldId: FIELD_ID_IMAGE_TYPE,
            getValue: () => itemList.SelectedItem.SpriteSourceFormat.ToString(),
            setValue: value => itemList.SelectedItem.SpriteSourceFormat =
                Enum.Parse<SpriteSourceFormat>(value),
            allowedValues: Enum.GetNames<SpriteSourceFormat>(),
            formatAllowedValue: value => translations.Get($"gmcm.custom.item.image.type.{value}"));
        reg.AddTextOption(
            name: () => translations.Get("gmcm.custom.item.image.item"),
            tooltip: () => translations.Get("gmcm.custom.item.image.item.tooltip"),
            fieldId: FIELD_ID_IMAGE_ITEM_ID,
            getValue: () => itemList.SelectedItem.SpriteSourceFormat == SpriteSourceFormat.ItemIcon
                ? itemList.SelectedItem.SpriteSourcePath
                : "",
            setValue: value =>
            {
                if (itemList.SelectedItem.SpriteSourceFormat == SpriteSourceFormat.ItemIcon)
                {
                    itemList.SelectedItem.SpriteSourcePath = value;
                }
            });
        reg.AddComplexOption(
            name: () => "",
            draw: iconSelector.Draw,
            height: iconSelector.GetHeight);
        if (gmcmBindings is not null)
        {
            reg.AddSectionTitle(() => translations.Get("gmcm.custom.item.gmcm"));
            reg.AddParagraph(() => translations.Get("gmcm.custom.item.gmcm.note"));
            gmcmFilterModId = itemList.SelectedItem?.Gmcm?.ModId ?? "";
            reg.AddTextOption(
                name: () => translations.Get("gmcm.custom.item.gmcm.mod"),
                tooltip: () => translations.Get("gmcm.custom.item.gmcm.mod.tooltip"),
                fieldId: FIELD_ID_GMCM_MOD,
                getValue: () => gmcmFilterModId,
                setValue: value => gmcmFilterModId = value,
                allowedValues: gmcmBindings.AllMods.Keys
                    .Prepend("")
                    .ToArray(),
                formatAllowedValue: modId => !string.IsNullOrEmpty(modId)
                    ? gmcmBindings.AllMods[modId].Name
                    : "--- NONE ---");
            reg.AddTextOption(
                name: () => translations.Get("gmcm.custom.item.gmcm.action"),
                tooltip: () => translations.Get("gmcm.custom.item.gmcm.action.tooltip"),
                fieldId: FIELD_ID_GMCM_KEYBIND,
                getValue: () => GetGmcmAssociationId(itemList.SelectedItem?.Gmcm),
                setValue: value => itemList.SelectedItem!.Gmcm = ResolveGmcmAssociation(
                    value,
                    itemList.SelectedItem.Gmcm?.UseCustomName ?? false),
                // ChoiceOption has an especially troublesome design in which the Choices field is
                // both read-only AND an array, so it is impossible to actually update for
                // filtering; we can still play with the UI Dropdown, but if we select an option
                // that's not in the option's Choices, it will ignore the value and not trigger a
                // notification (OnFieldChanged).
                // The workaround is to let the ChoiceOption itself contain all possible values,
                // from all mods, and apply the filter only to the dropdown element.
                allowedValues: gmcmBindings.AllOptions
                    .Select(GetGmcmAssociationId)
                    .Prepend("") // Allow for no selection
                    .ToArray(),
                formatAllowedValue: id => ResolveGmcmAssociation(id, false)?.FieldName ?? id);
            reg.AddBoolOption(
                name: () => translations.Get("gmcm.custom.item.gmcm.override"),
                tooltip: () => translations.Get("gmcm.custom.item.gmcm.override.tooltip"),
                fieldId: FIELD_ID_GMCM_OVERRIDE,
                getValue: () => itemList.SelectedItem?.Gmcm?.UseCustomName ?? false,
                setValue: value =>
                {
                    isGmcmTitleOverrideEnabled = value;
                    if (itemList.SelectedItem?.Gmcm is GmcmAssociation association)
                    {
                        association.UseCustomName = value;
                    }
                });
        }

        gmcm.OnFieldChanged(mod, (fieldId, value) =>
        {
            switch (fieldId)
            {
                case FIELD_ID_COUNT:
                    itemList.SetCount((int)value);
                    break;
                case FIELD_ID_IMAGE_TYPE:
                    // TODO: Update UI to reflect current setting.
                    break;
                case FIELD_ID_IMAGE_ITEM_ID:
                    if (itemList.SelectedItem?.SpriteSourceFormat == SpriteSourceFormat.ItemIcon)
                    {
                        iconSelector.SelectedItemId = (string)value;
                    }
                    break;
                case FIELD_ID_GMCM_MOD:
                    gmcmFilterModId = (string)value;
                    if (UiInternals.TryGetModConfigMenuAndPage(out var menu, out var page))
                    {
                        ForceUpdateDropdown(
                            page,
                            menu,
                            FIELD_ID_GMCM_KEYBIND,
                            getChoices: GetFilteredGmcmBindingChoices,
                            alwaysResetPosition: true);
                    }
                    break;
                case FIELD_ID_GMCM_KEYBIND:
                    itemList.SelectedItem!.Gmcm =
                        ResolveGmcmAssociation((string)value, isGmcmTitleOverrideEnabled);
                    break;
                case FIELD_ID_GMCM_OVERRIDE:
                    isGmcmTitleOverrideEnabled = (bool)value;
                    if (itemList.SelectedItem?.Gmcm is GmcmAssociation association)
                    {
                        association.UseCustomName = (bool)value;
                    }
                    ForceUpdateGmcmSyncDescription();
                    break;
            }
        });
    }

    private void IconSelector_SelectedItemChanged(object? sender, EventArgs e)
    {
        if (itemList.SelectedItem?.SpriteSourceFormat == SpriteSourceFormat.ItemIcon)
        {
            itemList.SelectedItem.SpriteSourcePath = iconSelector.SelectedItemId;
            if (UiInternals.TryGetModConfigMenuAndPage(out var menu, out var page, ID))
            {
                ForceUpdateTextBox(page, menu, FIELD_ID_IMAGE_ITEM_ID);
            }
        }
    }

    private void ItemList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        ForceUpdateSelectedItemProperties();
    }

    private void ItemList_SelectedIndexChanging(object? sender, EventArgs e)
    {
        ForceSaveSelectedItemProperties();
    }

    // GMCM doesn't give us an "after menu opened" event, probably because of technical limitations
    // in its design: SpecificModConfigMenu breaks the rule of not doing expensive/complicated work
    // from the constructor, and since all the lifecycle events are fired FROM the constructor,
    // there's no clear way to fire another event AFTER the constructor - which would be required in
    // this case, because Mod.ActiveConfigMenu actually calls the ctor to set its value, thus the
    // active menu is actually the previous menu while the ctor is running.
    //
    // To work around this without having to take escalating risks (i.e. Harmony, or some kind of
    // async callback), we can use a little trick; widgets (complex options) will not get _drawn_
    // until after the window is actually opened, and the game is trying to draw it. We can
    // therefore run our late-load logic from the draw callback, which is far from ideal
    // performance-wise, but only happens on the first frame, and afterwards skip it.
    //
    // The StylePage uses a placeholder widget at the bottom and hooks into the beforeMenuOpened
    // callback, but it only manipulates its own internal state; it does not require access to the
    // Table. Since we need to hook the draw callback, we can't use a placeholder widget at the
    // bottom because it won't actually draw unless it's within scroll range; on the other hand,
    // SpaceCore's Table doesn't handle hidden/zero-height rows entirely correctly and inserts the
    // padding regardless, so an "empty" widget at the top will still take up space. Consequently,
    // we can instead tie into the draw of the ItemList widget, which is (or should be) guaranteed
    // to draw on the first frame of the menu because it is so close to the top, and is an actual
    // real UI element that is supposed to take up space.
    private void LateLoad()
    {
        if (isLateLoadFinished)
        {
            return;
        }
        gmcmFilterModId = "";
        ForceUpdateSelectedItemProperties();
        isLateLoadFinished = true;
    }

    /* ----- GMCM association options ----- */

    private void ForceSaveSelectedItemProperties()
    {
        if (!UiInternals.TryGetModConfigPage(out var page, ID))
        {
            return;
        }
        page.ForceSaveOption(FIELD_ID_NAME);
        page.ForceSaveOption(FIELD_ID_DESCRIPTION);
        page.ForceSaveOption(FIELD_ID_KEYBIND);
        page.ForceSaveOption(FIELD_ID_IMAGE_TYPE);
        page.ForceSaveOption(FIELD_ID_IMAGE_ITEM_ID);
        // We don't need to save the CUSTOM_GMCM_MOD here, it's just local state.
        page.ForceSaveOption(FIELD_ID_GMCM_KEYBIND);
        page.ForceSaveOption(FIELD_ID_GMCM_OVERRIDE);
    }

    private void ForceUpdateSelectedItemProperties()
    {
        iconSelector.SelectedItemId =
            itemList.SelectedItem?.SpriteSourceFormat == SpriteSourceFormat.ItemIcon
                ? itemList.SelectedItem.SpriteSourcePath
                : "";

        if (!UiInternals.TryGetModConfigMenuAndPage(out var menu, out var page, ID))
        {
            return;
        }
        menu.ForceUpdateElement<Label>(
            reg.GetTablePosition(FIELD_ID_NAME) - 1,
            label => label.String = translations.Get(
                "gmcm.custom.item.properties",
                new { index = itemList.SelectedIndex + 1 }));
        ForceUpdateTextBox(page, menu, FIELD_ID_NAME);
        ForceUpdateTextBox(page, menu, FIELD_ID_DESCRIPTION);
        ForceUpdateKeybind(page, menu, FIELD_ID_KEYBIND);
        ForceUpdateDropdown(page, menu, FIELD_ID_IMAGE_TYPE);
        ForceUpdateTextBox(page, menu, FIELD_ID_IMAGE_ITEM_ID);
        if (gmcmBindings is not null)
        {
            gmcmFilterModId = itemList.SelectedItem?.Gmcm?.ModId ?? "";
            ForceUpdateDropdown(page, menu, FIELD_ID_GMCM_MOD);
            ForceUpdateDropdown(
                page, menu, FIELD_ID_GMCM_KEYBIND, getChoices: GetFilteredGmcmBindingChoices);
            if (ForceUpdateCheckbox(page, menu, FIELD_ID_GMCM_OVERRIDE))
            {
                ForceUpdateGmcmSyncDescription();
            }
        }
    }

    private void ForceUpdateGmcmSyncDescription()
    {
        if (!UiInternals.TryGetModConfigMenu(out var menu))
        {
            return;
        }
        menu.ForceUpdateElement<Label>(
            reg.GetTablePosition(FIELD_ID_GMCM_MOD) - 5,
            label => label.String = GetGmcmSyncDescriptionText(
                itemList.SelectedItem?.Gmcm,
                (int)menu.Table.Size.X));
    }

    private IEnumerable<string> GetFilteredGmcmBindingChoices()
    {
        return gmcmBindings!.AllOptions
            .Where(opt => opt.ModManifest.UniqueID == gmcmFilterModId)
            .Select(GetGmcmAssociationId)
            .DefaultIfEmpty("");
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
        return UiHelper.BreakParagraph(unbrokenText, maxWidth);
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

    /* ----- GMCM/SpaceCore monkey-patching for immediate updates ----- */

    private bool ForceUpdateCheckbox(
        ModConfigPage page,
        SpecificModConfigMenu menu,
        string fieldId,
        int fieldOffset = 1)
    {
        if (page.ForceResetOption(fieldId) is SimpleModOption<bool> boolOption)
        {
            menu.ForceUpdateElement<Checkbox>(
                reg.GetTablePosition(fieldId) + fieldOffset,
                checkbox => checkbox.Checked = boolOption.Value);
            return true;
        }
        return false;
    }

    private bool ForceUpdateDropdown(
        ModConfigPage page,
        SpecificModConfigMenu menu,
        string fieldId,
        int fieldOffset = 1,
        Func<IEnumerable<string>>? getChoices = null,
        bool alwaysResetPosition = false)
    {
        if (page.ForceResetOption(fieldId) is ChoiceModOption<string> choiceOption)
        {
            menu.ForceUpdateElement<Dropdown>(
                reg.GetTablePosition(fieldId) + fieldOffset,
                dropdown =>
                {
                    dropdown.Value = choiceOption.Value;
                    if (getChoices is null)
                    {
                        return;
                    }
                    var choices = getChoices().ToArray();
                    var nextIndex = alwaysResetPosition
                        ? 0
                        : Math.Max(0, Array.IndexOf(choices, choiceOption.Value));
                    choiceOption.Value = choices[nextIndex];
                    dropdown.Choices = choices;
                    dropdown.Labels = dropdown.Choices
                        .Select(id => choiceOption.FormatChoice(id))
                        .ToArray();
                    dropdown.MaxValuesAtOnce = Math.Min(choices.Length, 5);
                    dropdown.ActiveChoice = nextIndex;
                });
            return true;
        }
        return false;
    }

    private bool ForceUpdateKeybind(
        ModConfigPage page,
        SpecificModConfigMenu menu,
        string fieldId,
        int fieldOffset = 1)
    {
        if (page.ForceResetOption(fieldId) is SimpleModOption<KeybindList> keybindOption)
        {
            menu.ForceUpdateElement<Label>(
                reg.GetTablePosition(fieldId) + fieldOffset,
                label => label.String = keybindOption.Value.ToString());
            return true;
        }
        return false;
    }

    private bool ForceUpdateTextBox(
        ModConfigPage page,
        SpecificModConfigMenu menu,
        string fieldId,
        int fieldOffset = 1)
    {
        if (page.ForceResetOption(fieldId) is SimpleModOption<string> stringOption)
        {
            menu.ForceUpdateElement<Textbox>(
                reg.GetTablePosition(fieldId) + fieldOffset,
                textBox => textBox.String = stringOption.Value);
            return true;
        }
        return false;
    }
}
