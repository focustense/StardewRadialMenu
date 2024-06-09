using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace RadialMenu
{
    public class Configuration
    {
        /// <summary>
        /// Dead zone for the left/right trigger buttons for activating/deactivating the menu.
        /// </summary>
        /// <remarks>
        /// Triggers are generally used as regular buttons in Stardew Valley, but are technically
        /// analog inputs. Due to a variety of technical issues, this mod needs to ignore the
        /// simpler on/off behavior and read the analog input directly. Increase the dead zone if
        /// necessary to prevent accidental presses, or reduce it for hair-trigger response.
        /// </remarks>
        public float TriggerDeadZone { get; set; } = 0.2f;

        /// <summary>
        /// Customizes which thumbstick is used to select items from the radial menus.
        /// </summary>
        public ThumbStickPreference ThumbStickPreference { get; set; }

        /// <summary>
        /// Dead-zone for the thumbstick when selecting from a radial menu.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Only applies to menu selection; changing this setting will not affect the dead zone used
        /// for any other mods or controls in the vanilla game.
        /// </para>
        /// <para>
        /// Many, if not most controllers suffer from drift issues in the analog sticks. Setting
        /// this value too low could cause items to get selected even when the thumbstick has not
        /// been moved.
        /// </para>
        /// </remarks>
        public float ThumbStickDeadZone { get; set; } = 0.2f;
        /// <summary>
        /// How to activate the selected item; refer to <see cref="ItemActivation"/>.
        /// </summary>
        public ItemActivation Activation { get; set; }

        /// <summary>
        /// Maximum number of items to display in the inventory radial menu.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This value is meant to approximate the same "backpack page" that appears in the game's
        /// toolbar, and uses the same default value. Empty slots will not show up in the menu, but
        /// are still counted against this limit, to provide a similar-to-vanilla experience where
        /// only the first "page" of items can be quick-selected.
        /// </para>
        /// <para>
        /// For example, given the default value of 12, if the fully-upgraded backpack contains 30
        /// items, but the first row has 8 empty slots, then only 4 items will show up in the radial
        /// menu.
        /// </para>
        /// </remarks>
        public int MaxInventoryItems { get; set; } = 12;

        /// <summary>
        /// List of all items configured for the custom radial menu.
        /// </summary>
        public List<CustomMenuItem> CustomMenuItems { get; set; } = [];

        /// <summary>
        /// Debug option that prints the list of all registered GMCM key bindings when starting the
        /// game.
        /// </summary>
        /// <remarks>
        /// Can be helpful for manually editing the <c>config.json</c>, since the field IDs/names
        /// are not documented anywhere except in the source code of the GMCM-enabled mods.
        /// </remarks>
        public bool DumpAvailableKeyBindingsOnStartup { get; set; }
    }

    /// <summary>
    /// Methods of activating an item in a radial menu.
    /// </summary>
    public enum ItemActivation
    {
        /// <summary>
        /// Activate by pressing the action button (typically the "A" button).
        /// </summary>
        /// <remarks>
        /// Most often used in combination with <see cref="ThumbStickPreference.AlwaysLeft"/>, since
        /// pressing the action button while using the right thumbstick is awkward.
        /// </remarks>
        ActionButtonPress,
        /// <summary>
        /// Activate by pressing the same thumbstick that is being used for selection.
        /// </summary>
        /// <remarks>
        /// This is a middle-of-the-road option that provides the benefit of explicit activation,
        /// without needing to worry about accidental trigger release, but is more compatible with
        /// right-thumbstick preferences (<see cref="ThumbStickPreference.AlwaysRight"/> or
        /// <see cref="ThumbStickPreference.SameAsTrigger"/>). However, ease of use depends heavily
        /// on the controller, and on some controllers it may be difficult to press the thumbstick
        /// without moving it.
        /// </remarks>
        ThumbStickPress,
        /// <summary>
        /// Activate whichever item was last selected when the menu trigger is released.
        /// </summary>
        /// <remarks>
        /// This mode is better optimized for fast-paced (speedrun/minmax) gameplay as it allows the
        /// radials to be operated using only two inputs instead of three. However, players who are
        /// not very experienced with radial menus might find it error-prone due to accidental
        /// movement of the thumbstick while releasing the trigger.
        /// </remarks>
        TriggerRelease
    };

    /// <summary>
    /// Controls which thumbstick is used to select items from a radial menu.
    /// </summary>
    /// <remarks>
    /// Thumbstick preference typically needs to be coordinated with <see cref="ItemActivation"/>,
    /// i.e. in order to avoid very awkward gestures such as right trigger + right thumb + action
    /// button on the right-hand side of the controller. If you are using a traditional controller
    /// layout, it is recommended to use either <see cref="ItemActivation.ThumbStickPress"/> or
    /// <see cref="ItemActivation.TriggerRelease"/> when choosing any preference other than
    /// <see cref="AlwaysLeft"/>.
    /// </remarks>
    public enum ThumbStickPreference
    {
        /// <summary>
        /// Always use the left thumbstick, regardless of which menu is open.
        /// </summary>
        AlwaysLeft,
        /// <summary>
        /// Always use the right thumbstick, regardless of which menu is open.
        /// </summary>
        AlwaysRight,
        /// <summary>
        /// Use the thumbstick that is on the same side as the trigger button used to open the menu.
        /// </summary>
        SameAsTrigger
    };

    /// <summary>
    /// Configuration for an item in the custom radial menu.
    /// </summary>
    public class CustomMenuItem
    {
        /// <summary>
        /// Short name for the item; displayed as the title when selected.
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Item description, to display under the title when selected.
        /// </summary>
        public string Description { get; set; } = "";
        /// <summary>
        /// Key binding specifying the keys to simulate pressing when activated.
        /// </summary>
        public Keybind Keybind { get; set; } = new();
        /// <summary>
        /// Specifies the format of the <see cref="SpriteSourcePath"/>.
        /// </summary>
        public SpriteSourceFormat SpriteSourceFormat { get; set; }
        /// <summary>
        /// Path identifying which sprite to display for this item in the menu.
        /// </summary>
        /// <remarks>
        /// The format depends on the <see cref="SpriteSourceFormat"/> setting:
        /// <list type="bullet">
        /// <item>If the format is <see cref="SpriteSourceFormat.ItemIcon"/>, then this is the
        /// item's <see cref="StardewValley.Item.QualifiedItemId"/>, such as <c>O(128)</c>. If an
        /// unqualified ID is used, the menu will make an attempt anyway, but ID conflicts are
        /// possible and the wrong icon may get displayed.</item>
        /// <item>If the format is <see cref="SpriteSourceFormat.TextureRect"/>, then this is a
        /// string in the form of <c>{assetPath}:(left,top,width,height)</c>. For example, to
        /// display the sprite for the Lucky Purple Shorts, use the string
        /// <c>maps/springobjects:(368,32,16,16)</c>.</item>
        /// </list>
        /// </remarks>
        public string SpriteSourcePath { get; set; } = "";
        /// <summary>
        /// Associates this item with a Generic Mod Config Menu key binding, which will cause it to
        /// automatically update the <see cref="Name"/>, <see cref="Description"/> and
        /// <see cref="Keybind"/>.
        /// </summary>
        public GmcmAssociation? Gmcm { get; set; }
    }

    /// <summary>
    /// Configuration for a radial menu item associated with a Generic Mod Config Menu key binding.
    /// </summary>
    /// <remarks>
    /// GMCM bindings will, by default, use the mod name as the <see cref="CustomMenuItem.Name"/>
    /// and the field name as the <see cref="CustomMenuItem.Description"/>. These can be overridden
    /// by setting <see cref="UseCustomName"/> to <c>true</c>.
    /// </remarks>
    public class GmcmAssociation
    {
        /// <summary>
        /// The unique identifier of the mod that owns the keybinding associated with this item in
        /// Generic Mod Config Menu.
        /// </summary>
        public string ModId { get; set; } = "";
        /// <summary>
        /// The unique identifier of the keybinding field associated with this item in Generic Mod
        /// Config Menu.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is populated automatically as a backup in case <see cref="GmcmFieldName"/> fails to
        /// turn up any result, e.g. due to a language change. End users generally will not know
        /// this ID as it is internal to GMCM. If changing the <see cref="GmcmFieldName"/> manually
        /// in <c>config.json</c> instead of using GMCM to configure the menu, clear this field so
        /// that it does not accidentally pick up the old value.
        /// </para>
        /// <para>
        /// One special case is if a mod defines multiple keybindings with the same name, under the
        /// same section title, on the same page. While mods generally shouldn't do this, since it's
        /// confusing, some do anyway, and in these cases the field ID is used in addition to the
        /// field name as a disambiguator during lookups.
        /// </para>
        /// <para>
        /// This property has no utility whatsoever if the mod author does not specify explicit
        /// field IDs, since the autogenerated field IDs are not stable across game launches.
        /// However, in the relatively rare event that the mod author did so, the field ID is a
        /// better choice for fallback/disambiguation than the existing key binding (see comments
        /// on <see cref="FieldName"/>).
        /// </para>
        /// </remarks>
        public string FieldId { get; set; } = "";
        /// <summary>
        /// The visible name of the keybinding field associated with this item in Generic Mod Config
        /// Menu.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Lookups are attempted by name first, then by ID (<see cref="GmcmFieldId"/>), so that
        /// users can change the value in <c>config.json</c> without needing to know the ID. If no
        /// match is found, the <see cref="GmcmFieldId"/> is used as fallback lookup.
        /// </para>
        /// <para>
        /// In many if not most cases, the field ID is not stable because the mod author has not
        /// opted into that feature by providing explicit IDs. Consequently, a final fallback and/or
        /// disambiguation can be performed using the existing <see cref="CustomMenuItem.Keybind"/>.
        /// Note, however, that keybinds are not guaranteed to be any more unique than field names,
        /// and it is entirely possible for a mod to support multiple key bindings that are normally
        /// bound to the same key(s), and either use the same name for each setting (requiring
        /// disambiguation) or change the name in a future update (fallback).
        /// </para>
        /// <para>
        /// Matching is always best-effort. This mod will always warn when a match wasn't possible,
        /// and continue to use the last-known <see cref="CustomMenuItem.Keybind"/> until the link
        /// is restored by updating the field or binding to one that matches. However, if a target
        /// mod has completely changed its keybinding settings, it may need to be set up again in
        /// the menu.
        /// </para>
        /// </remarks>
        public string FieldName { get; set; } = "";
        /// <summary>
        /// If set, only the <see cref="CustomMenuItem.Keybind"/> will track the current setting in
        /// GMCM; <see cref="CustomMenuItem.Name"/> and <see cref="CustomMenuItem.Description"/>
        /// will retain their current values even if they change in GMCM.
        /// </summary>
        /// <remarks>
        /// Use this to set friendlier names and descriptions when the GMCM page uses names that are
        /// very generic or confusing in the context of a radial menu, e.g. to change a name like
        /// "Keybind" to "Toggle &lt;Feature Name&gt;".
        /// </remarks>
        public bool UseCustomName { get; set; }
    }

    /// <summary>
    /// Sources from which the sprite for a custom menu item can be taken.
    /// </summary>
    public enum SpriteSourceFormat
    {
        /// <summary>
        /// Use the icon of an existing in-game item.
        /// </summary>
        ItemIcon,
        /// <summary>
        /// Draw an arbitrary area from any texture, tile sheet, etc.
        /// </summary>
        TextureRect,
    }
}
