# Simple Location Display

[![Latest release](https://img.shields.io/github/v/release/Kkthnx-Stardew/SimpleLocationDisplay?include_prereleases&sort=semver)](https://github.com/Kkthnx-Stardew/SimpleLocationDisplay/releases) [![Issues](https://img.shields.io/github/issues/Kkthnx-Stardew/SimpleLocationDisplay)](https://github.com/Kkthnx-Stardew/SimpleLocationDisplay/issues) [![License](https://img.shields.io/github/license/Kkthnx-Stardew/SimpleLocationDisplay)](LICENSE.txt) [![CurseForge](https://img.shields.io/badge/CurseForge-Download-orange)](https://www.curseforge.com/stardewvalley/mods/simplelocationdisplay) [![Nexus Mods](https://img.shields.io/badge/Nexus_Mods-Download-blue)](https://www.nexusmods.com/stardewvalley/mods/33749)

A small popup in the bottom-left corner names each new area you enter. It uses the same box as the game's "It's getting late" message and fades out by itself.

It takes its names from the game, so it follows your language setting, and it works with large location mods such as Stardew Valley Expanded, Ridgeside Village and East Scarp.

## Download

- [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/33749) has the stable releases.
- [CurseForge](https://www.curseforge.com/stardewvalley/mods/simplelocationdisplay) has stable releases and betas.
- [GitHub Releases](https://github.com/Kkthnx-Stardew/SimpleLocationDisplay/releases) has every release, betas included, with the changelog.

## What it shows

- Towns, shops, homes and other places with a name in the game data show that name.
- Farm buildings show the building's name, such as "Deluxe Coop".
- The Mines show "Mines" with "Level 12" on a second line. The Skull Cavern and the Volcano do the same, and the number matches the one the game draws in the top-left corner. Skull Cavern levels start again at 1 after level 120.
- A few places the game never names, such as the Night Market, the Desert Festival and some Ginger Island rooms, use text the game already translates.
- Locations added by other mods show the name the mod gave them. If a mod gave none, the popup tries the warp name from CJB Cheats Menu (when you have it), and after that a readable version of the internal name. `Custom_FrontierFarm_HiddenCave` becomes "Frontier Farm Hidden Cave", and a character's name before a place word gets a possessive, so `Custom_MarnieShed` reads "Marnie's Shed".

A popup that would appear during a cutscene waits until the cutscene ends. Temporary locations used for events, festivals and minigames never get one, and neither do the `WarpRoom` rooms that mods use to park characters between schedule stops.

## Install

1. Install [SMAPI](https://smapi.io/) 4.2.1 or newer.
2. Unzip the download and put the `SimpleLocationDisplay` folder in `Stardew Valley/Mods`.
3. Optional: install Generic Mod Config Menu to change settings in game.

Needs Stardew Valley 1.6.9 or newer.

### Updating from 1.x

Replace the old `SimpleLocationDisplay` folder with the new one. Your `config.json` carries over and needs no edits. Two things look different in 2.0: Mines, Skull Cavern and Volcano popups put the level on a second line, and three translation keys that could no longer be reached were removed (`location.Temp`, `location.BathHouse_Entry`, `location.UndergroundMine`). The full list is in the [changelog](CHANGELOG.md).

## Settings

Open the config menu from the title screen or the game menu, or edit `config.json` in the mod folder.

| Setting | Default | What it does |
| --- | --- | --- |
| `EnableMod` | `true` | Turns the popups on or off. |
| `NotificationDuration` | `3500` | How long the popup stays, in milliseconds. Values are kept between 1000 and 10000. |
| `ShowOnSaveLoad` | `true` | Also shows the popup for the place you start in when you load a save. |
| `ShowZeroAsL0` | `false` | Shows level 0 of a dungeon as "L0" instead of leaving the number off. |
| `HiddenLocations` | empty | Places that never get a popup. In `config.json` this is a list such as `["Farm", "Pelican Town"]`; in the config menu it is a comma-separated box. Each entry is an internal location name or the text the popup would show, and case doesn't matter. |
| `EnableDebugLogging` | `false` | Writes extra detail about each popup to the SMAPI log. |

## Works with other mods

I checked the popup in a save with Stardew Valley Expanded 1.15.11, Ridgeside Village 2.5.17 and East Scarp Remastered 3.0.9 loaded, on Stardew Valley 1.6.15 with SMAPI 4.5.2. The popup text was worked out for all 367 locations in that world with no errors. The three mods give most of their places a display name, which the popup uses, and cutscenes in them wait as described above.

Generic Mod Config Menu and CJB Cheats Menu are optional. The mod's page in Generic Mod Config Menu was opened and built in that same test. Split-screen players each keep their own popup, but I haven't run a live multiplayer or split-screen session.

## For mod authors

The best fix is to set `DisplayName` on your location in `Data/Locations`. The game uses it everywhere, and this mod picks it up with no extra work. Building interiors use the `Name` from `Data/Buildings` in the same way.

If you can't change the location, or you want a different popup text, edit the asset `Mods/Kkthnx.SimpleLocationDisplay/Names` with Content Patcher. The key is the location name. The value is plain text or a tokenizable string. A blank value hides the popup for that location.

```json
{
    "Format": "2.0.0",
    "Changes": [
        {
            "Action": "EditData",
            "Target": "Mods/Kkthnx.SimpleLocationDisplay/Names",
            "Entries": {
                "{{ModId}}_TreeHouse": "{{i18n: location.treehouse}}",
                "{{ModId}}_WarpRoom": ""
            }
        }
    ]
}
```

Entries in this asset win over every other source, including the game's own names, so players can use it to rename places too.

If your mod adds warps to CJB Cheats Menu with a translated `DisplayName`, the popup uses that name for any location that has a single warp and no `DisplayName` of its own.

When the popup has to fall back to a readable version of a raw name, it drops a leading mod unique ID or `Custom_`, since Content Patcher expects location names to start with one of them.

## Translations

The mod ships a Spanish file and a file for each of the game's other languages. All of them carry the names of the Skull Cavern, the Cellar and the Quarry Mine, taken from the game's own text in that language. I checked the popup in all twelve languages in game. The Casino has no text in the game to take a word from, so it stays English until someone translates it. Everything else comes from the game, or falls back to English from `i18n/default.json`.

To find places that fell back to a readable guess, play for a while and run `sld_dump_missing_translations` in the SMAPI console. It writes `i18n/missing.en.json` in the mod folder, with a key for each place. Copy the keys you want to name into `i18n/default.json` (or your language file) and open a pull request or an issue with them.

## Console commands

- `debug_location` prints the current location's names, its building, and what the popup would say.
- `sld_dump_missing_translations` writes the file described above.

## Troubleshooting

If no popup appears, check that `EnableMod` is `true` and that the place isn't in `HiddenLocations`, then look at the SMAPI console for errors.

To report a problem, [open an issue](https://github.com/Kkthnx-Stardew/SimpleLocationDisplay/issues/new/choose). Include a SMAPI log from [smapi.io/log](https://smapi.io/log) and, if a place shows a strange name, the line `debug_location` prints while you stand in it.

## Versioning

Versions follow [Semantic Versioning](https://semver.org/): `MAJOR.MINOR.PATCH`. A patch release fixes bugs, a minor release adds features without breaking anything, and a major release changes something existing configs, translations or content packs rely on. Pre-releases are labeled `-alpha.N`, `-beta.N` or `-rc.N`, in that order. A released version is never changed; fixes ship as a new number. Changes are listed in [CHANGELOG.md](CHANGELOG.md).

## Contributing

Bug reports, translations and pull requests are welcome. [CONTRIBUTING.md](CONTRIBUTING.md) covers building, testing and how releases are made.

## License

MIT. See [LICENSE.txt](LICENSE.txt).
