namespace RadialMenu
{
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
        /// Maximum number of items to display in the inventory radial.
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
    }
}
