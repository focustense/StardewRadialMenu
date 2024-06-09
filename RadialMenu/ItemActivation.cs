using StardewValley;
using StardewValley.Locations;

namespace RadialMenu
{
    internal static class FuzzyActivation
    {
        public static void ConsumeOrSelect(int itemIndex)
        {
            var item = itemIndex >= 0 && itemIndex < Game1.player.Items.Count
                ? Game1.player.Items[itemIndex]
                : null;
            if (item is null || TryConsume(item))
            {
                return;
            }
            Game1.player.CurrentToolIndex = itemIndex;
            if (Game1.player.CurrentTool is not null)
            {
                Game1.playSound("toolSwap");
            }
        }

        private static bool TryConsume(Item item)
        {
            if (item.Name == "Staircase" && Game1.currentLocation is MineShaft mineShaft)
            {
                ReduceStack(item);
                Game1.enterMine(mineShaft.mineLevel + 1);
                Game1.playSound("stairsdown");
                return true;
            }
            if (item is not StardewValley.Object obj)
            {
                return false;
            }
            if (obj.Edibility > 0)
            {
                ReduceStack(obj);
                Game1.player.eatObject(obj);
                return true;
            }
            return obj.performUseAction(Game1.currentLocation);
        }

        private static void ReduceStack(Item item)
        {
            if (item.Stack > 1)
            {
                item.Stack--;
            }
            else
            {
                Game1.player.Items.RemoveButKeepEmptySlot(item);
            }
        }
    }
}
