# Star Control

(aka: "Better Gamepads")

![Full overlay screenshot](docs/images/screenshot-controller-overlay.png)

## Background

Star Control (**Star**dew **Control**lers) is a mod and framework for replacing Stardew Valley's dated Harvest Moon-style gamepad control scheme with the modal, remappable controls found in modern games, and making the majority of other Stardew mods convenient to interact with using a controller.

Main features include:
- [Pie menus](https://focustense.github.io/StardewControllers/controller-hud/#pie-menus): dynamic radial/wheel style menus for inventory and mod actions that can be navigated with the analog stick;
- [Quick Actions](https://focustense.github.io/StardewControllers/controller-hud/#quick-actions): reassignable one-button actions that are active while the pie menu overlay is open;
- [Instant Actions](https://focustense.github.io/StardewControllers/instant-actions/): reassignable "Zelda-style" one-button actions that be bound to tool swings, melee combat, bomb placement and other in-world actions;
- A complete in-game [configuration system](https://focustense.github.io/StardewControllers/configuration/) for managing all the menus, shortcuts and bindings;
- [Mod API](https://focustense.github.io/StardewControllers/api/) for other mods to register their own actions and custom pages.

## Documentation

User and integrator documentation, including all content on this page, is available at the [Star Control Docs](https://focustense.github.io/StardewControllers/).

## Requirements

Using Star Control requires:

- A valid Stardew Valley install with SMAPI and Stardew UI (see [setup](#setup) below);
- Stardew-compatible gamepad controller, such as any Xbox controller;
- Screen resolution or window size of at least 1080p (1920x1080).

## Setup

To get started:

1. Install [SMAPI](https://smapi.io) and set up your game for mods, per the [Modding: Player's Guide](https://stardewvalleywiki.com/Modding:Player_Guide/Getting_Started) instructions.
2. Download and install [Stardew UI](https://github.com/focustense/StardewUI/releases).
3. Download the latest release from [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/25257) or [GitHub](https://github.com/focustense/StardewControllers/releases).
4. Open the .zip file and extract the `StarControl` folder into your `Stardew Valley\Mods` folder, or use a mod manager such as Stardrop.
5. Launch the game and load a save.

Refer to the [setup docs](https://focustense.github.io/StardewControllers/#setup) for more information on default controls and the [configuration docs](https://focustense.github.io/StardewControllers/configuration/) for customization.

## Contact

To report an issue or contact the author:

* Create a [GitHub issue](https://github.com/focustense/StardewControllers/issues); for bug reports, be sure to [enable all logging](https://focustense.github.io/StardewControllers/configuration/#debug) and include clear repro steps along with your [SMAPI log](https://smapi.io/log).
* Ping `@focustense` on the [SV Discord](https://discord.com/invite/stardewvalley) or start a thread in `#modded-tech-support`.

## See Also

* [Changelog](CHANGELOG.md)
