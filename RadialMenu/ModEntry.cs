using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Reflection;

namespace RadialMenu
{
    record PreMenuState(bool WasFrozen);

    public class ModEntry : Mod
    {
        private const string GMCM_MOD_ID = "spacechase0.GenericModConfigMenu";

        private Configuration config = null!;
        private IGenericModMenuConfigApi? configMenu;
        private GenericModConfigSync? gmcmSync;
        private MenuItemBuilder menuItemBuilder = null!;
        private IReadOnlyList<MenuItem> activeMenuItems = [];
        private Cursor cursor = null!;
        private Painter painter = null!;
        private KeybindActivator keybindActivator = null!;
        private PreMenuState preMenuState = null!;
        private Action? pendingActivation;

        public override void Entry(IModHelper helper)
        {
            config = Helper.ReadConfig<Configuration>();
            menuItemBuilder = new(helper.GameContent, ActivateCustomMenuItem, Monitor);
            cursor = new Cursor()
            {
                ThumbStickPreference = config.ThumbStickPreference,
                ThumbStickDeadZone = config.ThumbStickDeadZone,
                TriggerDeadZone = config.TriggerDeadZone,
            };
            painter = new(Game1.graphics.GraphicsDevice);
            keybindActivator = new(helper.Input);
            preMenuState = new(Game1.freezeControls);

            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            // For optimal latency: handle input before the Update loop, perform actions/rendering after.
            helper.Events.GameLoop.UpdateTicking += GameLoop_UpdateTicking;
            helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
            helper.Events.Input.ButtonsChanged += Input_ButtonsChanged;
            helper.Events.Display.RenderedHud += Display_RenderedHud;
        }

        private void Display_RenderedHud(object? sender, RenderedHudEventArgs e)
        {
            if (cursor.ActiveMenu is null)
            {
                return;
            }
            painter.Paint(
                e.SpriteBatch,
                Game1.uiViewport,
                cursor.CurrentTarget?.SelectedIndex ?? -1,
                cursor.CurrentTarget?.Angle);
        }

        [EventPriority(EventPriority.Low - 10)]
        private void GameLoop_GameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            configMenu = Helper.ModRegistry.GetApi<IGenericModMenuConfigApi>(GMCM_MOD_ID);
            LoadGmcmKeybindings();
        }

