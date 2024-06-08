using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;

namespace RadialMenu
{
    internal record MenuCursorTarget(float Angle, int SelectedIndex);

    internal enum MenuKind { Inventory, Custom };

    internal class Cursor
    {
        private const float MAX_ANGLE = MathF.PI * 2;

        public float ThumbStickDeadZone { get; init; } = 0.2f;
        public float TriggerDeadZone { get; init; } = 0.2f;
        public GamePadState GamePadState { get; set; }
        public ThumbStickPreference ThumbStickPreference { get; set; }
        public MenuKind? ActiveMenu { get; private set; }
        public MenuCursorTarget? CurrentTarget { get; private set; }
        public bool WasMenuChanged { get; private set; }
        public bool WasTargetChanged { get; private set; }

        private MenuKind? suppressedMenu;

        public void UpdateActiveMenu()
        {
            WasMenuChanged = false;
            var nextActiveMenu = GetNextActiveMenu();
            if (suppressedMenu.HasValue)
            {
                if (nextActiveMenu == suppressedMenu.Value)
                {
                    return;
                }
                suppressedMenu = null;
            }
            // Fighting between menus would be distracting; instead do first-come, first-serve.
            // Whichever menu became active first, stays active until dismissed.
            if (ActiveMenu != null && nextActiveMenu != null)
            {
                return;
            }
            var previousActiveMenu = ActiveMenu;
            ActiveMenu = nextActiveMenu;
            WasMenuChanged = ActiveMenu != previousActiveMenu;
        }

        public void UpdateCurrentTarget(int itemCount)
        {
            var previousTarget = CurrentTarget;
            CurrentTarget = ComputeCurrentTarget(itemCount);
            WasTargetChanged = CurrentTarget?.SelectedIndex != previousTarget?.SelectedIndex;
        }

        public bool IsThumbStickForActiveMenu(SButton button)
        {
            return button == ThumbStickPreference switch
            {
                ThumbStickPreference.AlwaysLeft => SButton.LeftStick,
                ThumbStickPreference.AlwaysRight => SButton.RightStick,
                ThumbStickPreference.SameAsTrigger =>
                    ActiveMenu == MenuKind.Custom ? SButton.RightStick : SButton.LeftStick,
                _ => throw new NotImplementedException(),
            };
        }

        public void SuppressUntilTriggerRelease()
        {
            suppressedMenu = ActiveMenu;
            ActiveMenu = null;
            CurrentTarget = null;
        }

        private MenuKind? GetNextActiveMenu()
        {
            if (GamePadState.Triggers.Left > TriggerDeadZone)
            {
                return MenuKind.Inventory;
            }
            else if (GamePadState.Triggers.Right > TriggerDeadZone)
            {
                return MenuKind.Custom;
            }
            else
            {
                return null;
            }
        }

        private MenuCursorTarget? ComputeCurrentTarget(int itemCount)
        {
            if (ActiveMenu == null)
            {
                return null;
            }
            var thumbStickAngle = GetCurrentThumbStickAngle();
            if (!thumbStickAngle.HasValue)
            {
                return null;
            }
            if (itemCount == 0)
            {
                return new(thumbStickAngle.Value, -1);
            }
            var maxAngle = MathF.PI * 2;
            var itemAngle = maxAngle / itemCount;
            var selectedIndex = (int)MathF.Round(thumbStickAngle.Value / itemAngle) % itemCount;
            return new(thumbStickAngle.Value, selectedIndex);
        }

        private float? GetCurrentThumbStickAngle()
        {
            var position = GetCurrentThumbStickPosition(GamePadState.ThumbSticks);
            float? angle = position.Length() > ThumbStickDeadZone
                ? MathF.Atan2(position.X, position.Y)
                : null;
            return (angle + MAX_ANGLE) % MAX_ANGLE;
        }

        private Vector2 GetCurrentThumbStickPosition(GamePadThumbSticks sticks)
        {
            return ThumbStickPreference switch
            {
                ThumbStickPreference.AlwaysLeft => sticks.Left,
                ThumbStickPreference.AlwaysRight => sticks.Right,
                ThumbStickPreference.SameAsTrigger =>
                    ActiveMenu == MenuKind.Custom ? sticks.Right : sticks.Left,
                _ => throw new NotImplementedException(),
            };
        }
    }
}
