using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Numerics;
using System.Reflection;

namespace RadialMenu
{
    enum MenuKind
    {
        Inventory,
        Custom
    }

    record PreMenuState(bool WasFrozen);

    record MenuCursorState(float Angle, int SelectedIndex);

    public class ModEntry : Mod
    {
        private MenuKind? activeMenu;
        private IReadOnlyList<MenuItem> activeMenuItems = [];
        private Painter menuPainter = null!;
        private PreMenuState preMenuState = null!;
        private MenuCursorState? menuCursorState;

        public override void Entry(IModHelper helper)
        {
            menuPainter = new(Game1.graphics.GraphicsDevice);
            preMenuState = new(Game1.freezeControls);

            // For optimal latency: handle input before the Update loop, perform actions/rendering after.
            helper.Events.GameLoop.UpdateTicking += GameLoop_UpdateTicking;
            helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
            helper.Events.Display.RenderedHud += Display_RenderedHud;
        }

        private void Display_RenderedHud(object? sender, RenderedHudEventArgs e)
        {
            if (activeMenu is null)
            {
                return;
            }
            menuPainter.Paint(
                e.SpriteBatch,
                Game1.uiViewport,
                menuCursorState?.SelectedIndex ?? -1,
                menuCursorState?.Angle);
        }

        private void GameLoop_UpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
            {
                return;
            }
        }

        private void GameLoop_UpdateTicking(object? sender, UpdateTickingEventArgs e)
        {
            if (!Context.IsPlayerFree && activeMenu == null)
            {
                return;
            }
            if (MaybeSetActiveMenu())
            {
                Monitor.Log($"Set active menu to {activeMenu}", LogLevel.Info);
                if (activeMenu != null)
                {
                    preMenuState = new(Game1.freezeControls);
                    Game1.player.completelyStopAnimatingOrDoingAction();
                    Game1.freezeControls = true;
                }
                else
                {
                    Game1.freezeControls = preMenuState.WasFrozen;
                }
                UpdateMenuItems();
            }
            menuCursorState = ComputeMenuCursorState();
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

        private MenuKind? GetNextActiveMenu()
        {
            var rawGamePadState = GetRawGamePadState();
            if (rawGamePadState.Triggers.Left > 0.2f)
            {
                return MenuKind.Inventory;
            }
            else if (rawGamePadState.Triggers.Right > 0.2f)
            {
                return MenuKind.Custom;
            }
            else
            {
                return null;
            }
        }

        private bool MaybeSetActiveMenu()
        {
            var previousActiveMenu = activeMenu;
            var nextActiveMenu = GetNextActiveMenu();
            // Fighting between menus would be distracted; instead do first-come, first-serve.
            // Whichever menu became active first, stays active until dismissed.
            if (activeMenu != null && nextActiveMenu != null)
            {
                return false;
            }
            activeMenu = nextActiveMenu;
            return activeMenu != previousActiveMenu;
        }

        private GamePadState GetRawGamePadState()
        {
            return Game1.playerOneIndex >= PlayerIndex.One
                ? GamePad.GetState(Game1.playerOneIndex)
                : new GamePadState();
        }

        private MenuCursorState? ComputeMenuCursorState()
        {
            if (activeMenu is null)
            {
                return null;
            }
            var thumbStickAngle = GetThumbStickAngle(GetRawGamePadState());
            if (!thumbStickAngle.HasValue)
            {
                return null;
            }
            var itemCount = menuPainter.Items.Count;
            if (itemCount == 0)
            {
                return new(thumbStickAngle.Value, -1);
            }
            var maxAngle = MathF.PI * 2;
            var itemAngle = maxAngle / itemCount;
            var selectedIndex = (int)MathF.Round(thumbStickAngle.Value / itemAngle) % itemCount;
            return new(thumbStickAngle.Value, selectedIndex);
        }

        private float? GetThumbStickAngle(GamePadState state)
        {
            // TODO: Choose thumbstick based on active menu + config
            var thumbStick = state.ThumbSticks.Right;
            var maxAngle = MathF.PI * 2;
            float? angle = thumbStick.Length() > 0.2f /* deadzone */
                ? MathF.Atan2(thumbStick.X, thumbStick.Y)
                : null;
            return (angle + maxAngle) % maxAngle;
        }

        private void UpdateMenuItems()
        {
            menuPainter.Items = activeMenu switch
            {
                MenuKind.Inventory => Game1.player.Items
                    .Take(12) // Same number of items displayed in the toolbar
                    .Select(ConvertMenuItem)
                    .Where(i => i is not null)
                    .Cast<MenuItem>()
                    .ToList(),
                _ => [],
            };
        }

        private MenuItem? ConvertMenuItem(Item? item)
        {
            if (item == null)
            {
                return null;
            }
            var data = ItemRegistry.GetData(item.QualifiedItemId);
            var texture = data.GetTexture();
            var sourceRect = data.GetSourceRect();
            return new(data.DisplayName, data.Description, texture, sourceRect);
        }
    }
}
