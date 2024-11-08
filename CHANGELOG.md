# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.2] - 2024-11-06

### Fixed

- No code changes, but rebuilt binary with Stardew Valley 1.6.10 DLL to eliminate `InvalidProgramException` after update.

## [0.2.1] - 2024-08-28

### Fixed

- Inventory radial menu now stays in sync with actual inventory when an item from the second (or later) row is moved up to the first row and becomes the selected tool.
- Custom menu no longer fails to render when focused on an item without a valid texture (i.e. provided by an API user).

## [0.2.0] - 2024-08-15

### Added

- Public API for use by other mods, with demo project; see [documentation in the readme](README.md#api).
- Option to remember the last-opened menu page.
- Per-item setting to force activation delay on custom menu items.

### Changed

- Menu pages are persistent and synced on specific triggers, such as `InventoryChanged`, instead of being recreated every time the menu is opened; this change supports the new API.
- Switch from `TranslationHelper` to `ModTranslationClassBuilder`; should have no user-facing effects.

### Fixed

- Items that are picked up while a menu is already open (i.e. pulled in by "magnetism") will show up in the inventory menu immediately, without having to close/reopen the menu.
- Minor fixes to error logging.

## [0.1.6] - 2024-06-23

### Added

- Switch pages (using L/R shoulder buttons) while menu is open.
  - By design, this works differently from vanilla shifting during gameplay; the switch only affects the menu itself, and only persists while the menu is open, so cancelling out of the menu or auto-using an item on a different page does not affect current tool selection.
  - In simple terms, you can be busy chopping/mining, eat a food item on backpack page 2 or 3 using the menu, and still have the axe/pickaxe selected afterward.
- Highlight current tool selection in the menu, using a different background color for that wedge.
  - Color is customizable in GMCM; default is a darker shade of the normal menu background. Players who find this distracting can set it to the menu background color.
  - This was added because the radial menu freezes gameplay controls which, as a side effects, also hides the vanilla toolbar, so previously the only way to remind oneself of which item was already selected was to dismiss the radial menu in order to see the toolbar again.
- This changelog. Covers all past releases.

## [0.1.5] - 2024-06-21

### Fixed

- Menu now works in local co-op without freezing, thrashing or locking out controls.

## [0.1.4] - 2024-06-20

### Fixed

- Item titles are now correct for flavored items, e.g. "Starfruit Wine" instead of just "Wine".
- Focused item descriptions will no longer include format args like `{0}` as literal text; they will display the item's full and correctly-formatted text. As a side effect, this also includes occasional suffix text like weapon stats.
- Menu/focused items will have the correct color tint.
- Smoked Fish will show the correct fish (previously it was always a carp).

## [0.1.3-alpha] - 2024-06-18

### Changed

- Primary and secondary actions are both configurable, meaning the "default" (now primary) action for a consumable item can instead be to select it, and the secondary action to auto-use.
- Out-of-the-box control scheme remains the scheme (A to auto-use, X to force-select) but players can now edit these to form one of the more commonly-requested alternate schemes, using trigger release for select and A or X button for auto-use.
- Changes names of the configuration settings related to the above features, so those settings will be reset on first-time launch and may have to be updated again for players not using the defaults.

## [0.1.2-alpha] - 2024-06-17

### Added

- Forced tool selection (skip quick actions like eat, warp) using a secondary button, default "X". This restores the ability to hold a consumable item for putting into machines, gifting, etc.
- Secondary button configuration in GMCM settings.

## [0.1.1-alpha] - 2024-06-17

### Changed

- Only play "select" sound when activation is delayed. Many items have their own activation sounds and/or animations, so having the menu also play a sound could lead to confusing and annoying feedback.

## [0.1.0-alpha] - 2024-06-16

### Added

- Initial release.
- Inventory menu via left trigger (default).
- Custom shortcuts menu via right trigger (default).
- [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) pages for control scheme, appearance and custom shortcuts.

[Unreleased]: https://github.com/focustense/StardewRadialMenu/compare/v0.2.2...HEAD
[0.2.2]: https://github.com/focustense/StardewRadialMenu/compare/v0.2.1...v0.2.2
[0.2.1]: https://github.com/focustense/StardewRadialMenu/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/focustense/StardewRadialMenu/compare/v0.1.6...v0.2.0
[0.1.6]: https://github.com/focustense/StardewRadialMenu/compare/v0.1.5...v0.1.6
[0.1.5]: https://github.com/focustense/StardewRadialMenu/compare/v0.1.4...v0.1.5
[0.1.4]: https://github.com/focustense/StardewRadialMenu/compare/v0.1.3-alpha...v0.1.4
[0.1.3-alpha]: https://github.com/focustense/StardewRadialMenu/compare/v0.1.2-alpha...v0.1.3-alpha
[0.1.2-alpha]: https://github.com/focustense/StardewRadialMenu/compare/v0.1.1-alpha...v0.1.2-alpha
[0.1.1-alpha]: https://github.com/focustense/StardewRadialMenu/compare/v0.1.0-alpha...v0.1.1-alpha
[0.1.0-alpha]: https://github.com/focustense/StardewRadialMenu/tree/v0.1.0-alpha