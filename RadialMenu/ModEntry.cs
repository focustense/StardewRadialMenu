using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RadialMenu.Config;
using RadialMenu.Gmcm;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Reflection;

namespace RadialMenu;

record PreMenuState(bool WasFrozen);

public class ModEntry : Mod
{
    private const string GMCM_MOD_ID = "spacechase0.GenericModConfigMenu";
    private const string GMCM_OPTIONS_MOD_ID = "jltaylor-us.GMCMOptions";

    private Configuration config = null!;
    private ConfigMenu? configMenu;
    private IGenericModMenuConfigApi? configMenuApi;
    private IGMCMOptionsAPI? gmcmOptionsApi;
    private GenericModConfigKeybindings? gmcmKeybindings;
    private GenericModConfigSync? gmcmSync;
    private MenuItemBuilder menuItemBuilder = null!;
    private IReadOnlyList<MenuItem> activeMenuItems = [];
    private Cursor cursor = null!;
    private Painter painter = null!;
    private TextureHelper textureHelper = null!;
    private KeybindActivator keybindActivator = null!;
    private PreMenuState preMenuState = null!;
    private Func<DelayedActions?, ItemActivationResult>? pendingActivation;
    private double remainingActivationDelayMs;

    public override void Entry(IModHelper helper)
    {
        config = Helper.ReadConfig<Configuration>();
        textureHelper = new(Helper.GameContent, Monitor);
        menuItemBuilder = new(textureHelper, ActivateCustomMenuItem);
        cursor = new(() => config);
        painter = new(Game1.graphics.GraphicsDevice, () => config.Styles);
        keybindActivator = new(helper.Input);
        preMenuState = new(Game1.freezeControls);

        helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
        // For optimal latency: handle input before the Update loop, perform actions/rendering after.
        helper.Events.GameLoop.UpdateTicking += GameLoop_UpdateTicking;
        helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
        helper.Events.Input.ButtonsChanged += Input_ButtonsChanged;
        helper.Events.Display.RenderedHud += Display_RenderedHud;
    }

    private float GetSelectionBlend()
    {
        if (pendingActivation is null)
        {
            return 1.0f;
        }
        var elapsed = (float)(config.ActivationDelayMs - remainingActivationDelayMs);
        return MathF.Abs(((elapsed / 80) % 2) - 1);
    }

