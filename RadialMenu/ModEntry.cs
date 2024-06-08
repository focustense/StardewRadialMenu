using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace RadialMenu
{
    record PreMenuState(bool WasFrozen);

    public class ModEntry : Mod
    {
        private IReadOnlyList<MenuItem> activeMenuItems = [];
        private Cursor cursor = null!;
        private Painter painter = null!;
        private PreMenuState preMenuState = null!;

        public override void Entry(IModHelper helper)
        {
            cursor = new Cursor();
            painter = new(Game1.graphics.GraphicsDevice);
            preMenuState = new(Game1.freezeControls);

            // For optimal latency: handle input before the Update loop, perform actions/rendering after.
            helper.Events.GameLoop.UpdateTicking += GameLoop_UpdateTicking;
            helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
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

        private void GameLoop_UpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
            {
                return;
            }
        }

        private void GameLoop_UpdateTicking(object? sender, UpdateTickingEventArgs e)
        {
            if (!Context.IsPlayerFree && cursor.ActiveMenu == null)
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
                    Game1.freezeControls = preMenuState.WasFrozen;
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

        private static GamePadState GetRawGamePadState()
        {
            return Game1.playerOneIndex >= PlayerIndex.One
                ? GamePad.GetState(Game1.playerOneIndex)
                : new GamePadState();
        }

        private void UpdateMenuItems(MenuKind? menu)
        {
            activeMenuItems = menu switch
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
