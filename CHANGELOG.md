# Changelog

Versions follow [Semantic Versioning](https://semver.org/). Newest first.

## 2.0.0-beta.1 (2026-10-12)

A preview of 2.0.0, so the release path and the new behavior get a real test before the final release. It has every change listed under 2.0.0 below. The main ones:

- Mines, Skull Cavern and Volcano popups show the level on a second line, in the game's language.
- A `HiddenLocations` setting, and a `Names` asset that content packs can edit.
- Popups wait for cutscenes to end and skip temporary and `WarpRoom` locations.
- Better names for modded locations: mod prefixes dropped, possessives, and CJB Cheats Menu warp names.

Read "Breaking changes" under 2.0.0 if you translate the mod or make a content pack for it. Please report problems with a SMAPI log.

## 2.0.0 (not released)

This is a major release because some things translators and content packs may rely on changed. Nothing in `config.json` needs editing.

### Breaking changes

- Mine, Skull Cavern and Volcano popups read "Mines" with "Level 12" on a second line. Before, they read "Underground Mine L12", and the Skull Cavern showed an internal number.
- These i18n keys are gone: `location.Temp`, `location.BathHouse_Entry` and `location.UndergroundMine`. The game's own names or the new level handling always win over them, so a translation of those keys no longer shows.
- No popup appears in temporary event, festival and minigame locations, or in rooms with `WarpRoom` in the name. During a cutscene the popup waits until the cutscene ends.
- The new `HiddenLocations` setting defaults to an empty list. You can add it to `config.json` by hand, or it is written when you save from the config menu.

### Added

- A `HiddenLocations` setting, also in the config menu, for places that should never get a popup. Entries match the internal name or the text the popup shows.
- An asset, `Mods/Kkthnx.SimpleLocationDisplay/Names`, that content packs can edit to name a location or hide its popup.
- Mines, Skull Cavern and Volcano levels show the level number the game draws on screen, in the game's own language, with the level on a second line. The Quarry Mine and numbered cellars get proper names.
- Vanilla places with no display name use text the game already translates: the Night Market, the Desert Festival, the Island farmhouse and farm cave, the Qi walnut room, the Field Office and the Pirate Cove. The Casino and the Skull Cavern entrance are named too.
- When CJB Cheats Menu is installed, its warp names are used for modded locations that have no display name.
- A guessed name gets a possessive when a character's name comes before a place word: "Marnie's Shed", "Lewis' Basement". The game's character list is used, so mod characters count.
- Farm building interiors take the building's name from the game, so modded buildings and every language work without extra files.
- The Skull Cavern name in the game's other ten languages, copied from the game's own text.
- Unit tests for the name logic.

### Changed

- Popups no longer appear on top of cutscenes, which the game draws HUD messages over. A popup that would have shown during a cutscene shows when it ends. The temporary locations the game uses for events, festivals and minigames are skipped.
- Rooms with `WarpRoom` in the name get no popup unless a mod or an override names them. Location mods use them to park characters, and a popup like "Alesia Warp Room" only adds noise.
- A location with broken data from another mod logs one warning and still gets a popup, instead of an error on every visit.
- Numbers in raw names are spaced out ("Bert House2nd Floor" becomes "Bert House 2nd Floor"), and a repeated word is dropped ("Ridgeside Ridgeside Village").
- Split-screen players each keep their own popup state.
- The readable fallback drops a leading mod unique ID or `Custom_`, splits acronyms ("ARVHouse" becomes "ARV House") and uses invariant casing, which fixes a dotted capital I on Turkish systems.
- The version is set once in `SimpleLocationDisplay.csproj` and copied into the manifest at build time.
- The config menu text comes from a generated `I18n` class, so a misspelled key fails the build. The duration shows in seconds.
- `sld_dump_missing_translations` writes the file with SMAPI's JSON writer. `debug_location` also prints the building and whether the location is temporary.
- The "Generic Mod Config Menu not found" message is a trace line, since the dependency is optional.
- README rewritten to match the current behavior.

### Removed

- i18n keys that can no longer be reached: `location.Temp` (those locations are skipped now), `location.BathHouse_Entry` (the game has a display name for it) and `location.UndergroundMine` (it never matched a leveled name).

### Fixed

- Skull Cavern levels showed the internal number (130 for the game's level 10).
- Generated mine levels with a layout suffix, such as `UndergroundMine12:3`, are named correctly.
- `LICENSE.txt` had the template placeholders for the year and name.

## 1.1.0 (2026-07-16)

- Added the `ShowOnSaveLoad` option, which shows the popup for your starting location when a save loads.
- Added a Spanish translation file.
- Repeated popups are suppressed by the raw location name instead of the displayed text.
- Names are looked up again after a language change.
- Config values are clamped to the range the menu allows.
- GUID suffixes are stripped more reliably, and the plain location name is tried as a second translation key.
- Declared Generic Mod Config Menu as an optional dependency and set a minimum game version of 1.6.0.

## 1.0.7 (2026-01-02)

- Updated location names and added translations.

## 1.0.6 (2025-12-18)

- Moved the name logic into its own service.

## 1.0.0 to 1.0.5

- See the git history.