    [EventPriority(EventPriority.Low)]
    private void Display_RenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (cursor.ActiveMenu is null)
        {
            return;
        }
        var selectionBlend = GetSelectionBlend();
        painter.Paint(
            e.SpriteBatch,
            Game1.uiViewport,
            cursor.CurrentTarget?.SelectedIndex ?? -1,
            cursor.CurrentTarget?.Angle,
            selectionBlend);
    }

    [EventPriority(EventPriority.Low - 10)]
    private void GameLoop_GameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        configMenuApi = Helper.ModRegistry.GetApi<IGenericModMenuConfigApi>(GMCM_MOD_ID);
        gmcmOptionsApi = Helper.ModRegistry.GetApi<IGMCMOptionsAPI>(GMCM_OPTIONS_MOD_ID);
        LoadGmcmKeybindings();
        RegisterConfigMenu();
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
            cursor.CheckSuppressionState(out _);
            // Delay filtering is slippery, because sometimes in order to know whether an item
            // _will_ be consumed (vs. switched to), we just have to attempt it, i.e. using the
            // performUseAction method.
            //
            // So what we can do is pass the delay parameter into the action itself, removing it
            // once the delay is up; the action will abort if it matches the delay setting but
            // proceed otherwise.
            if (remainingActivationDelayMs > 0)
            {
                remainingActivationDelayMs -= Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
            }
            var activationResult = MaybeInvokePendingActivation();
            if (activationResult != ItemActivationResult.Ignored && activationResult != ItemActivationResult.Delayed)
            {
                pendingActivation = null;
                remainingActivationDelayMs = 0;
                cursor.Reset();
                RestorePreMenuState();
            }
        }
    }

    private void GameLoop_UpdateTicking(object? sender, UpdateTickingEventArgs e)
    {
        if (!Context.IsWorldReady)
        {
            return;
        }

        cursor.GamePadState = GetRawGamePadState();
        if (!Context.CanPlayerMove && cursor.ActiveMenu is null)
        {
            cursor.CheckSuppressionState(out _);
            return;
        }

        if (remainingActivationDelayMs <= 0)
        {
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
                    if (config.Activation == ItemActivationMethod.TriggerRelease
                        && cursor.CurrentTarget is not null)
                    {
                        cursor.RevertActiveMenu();
                        ScheduleActivation();
                    }
                    else
                    {
                        RestorePreMenuState();
                    }
                }
                UpdateMenuItems(cursor.ActiveMenu);
                painter.Items = activeMenuItems;
            }
            cursor.UpdateCurrentTarget(activeMenuItems.Count);
        }

        // Here be dragons: because the triggers are analog values and SMAPI uses a deadzone, it
        // will race with Stardew (and usually lose), with the practical symptom being that if we
        // try to do the suppression in the "normal" spot (e.g. on input events), Stardew still
        // receives not only the initial trigger press, but another spurious "press" when the
        // trigger is released.
        //
        // The "solution" - preemptively suppress all trigger presses whenever the player is being
        // controlled, and punch through SMAPI's controller abstraction when reading the trigger
        // values ourselves. This will almost certainly cause incompatibilities with any other mods
        // that want to leverage the trigger buttons outside of menus... but then again, the overall
        // functionality is inherently incompatible regardless of hackery.
        Helper.Input.Suppress(SButton.LeftTrigger);
        Helper.Input.Suppress(SButton.RightTrigger);
    }

    private void Input_ButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!Context.IsWorldReady
            || remainingActivationDelayMs > 0
            || cursor.ActiveMenu is null
            || cursor.CurrentTarget is null
            || config.Activation == ItemActivationMethod.TriggerRelease)
        {
            return;
        }
        foreach (var button in e.Pressed)
        {
            if (IsActivationButton(button))
            {
                ScheduleActivation();
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

    private Func<DelayedActions?, ItemActivationResult>? GetSelectedItemActivation()
    {
        var itemIndex = cursor.CurrentTarget?.SelectedIndex;
        return itemIndex < activeMenuItems.Count
            ? activeMenuItems[itemIndex.Value].Activate : null;
    }

    private bool IsActivationButton(SButton button)
    {
        return config.Activation switch
        {
            ItemActivationMethod.ActionButtonPress => button.IsActionButton(),
            ItemActivationMethod.ThumbStickPress => cursor.IsThumbStickForActiveMenu(button),
            _ => false,
        };
    }

    private ItemActivationResult MaybeInvokePendingActivation()
    {
        if (pendingActivation is null)
        {
            // We shouldn't actually hit this, since it's only called from conditional blocks that
            // have already confirmed there's a pending activation.
            // Nonetheless, for safety we assign a special result type to this, just in case the
            // assumption gets broken later.
            return ItemActivationResult.Ignored;
        }
        return remainingActivationDelayMs <= 0
            ? pendingActivation.Invoke(null)
            : pendingActivation.Invoke(config.DelayedActions);
    }

    private void LoadGmcmKeybindings()
    {
        if (configMenuApi is null)
        {
            Monitor.Log(
                $"Couldn't read global keybindings; mod {GMCM_MOD_ID} is not installed.",
                LogLevel.Warn);
            return;
        }
        Monitor.Log("Generic Mod Config Menu is loaded; reading keybindings.", LogLevel.Info);
        try
        {
            gmcmKeybindings = GenericModConfigKeybindings.Load();
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
            gmcmSync = new(() => config, gmcmKeybindings, Monitor);
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

    private void RegisterConfigMenu()
    {
        if (configMenuApi is null)
        {
            return;
        }
        configMenuApi.Register(
            mod: ModManifest,
            reset: ResetConfiguration,
            save: () => Helper.WriteConfig(config));
        configMenu = new(
            configMenuApi,
            gmcmOptionsApi,
            gmcmKeybindings,
            gmcmSync,
            ModManifest,
            Helper.Translation,
            Helper.ModContent,
            textureHelper,
            Helper.Events.GameLoop,
            () => config);
        configMenu.Setup();
    }

    private void ResetConfiguration()
    {
        config = new();
    }

    private void RestorePreMenuState()
    {
        Game1.freezeControls = preMenuState.WasFrozen;
    }

    private void ScheduleActivation()
    {
        pendingActivation = GetSelectedItemActivation();
        if (pendingActivation is null)
        {
            return;
        }
        Game1.playSound("select");
        remainingActivationDelayMs = config.ActivationDelayMs;
        cursor.SuppressUntilTriggerRelease();
    }

    private void ActivateCustomMenuItem(CustomMenuItemConfiguration item)
    {
        if (!item.Keybind.IsBound)
        {
            Game1.showRedMessage(Helper.Translation.Get("error.missingbinding"));
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
