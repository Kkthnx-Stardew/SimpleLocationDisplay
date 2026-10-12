# Simple Location Display

A small popup in the bottom-left corner tells you where you are whenever you enter a new area. It uses the same box as the game's "It's getting late" message and fades out by itself.

Download it from [CurseForge](https://www.curseforge.com/stardewvalley/mods/simplelocationdisplay) or [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/33749).

## What it shows

The popup uses the game's own names, so it follows your language setting with no extra files:

- Towns, shops, homes and other places with a name in the game data show that name.
- Farm buildings show the building's name, such as "Deluxe Coop".
- The Mines show "Mines" with "Level 12" on a second line. The Skull Cavern and the Volcano do the same, and the number matches the one the game draws in the top-left corner. Skull Cavern levels start again at 1 after level 120.
- A few places the game never names, such as the Night Market, the Desert Festival and some Ginger Island rooms, use text the game already translates.
- Locations added by other mods show the name the mod gave them. If a mod gave none, the popup tries the warp name from CJB Cheats Menu (when you have it), and after that a readable version of the internal name, for example `Custom_FrontierFarm_HiddenCave` becomes "Frontier Farm Hidden Cave".

A popup that would appear during a cutscene waits until the cutscene ends. The short-lived temporary locations the game uses for events, festivals and minigames never get one.

## Install

1. Install [SMAPI](https://smapi.io/) 4.2.1 or newer.
2. Unzip the download and put the `SimpleLocationDisplay` folder in `Stardew Valley/Mods`.
3. Optional: install Generic Mod Config Menu to change settings in game.

Needs Stardew Valley 1.6 or newer. It loaded without errors on Stardew Valley 1.6.15 with SMAPI 4.5.2.

## Settings

Open the config menu from the title screen or the game menu, or edit `config.json` in the mod folder.

| Setting | Default | What it does |
| --- | --- | --- |
| `EnableMod` | `true` | Turns the popups on or off. |
| `NotificationDuration` | `3500` | How long the popup stays, in milliseconds. Values are kept between 1000 and 10000. |
| `ShowOnSaveLoad` | `true` | Also shows the popup for the place you start in when you load a save. |
| `ShowZeroAsL0` | `false` | Shows level 0 of a dungeon as "L0" instead of leaving the number off. |
| `EnableDebugLogging` | `false` | Writes extra detail about each popup to the SMAPI log. |

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

The mod ships a Spanish file and a file for each of the game's other languages. All of them carry the Skull Cavern name as the game writes it in that language. Everything else comes from the game, or falls back to English from `i18n/default.json`.

To find places that fell back to a readable guess, play for a while and run `sld_dump_missing_translations` in the SMAPI console. It writes `i18n/missing.en.json` in the mod folder, with a key for each place. Copy the keys you want to name into `i18n/default.json` (or your language file) and send them to me.

## Console commands

- `debug_location` prints the current location's names, its building, and what the popup would say.
- `sld_dump_missing_translations` writes the file described above.

## Troubleshooting

If no popup appears, check that `EnableMod` is `true`, then look at the SMAPI console for errors. If a place shows a strange name, run `debug_location` while standing in it and include the line in your bug report, along with the SMAPI log from [smapi.io/log](https://smapi.io/log).

## Versioning

Versions follow [Semantic Versioning](https://semver.org/): `MAJOR.MINOR.PATCH`. A patch release fixes bugs, a minor release adds features without breaking anything, and a major release changes something existing configs or content packs rely on. A released version is never changed; fixes ship as a new number. The version lives in the `Version` property of `SimpleLocationDisplay.csproj`, and the build copies it into the manifest. Changes are listed in [CHANGELOG.md](CHANGELOG.md).

## Building

You need the .NET SDK, Stardew Valley and SMAPI. The mod build package finds the game folder on its own and copies the built mod into `Mods`.

```bash
dotnet build SimpleLocationDisplay.sln -c Release
dotnet test SimpleLocationDisplay.Tests
```

The release zip is written next to the built DLL in `SimpleLocationDisplay/bin/Release/net6.0`.

## License

MIT. See [LICENSE.txt](LICENSE.txt).