        private void GameLoop_UpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
            {
                return;
            }
            if (cursor.WasMenuChanged && cursor.ActiveMenu != null)
            {
                Game1.playSound("shwip");
            }
            else if (cursor.WasTargetChanged && cursor.CurrentTarget != null)
            {
                Game1.playSound("smallSelect");
            }
            if (pendingActivation is not null)
            {
                pendingActivation.Invoke();
                pendingActivation = null;
                cursor.SuppressUntilTriggerRelease();
                RestorePreMenuState();
            }
        }

        private void GameLoop_UpdateTicking(object? sender, UpdateTickingEventArgs e)
        {
            if (!Context.CanPlayerMove && cursor.ActiveMenu == null)
            {
                return;
            }
            cursor.GamePadState = GetRawGamePadState();
            cursor.UpdateActiveMenu();
            if (cursor.WasMenuChanged)
            {
                if (cursor.ActiveMenu is not null)
                {
                    preMenuState = new(Game1.freezeControls);
                    Game1.player.completelyStopAnimatingOrDoingAction();
                    Game1.freezeControls = true;
                }
                else
                {
                    if (config.Activation == ItemActivation.TriggerRelease)
                    {
                        pendingActivation = GetSelectedItemActivation();
                    }
                    RestorePreMenuState();
                }
                UpdateMenuItems(cursor.ActiveMenu);
                painter.Items = activeMenuItems;
            }
            cursor.UpdateCurrentTarget(activeMenuItems.Count);

            // Here be dragons: because the triggers are analog values and SMAPI uses a deadzone,
            // it will race with Stardew (and usually lose), with the practical symptom being that
            // if we try to do the suppression in the "normal" spot (e.g. on input events), Stardew
            // still receives not only the initial trigger press, but another spurious "press" when
            // the trigger is released.
            //
            // The "solution" - preemptively suppress all trigger presses whenever the player is
            // being controlled, and punch through SMAPI's controller abstraction when reading the
            // trigger values ourselves. This will almost certainly cause incompatibilities with any
            // other mods that want to leverage the trigger buttons outside of menus... but then
            // again, the overall functionality is inherently incompatible regardless of hackery.
            Helper.Input.Suppress(SButton.LeftTrigger);
            Helper.Input.Suppress(SButton.RightTrigger);
        }

        private void Input_ButtonsChanged(object? sender, ButtonsChangedEventArgs e)
        {
            if (!Context.IsWorldReady
                || cursor.ActiveMenu is null
                || cursor.CurrentTarget is null
                || config.Activation == ItemActivation.TriggerRelease)
            {
                return;
            }
            foreach (var button in e.Pressed)
            {
                if (IsActivationButton(button))
                {
                    pendingActivation = GetSelectedItemActivation();
                    Helper.Input.Suppress(button);
                    return;
                }
            }
        }

        private static GamePadState GetRawGamePadState()
        {
            return Game1.playerOneIndex >= PlayerIndex.One
                ? GamePad.GetState(Game1.playerOneIndex)
                : new GamePadState();
        }

        private Action? GetSelectedItemActivation()
        {
            var itemIndex = cursor.CurrentTarget?.SelectedIndex;
            return itemIndex < activeMenuItems.Count
                ? activeMenuItems[itemIndex.Value].Activate : null;
        }

        private bool IsActivationButton(SButton button)
        {
            return config.Activation switch
            {
                ItemActivation.ActionButtonPress => button.IsActionButton(),
                ItemActivation.ThumbStickPress => cursor.IsThumbStickForActiveMenu(button),
                _ => false,
            };
        }

        private void LoadGmcmKeybindings()
        {
            if (configMenu is null)
            {
                Monitor.Log(
                    $"Couldn't read global keybindings; mod {GMCM_MOD_ID} is not installed.",
                    LogLevel.Warn);
                return;
            }
            Monitor.Log("Generic Mod Config Menu is loaded; reading keybindings.", LogLevel.Info);
            try
            {
                var gmcmKeybindings = GenericModConfigKeyBindings.Load(configMenu, Helper.Reflection);
                Monitor.Log("Finished reading keybindings from GMCM.", LogLevel.Info);
                if (config.DumpAvailableKeyBindingsOnStartup)
                {
                    foreach (var option in gmcmKeybindings.AllOptions)
                    {
                        Monitor.Log(
                            "Found keybind option: " +
                            $"[{option.ModManifest.UniqueID}] - {option.UniqueFieldName}",
                            LogLevel.Info);
                    }
                }
                gmcmSync = new(config, gmcmKeybindings, Monitor);
                gmcmSync.SyncAll();
                Helper.WriteConfig(config);
            }
            catch (Exception ex)
            when (ex is InvalidOperationException || ex is TargetInvocationException)
            {
                Monitor.Log(
                    $"Couldn't read global keybindings; the current version of {GMCM_MOD_ID} is " +
                    $"not compatible.\n{ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}",
                    LogLevel.Error);
            }
        }

        private void RestorePreMenuState()
        {
            Game1.freezeControls = preMenuState.WasFrozen;
        }

        private void ActivateCustomMenuItem(CustomMenuItem item)
        {
            if (!item.Keybind.IsBound)
            {
                Monitor.Log(
                    $"Failed to activate '{item.Name}' because it has no configured key binding.",
                    LogLevel.Warn);
                return;
            }
            keybindActivator.Activate(item.Keybind);
        }

        private void UpdateMenuItems(MenuKind? menu)
        {
            activeMenuItems = menu switch
            {
                MenuKind.Inventory => Game1.player.Items
                    .Take(config.MaxInventoryItems)
                    .Select((item, index) =>
                        item != null ? menuItemBuilder.GameItem(item, index) : null)
                    .Where(i => i is not null)
                    .Cast<MenuItem>()
                    .ToList(),
                MenuKind.Custom => config.CustomMenuItems
                    .Select(item => menuItemBuilder.CustomItem(item))
                    .ToList(),
                _ => [],
            };
        }
    }
}
