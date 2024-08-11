using Microsoft.Xna.Framework.Graphics;
using RadialMenu;
using StardewModdingAPI.Events;

namespace RadialMenuApiTestMod;

internal sealed class ModEntry : Mod
{
    // Initialized in Entry
    private ModConfig config = null!;
    private Texture2D charactersTexture = null!;
    private Texture2D monstersTexture = null!;

    // Initialized in GameLaunched
    private IRadialMenuApi radialMenu = null!;

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        config = helper.ReadConfig<ModConfig>();
        charactersTexture = helper.ModContent.Load<Texture2D>("assets/characters.png");
        monstersTexture = helper.ModContent.Load<Texture2D>("assets/monsters.png");

        helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
    }

    private void GameLoop_GameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        radialMenu = Helper.ModRegistry.GetApi<IRadialMenuApi>("focustense.RadialMenu")!;
        if (radialMenu is null)
        {
            Monitor.Log("Unable to load Radial Menu API; mod functions disabled.", LogLevel.Error);
            return;
        }
        radialMenu.RegisterCustomMenuPage(
            ModManifest,
            "characters",
            new MenuPageFactory(() => new CharacterPage(charactersTexture)));
        radialMenu.RegisterCustomMenuPage(
            ModManifest,
            "monsters",
            new MenuPageFactory(() => new MonsterPage(monstersTexture, Monitor)));
    }

    class MenuPageFactory(Func<IRadialMenuPage> selector) : IRadialMenuPageFactory
    {
        public IRadialMenuPage CreatePage(Farmer _who)
        {
            return selector();
        }
    }
}