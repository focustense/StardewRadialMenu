using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RadialMenu.Config;
using StardewModdingAPI;
using StardewValley;

namespace RadialMenu;

internal class ConfigMenu(
    IGenericModMenuConfigApi gmcm,
    IManifest mod,
    ITranslationHelper translations,
    IModContentHelper modContent,
    TextureHelper textureHelper,
    Func<Configuration> getConfig)
{
    private const string CUSTOM_MENU_PAGE_ID = "CustomMenu";
    private const string STYLE_PAGE_ID = "MenuStyle";
    private const string FIELD_ID_PREFIX = "focustense.RadialMenu";
    // Fields related to menu colors; these update the preview.
    private const string FIELD_ID_INNER_COLOR = $"{FIELD_ID_PREFIX}.Style.InnerBackgroundColor";
    private const string FIELD_ID_OUTER_COLOR = $"{FIELD_ID_PREFIX}.Style.OuterBackgroundColor";
    private const string FIELD_ID_HIGHLIGHT_COLOR = $"{FIELD_ID_PREFIX}.Style.HighlightColor";
    private const string FIELD_ID_CURSOR_COLOR = $"{FIELD_ID_PREFIX}.Style.CursorColor";
    private const string FIELD_ID_TITLE_COLOR = $"{FIELD_ID_PREFIX}.Style.TitleColor";
    private const string FIELD_ID_DESCRIPTION_COLOR = $"{FIELD_ID_PREFIX}.Style.DescriptionColor";
    // Fields related to the custom menu; these update various other parts of the UI.
    private const string FIELD_ID_CUSTOM_COUNT = $"{FIELD_ID_PREFIX}.Custom.ItemCount";
    // A smaller image, scaled up, would be much, much more efficient to use here.
    // But we can't, because https://github.com/spacechase0/StardewValleyMods/issues/387.
    private const int PREVIEW_SIZE = 400;
    private const int PREVIEW_LENGTH = PREVIEW_SIZE * PREVIEW_SIZE;

    protected Configuration Config => getConfig();

    // These assets are initialized in Setup(), so we assume they're not null once we need them.
    private readonly Color[] innerPreviewData = new Color[PREVIEW_LENGTH];
    private readonly Color[] outerPreviewData = new Color[PREVIEW_LENGTH];
    private readonly Color[] highlightPreviewData = new Color[PREVIEW_LENGTH];
    private readonly Color[] cursorPreviewData = new Color[PREVIEW_LENGTH];
    private readonly Color[] itemsPreviewData = new Color[PREVIEW_LENGTH];
    private readonly Color[] titlePreviewData = new Color[PREVIEW_LENGTH];
    private readonly Color[] descriptionPreviewData = new Color[PREVIEW_LENGTH];
    // GMCM won't update the actual configuration until it's saved, so we have to store transient
    // values (while editing) from OnFieldChanged in a local lookup.
    private readonly CustomItemListWidget customItems = new(textureHelper);
    private readonly Dictionary<string, Color> liveColorValues = [];
    // Menu preview is owned by us.
    private Texture2D menuPreview = null!;

    public void Setup()
    {
        modContent.Load<Texture2D>("assets/preview-inner.png").GetData(innerPreviewData);
        modContent.Load<Texture2D>("assets/preview-outer.png").GetData(outerPreviewData);
        modContent.Load<Texture2D>("assets/preview-selection.png").GetData(highlightPreviewData);
        modContent.Load<Texture2D>("assets/preview-cursor.png").GetData(cursorPreviewData);
        modContent.Load<Texture2D>("assets/preview-items.png").GetData(itemsPreviewData);
        modContent.Load<Texture2D>("assets/preview-title.png").GetData(titlePreviewData);
        modContent.Load<Texture2D>("assets/preview-description.png").GetData(descriptionPreviewData);

        menuPreview = new(Game1.graphics.GraphicsDevice, PREVIEW_SIZE, PREVIEW_SIZE);

        AddMainOptions();
        AddCustomMenuPage();
        AddStylePage();
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
            // Technically the menu could have 0 items, but then we need a bunch of special-case
            // rendering for our hacked-up custom GMCM UI.
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
        gmcm.AddParagraph(mod, () => "Test placeholder");
    }

    private void AddStylePage()
    {
        liveColorValues[FIELD_ID_INNER_COLOR] = Config.Styles.InnerBackgroundColor;
        liveColorValues[FIELD_ID_OUTER_COLOR] = Config.Styles.OuterBackgroundColor;
        liveColorValues[FIELD_ID_HIGHLIGHT_COLOR] = Config.Styles.HighlightColor;
        liveColorValues[FIELD_ID_CURSOR_COLOR] = Config.Styles.CursorColor;
        liveColorValues[FIELD_ID_TITLE_COLOR] = Config.Styles.SelectionTitleColor;
        liveColorValues[FIELD_ID_DESCRIPTION_COLOR] = Config.Styles.SelectionDescriptionColor;
        UpdateMenuPreview();

        gmcm.AddPage(mod, STYLE_PAGE_ID, () => translations.Get("gmcm.style"));
        gmcm.AddImage(mod, () => menuPreview, scale: 1);
        gmcm.AddSectionTitle(
            mod,
            text: () => translations.Get("gmcm.style.colors"));
        gmcm.AddParagraph(
            mod,
            text: () => translations.Get("gmcm.style.colors.slidernote"));
        AddColorOption(
            FIELD_ID_INNER_COLOR,
            name: () => translations.Get("gmcm.style.colors.inner"),
            tooltip: () => translations.Get("gmcm.style.colors.inner.tooltip"),
            getColor: () => Config.Styles.InnerBackgroundColor,
            setColor: color => Config.Styles.InnerBackgroundColor = color);
        AddColorOption(
            FIELD_ID_OUTER_COLOR,
            name: () => translations.Get("gmcm.style.colors.outer"),
            tooltip: () => translations.Get("gmcm.style.colors.outer.tooltip"),
            getColor: () => Config.Styles.OuterBackgroundColor,
            setColor: color => Config.Styles.OuterBackgroundColor = color);
        AddColorOption(
            FIELD_ID_HIGHLIGHT_COLOR,
            name: () => translations.Get("gmcm.style.colors.highlight"),
            tooltip: () => translations.Get("gmcm.style.colors.highlight.tooltip"),
            getColor: () => Config.Styles.HighlightColor,
            setColor: color => Config.Styles.HighlightColor = color);
        AddColorOption(
            FIELD_ID_CURSOR_COLOR,
            name: () => translations.Get("gmcm.style.colors.cursor"),
            tooltip: () => translations.Get("gmcm.style.colors.cursor.tooltip"),
            getColor: () => Config.Styles.CursorColor,
            setColor: color => Config.Styles.CursorColor = color);
        AddColorOption(
            FIELD_ID_TITLE_COLOR,
            name: () => translations.Get("gmcm.style.colors.title"),
            tooltip: () => translations.Get("gmcm.style.colors.title.tooltip"),
            getColor: () => Config.Styles.SelectionTitleColor,
            setColor: color => Config.Styles.SelectionTitleColor = color);
        AddColorOption(
            FIELD_ID_DESCRIPTION_COLOR,
            name: () => translations.Get("gmcm.style.colors.description"),
            tooltip: () => translations.Get("gmcm.style.colors.description.tooltip"),
            getColor: () => Config.Styles.SelectionDescriptionColor,
            setColor: color => Config.Styles.SelectionDescriptionColor= color);

        gmcm.AddSectionTitle(
            mod,
            text: () => translations.Get("gmcm.style.dimensions"));
        gmcm.AddParagraph(
            mod,
            text: () => translations.Get("gmcm.style.dimensions.note"));
        gmcm.AddNumberOption(
            mod,
            name: () => translations.Get("gmcm.style.dimensions.inner"),
            tooltip: () => translations.Get("gmcm.style.dimensions.inner.tooltip"),
            getValue: () => Config.Styles.InnerRadius,
            setValue: value => Config.Styles.InnerRadius = value,
            min: 200,
            max: 400,
            interval: 25);
        gmcm.AddNumberOption(
            mod,
            name: () => translations.Get("gmcm.style.dimensions.outer"),
            tooltip: () => translations.Get("gmcm.style.dimensions.outer.tooltip"),
            getValue: () => Config.Styles.OuterRadius,
            setValue: value => Config.Styles.OuterRadius = value,
            min: 100,
            max: 200,
            interval: 10);
        gmcm.AddNumberOption(
            mod,
            name: () => translations.Get("gmcm.style.dimensions.gap"),
            tooltip: () => translations.Get("gmcm.style.dimensions.gap.tooltip"),
            getValue: () => Config.Styles.GapWidth,
            setValue: value => Config.Styles.GapWidth = value,
            min: 0,
            max: 20);
        gmcm.AddNumberOption(
            mod,
            name: () => translations.Get("gmcm.style.dimensions.cursor.size"),
            tooltip: () => translations.Get("gmcm.style.dimensions.cursor.size.tooltip"),
            getValue: () => Config.Styles.CursorSize,
            setValue: value => Config.Styles.CursorSize = value,
            min: 16,
            max: 64,
            interval: 4);
        gmcm.AddNumberOption(
            mod,
            name: () => translations.Get("gmcm.style.dimensions.cursor.distance"),
            tooltip: () => translations.Get("gmcm.style.dimensions.cursor.distance.tooltip"),
            getValue: () => Config.Styles.CursorDistance,
            setValue: value => Config.Styles.CursorDistance = value,
            min: 0,
            max: 16);
        gmcm.AddNumberOption(
            mod,
            name: () => translations.Get("gmcm.style.dimensions.itemheight"),
            tooltip: () => translations.Get("gmcm.style.dimensions.itemheight.tooltip"),
            getValue: () => Config.Styles.MenuSpriteHeight,
            setValue: value => Config.Styles.MenuSpriteHeight = value,
            min: 16,
            max: 64,
            interval: 8);
        gmcm.AddNumberOption(
            mod,
            name: () => translations.Get("gmcm.style.dimensions.selectionheight"),
            tooltip: () => translations.Get("gmcm.style.dimensions.selectionheight.tooltip"),
            getValue: () => Config.Styles.SelectionSpriteHeight,
            setValue: value => Config.Styles.SelectionSpriteHeight = value,
            min: 32,
            max: 256,
            interval: 16);

        gmcm.OnFieldChanged(mod, (fieldId, value) =>
        {
            switch (fieldId)
            {
                case FIELD_ID_OUTER_COLOR:
                case FIELD_ID_INNER_COLOR:
                case FIELD_ID_HIGHLIGHT_COLOR:
                case FIELD_ID_CURSOR_COLOR:
                case FIELD_ID_TITLE_COLOR:
                case FIELD_ID_DESCRIPTION_COLOR:
                    if (HexColor.TryParse((string)value, out var color))
                    {
                        liveColorValues[fieldId] = color;
                        UpdateMenuPreview();
                    }
                    break;
                case FIELD_ID_CUSTOM_COUNT:
                    customItems.SetCount((int)value);
                    break;
            }
        });
    }

    private void AddColorOption(
        string fieldId,
        Func<string> name,
        Func<HexColor> getColor,
        Action<HexColor> setColor,
        Func<string>? tooltip = null)
    {
        gmcm.AddTextOption(
            mod,
            name: name,
            tooltip: tooltip,
            fieldId: fieldId,
            getValue: () => getColor().ToString(),
            setValue: hexString =>
            {
                if (HexColor.TryParse(hexString, out var hexColor))
                {
                    setColor(hexColor);
                }
            });
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

    private void UpdateMenuPreview()
    {
        Color[] previewData = new Color[PREVIEW_LENGTH];
        for (var i = 0; i < PREVIEW_LENGTH; i++)
        {
            var innerColor =
                Premultiply(liveColorValues[FIELD_ID_INNER_COLOR], innerPreviewData[i].A);
            var outerColor =
                Premultiply(liveColorValues[FIELD_ID_OUTER_COLOR], outerPreviewData[i].A);
            var highlightColor =
                Premultiply(liveColorValues[FIELD_ID_HIGHLIGHT_COLOR], highlightPreviewData[i].A);
            var cursorColor =
                Premultiply(liveColorValues[FIELD_ID_CURSOR_COLOR], cursorPreviewData[i].A);
            var itemsColor = Premultiply(Color.DarkGreen, itemsPreviewData[i].A);
            var titleColor =
                Premultiply(liveColorValues[FIELD_ID_TITLE_COLOR], titlePreviewData[i].A);
            var descriptionColor = Premultiply(
                liveColorValues[FIELD_ID_DESCRIPTION_COLOR],
                descriptionPreviewData[i].A);
            previewData[i] = innerColor
                .BlendPremultiplied(outerColor)
                .BlendPremultiplied(highlightColor)
                .BlendPremultiplied(cursorColor)
                .BlendPremultiplied(itemsColor)
                .BlendPremultiplied(titleColor)
                .BlendPremultiplied(descriptionColor);
        }
        menuPreview.SetData(previewData);
    }

    private static Color Premultiply(Color color, byte alpha)
    {
        var a = (color.A / 255f) * (alpha / 255f);
        return new Color(
            (byte)(color.R * a),
            (byte)(color.G * a),
            (byte)(color.B * a),
            (byte)(a * 255));
    }
}

static class ColorExt
{
    public static Color BlendPremultiplied(this Color under, Color over)
    {
        var a1 = over.A / 255f;
        var a2 = under.A / 255f;

        byte r = (byte)MathF.Min(255, over.R + under.R * (1 - a1));
        byte g = (byte)MathF.Min(255, over.G + under.G * (1 - a1));
        byte b = (byte)MathF.Min(255, over.B + under.B * (1 - a1));
        byte a = (byte)(255 * (a1 + a2 * (1 - a1)));
        return new Color(r, g, b, a);
    }
}