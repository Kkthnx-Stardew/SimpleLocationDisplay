# Simple Location Display
Know where you are with elegant HUD popups!

## Overview
Simple Location Display enhances your Stardew Valley experience by showing the current location name (e.g., "Test Farm", "Pelican Town") as a fading HUD popup in the bottom-left corner, styled like the game's "It's getting late" notification. With translation support, customizable settings via GenericModConfigMenu, and reliable performance across single-player and co-op saves, this mod keeps you oriented in your adventures!

## Features
- **Location Popups**: Shows each location's name (e.g., farm, town, modded areas) with a smooth fade-out effect.
- **Translation Support**: Converts raw names (e.g., "FarmHouse") to friendly names (e.g., "Farm House" in English, "Casa de la Granja" in Spanish).
- **Configurable Settings**: Adjust popup duration and enable/disable via GenericModConfigMenu (optional).
- **Save Persistence**: Works seamlessly when switching saves, single-player or co-op.
- **Lightweight**: Minimal performance impact with efficient event handling and translation caching.

## Installation
1. **Install SMAPI**: Download SMAPI 4.0.0 or later (required).
2. **Install the Mod**: Download Simple Location Display from Nexus Mods, unzip, and place the `SimpleLocationDisplay` folder in `Stardew Valley/Mods/`.
3. **Optional - GenericModConfigMenu**: Install GenericModConfigMenu for in-game configuration.
4. **Run the Game**: Launch Stardew Valley via SMAPI to activate the mod.

## Configuration
Configure using GenericModConfigMenu or by editing `config.json` in the mod’s folder. Options:
- **EnableMod**: Enable/disable popups (default: true).
- **NotificationDuration**: Set popup duration in milliseconds (1000–10000, default: 3000).

**With GMCM**:
1. Open the game menu (Esc) and click the GMCM icon (gear).
2. Select "Simple Location Display" and adjust settings.
3. Save changes.

A `config.json` file appears in the mod folder after running the game. Edit manually without GMCM.

## Translations
Supports English (`default.json`) and Spanish (`es.json`) translations, with more languages planned. Uses game localization (e.g., `Strings\Locations`) and custom `i18n/` files for modded locations.

### Requesting Translations
If a location shows a raw ID (e.g., `Custom_FrontierFarm_HiddenCave`) or you want a new language:
- Post on the Nexus Mods Bugs or Posts tab.
- Include the raw location name (from game or logs) and suggested translation.
- Or message Kkthnx on Stardew Valley Modding Discord.

**Example**: Suggest "Cueva Oculta de la Granja Fronteriza" for `Custom_FrontierFarm_HiddenCave` in Spanish.

## Compatibility
- **Game**: Stardew Valley 1.6.14 or later.
- **SMAPI**: Requires SMAPI 4.0.0 or later.
- **Mods**: Works with most mods, including custom locations (e.g., Ridgeside Village). Tested with UI Info Suite 2 and Custom Companions.
- **Multiplayer**: Supports single-player and co-op.

## Known Issues
- "Unknown Location" may appear for some locations. Report for translation support.
- Rare multiplayer state issues (e.g., `IsServer: False` in single-player) don’t affect functionality.

## Troubleshooting
If popups don’t show:
- Check `EnableMod` is `true` in `config.json` or GMCM.
- Ensure SMAPI is running (view console for errors).
- Share `%appdata%\StardewValley\ErrorLogs\smapi-log.txt` via SMAPI Log Parser on Nexus Mods.
- Test with only SMAPI, Simple Location Display, and GMCM to isolate conflicts.

## Reporting Bugs
Post on the Nexus Mods Bugs tab with:
- Issue description (e.g., "Popups stop after switching saves").
- `smapi-log.txt` (via SMAPI Log Parser).
- Steps to reproduce.

Or contact Kkthnx on Discord.

## Credits
- **SMAPI**: Pathoschild
- **Community**: Stardew Valley modding community

## Support
- Endorse on Nexus Mods.
- Share feedback to improve the mod!

Happy farming, and enjoy always knowing your location!
