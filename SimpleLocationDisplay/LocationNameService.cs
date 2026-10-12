using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Characters;
using StardewValley.Locations;
using StardewValley.TokenizableStrings;
using System.Collections.Generic;
using System.Globalization;

namespace SimpleLocationDisplay
{
    /// <summary>Works out the text to show for a location.</summary>
    /// <remarks>
    /// Sources, in order:
    /// <list type="number">
    ///   <item>The override asset (<see cref="NamesAssetName"/>), which content packs and players can edit.</item>
    ///   <item>The display name from <c>Data/Locations</c> (localized by the game, set by mods that add locations).</item>
    ///   <item>The name of the building the location is inside, from <c>Data/Buildings</c>.</item>
    ///   <item>Cellars, generated mine levels and the Volcano.</item>
    ///   <item>The mod's own i18n files, keyed by the raw location name.</item>
    ///   <item>The warp name from CJB Cheats Menu, when it's installed.</item>
    ///   <item>A readable version of the raw name.</item>
    /// </list>
    /// </remarks>
    public class LocationNameService
    {
        /// <summary>
        /// The asset content packs can edit to name a location (key: location name, value: text or tokenizable string).
        /// A blank value hides the popup for that location.
        /// </summary>
        public const string NamesAssetName = "Mods/Kkthnx.SimpleLocationDisplay/Names";

        /// <summary>The game's localized "Level {0}" text.</summary>
        private const string LevelStringPath = "Strings\\UI:Inventory_PortraitHover_Level";

        /// <summary>The game's localized "Volcano" label, as used on its world map.</summary>
        private const string VolcanoNameToken = "[LocalizedText Strings\\WorldMap:GingerIsland_North_Volcano]";

        /// <summary>The <c>Data/Locations</c> entry that holds the display name of the Mines.</summary>
        private const string MinesLocationId = "Mine";

        /// <summary>The value <see cref="MineShaft.getMineArea"/> returns for Skull Cavern levels.</summary>
        private const int SkullCavernArea = 121;

        /// <summary>The location name of the room at the top of the Skull Cavern.</summary>
        private const string SkullCavernEntranceName = "SkullCave";

        /// <summary>
        /// Vanilla locations with no display name in <c>Data/Locations</c>, mapped to text the game already
        /// translates. Each pairing comes from the game's own data: the string's key names the place,
        /// or the world map data puts the location and the string in the same map area.
        /// </summary>
        private static readonly Dictionary<string, string> VanillaNameTokens = new(StringComparer.Ordinal)
        {
            ["BeachNightMarket"] = "[LocalizedText Strings\\UI:Billboard_NightMarket]",
            ["DesertFestival"] = "[LocalizedText Strings\\1_6_Strings:DesertFestival]",
            ["IslandFarmHouse"] = "[LocalizedText Strings\\WorldMap:GingerIsland_West_House]",
            ["IslandFarmCave"] = "[LocalizedText Strings\\StringsFromCSFiles:FrogCave]",
            ["IslandFieldOffice"] = "[LocalizedText Strings\\WorldMap:GingerIsland_North_FieldOffice]",
            ["IslandSouthEastCave"] = "[LocalizedText Strings\\WorldMap:GingerIsland_SouthEast_PirateCove]",
            ["QiNutRoom"] = "[LocalizedText Strings\\WorldMap:GingerIsland_West_QiWalnutRoom]"
        };

        private readonly ITranslationHelper _i18n;
        private readonly IGameContentHelper _content;
        private readonly CheatsMenuWarpNames _warpNames;
        private readonly ModConfig _config;
        private readonly IMonitor _monitor;

        private IReadOnlyList<string> _modIds = new List<string>();
        private bool _loggedOverrideError;

        // Locations that fell through to the readable fallback, for the dump command.
        private readonly SortedDictionary<string, string> _missingTranslations = new();

        public LocationNameService(ITranslationHelper i18n, IGameContentHelper content, CheatsMenuWarpNames warpNames, ModConfig config, IMonitor monitor)
        {
            _i18n = i18n;
            _content = content;
            _warpNames = warpNames;
            _config = config;
            _monitor = monitor;
        }

        /// <summary>Give the service the unique IDs of the loaded mods, so it can strip them from raw location names.</summary>
        public void SetModIds(IEnumerable<string> modIds)
        {
            _modIds = new List<string>(modIds);
        }

        public void ResetCache()
        {
            _missingTranslations.Clear();
        }

        /// <summary>Get the text to show for a location.</summary>
        /// <returns>The text, or null if no popup should be shown for this location.</returns>
        public string? GetLocationName(GameLocation location)
        {
            // 1. Explicit overrides from content packs or the player
            if (TryGetOverride(location, out string? overrideName))
                return overrideName;

            // 2. Priority: the game's own display name (Data/Locations, localized by the game)
            string? displayName = location.GetDisplayName();
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;

            // 3. Building interiors use the building's name (Data/Buildings, localized by the game)
            string? buildingName = ParseToken(location.ParentBuilding?.GetData()?.Name);
            if (buildingName != null)
                return buildingName;

            // 4. Locations the game has no display name for
            string? builtInName = location switch
            {
                MineShaft shaft => GetMineShaftName(shaft),
                VolcanoDungeon volcano => GetVolcanoName(volcano),
                Cellar => TryGetTranslation("location.Cellar") ?? "Cellar",
                _ when location.Name == SkullCavernEntranceName => TryGetTranslation("location.skullCavern") ?? "Skull Cavern",
                _ when location.Name != null && VanillaNameTokens.TryGetValue(location.Name, out string? token) => ParseToken(token),
                _ => null
            };
            if (builtInName != null)
                return builtInName;

            // 5. Translation overrides in i18n, keyed by raw name ("location.My_Cool_Location")
            string? rawName = location.NameOrUniqueName;
            if (string.IsNullOrWhiteSpace(rawName))
                rawName = location.Name;
            if (string.IsNullOrWhiteSpace(rawName))
                return GetUnknownString();

            string baseName = SanitizeRawName(rawName);

            // Instanced locations (such as building interiors) have a unique name with a GUID,
            // so also try the plain name.
            string? translation = TryGetTranslation(NameFormatting.BuildTranslationKey(baseName));
            if (translation == null && !string.IsNullOrWhiteSpace(location.Name) && location.Name != baseName)
                translation = TryGetTranslation(NameFormatting.BuildTranslationKey(location.Name));
            if (translation != null)
                return translation;

            // 6. The name of the warp for this location in CJB Cheats Menu, if it's installed
            string? warpName = ParseToken(_warpNames.GetName(baseName));
            if (warpName == null && !string.IsNullOrWhiteSpace(location.Name) && location.Name != baseName)
                warpName = ParseToken(_warpNames.GetName(location.Name));
            if (warpName != null)
                return warpName;

            // Location mods keep rooms like "Custom_Alesia_WarpRoom" where NPCs wait between
            // schedule stops. They have no name worth showing, so no popup unless something above named them.
            if (baseName.Contains("WarpRoom", StringComparison.OrdinalIgnoreCase))
                return null;

            // 7. Fallback: procedural generation (human readable), without any mod prefix
            string fallback = NameFormatting.MakeHumanReadable(NameFormatting.StripModPrefix(baseName, _modIds), _config.ShowZeroAsL0, IsCharacterName);
            TrackMissingTranslation(baseName, rawName, fallback);

            return fallback;
        }

        public void DumpMissingTranslations(IDataHelper data)
        {
            if (_missingTranslations.Count == 0)
            {
                _monitor.Log("No missing translations encountered this session.", LogLevel.Info);
                return;
            }

            data.WriteJsonFile("i18n/missing.en.json", _missingTranslations);
            _monitor.Log($"Wrote {_missingTranslations.Count} missing keys to 'i18n/missing.en.json'.", LogLevel.Info);
        }

        /// <summary>Look the location up in the override asset, by its plain name and its unique name.</summary>
        /// <param name="name">The text to show, or null if the entry is blank (which hides the popup).</param>
        /// <returns>Returns whether the asset has an entry for the location.</returns>
        private bool TryGetOverride(GameLocation location, out string? name)
        {
            name = null;

            Dictionary<string, string>? overrides = LoadOverrides();
            if (overrides == null || overrides.Count == 0)
                return false;

            string? text = null;
            bool found = false;

            string? unique = location.NameOrUniqueName;
            if (!string.IsNullOrWhiteSpace(unique))
            {
                found = overrides.TryGetValue(unique, out text)
                    || overrides.TryGetValue(NameFormatting.StripGuidSuffix(unique), out text);
            }
            if (!found && !string.IsNullOrWhiteSpace(location.Name))
                found = overrides.TryGetValue(location.Name, out text);

            if (!found)
                return false;

            name = ParseToken(text);
            return true;
        }

        private Dictionary<string, string>? LoadOverrides()
        {
            try
            {
                return _content.Load<Dictionary<string, string>>(NamesAssetName);
            }
            catch (Exception ex)
            {
                // Another mod broke the asset. Say so once, then carry on without overrides.
                if (!_loggedOverrideError)
                {
                    _loggedOverrideError = true;
                    _monitor.Log($"Couldn't load '{NamesAssetName}', so location name overrides are off. Details: {ex.Message}", LogLevel.Warn);
                }
                return null;
            }
        }

        private string GetMineShaftName(MineShaft shaft)
        {
            // Same test the game uses for the quarry mine
            if (shaft.isQuarryArea || shaft.getMineArea() == MineShaft.quarryMineShaft)
                return TryGetTranslation("location.quarryMine") ?? "Quarry Mine";

            // The in-game level counter in the Skull Cavern starts again at 1
            if (shaft.getMineArea() == SkullCavernArea)
            {
                string cavern = TryGetTranslation("location.skullCavern") ?? "Skull Cavern";
                return WithLevel(cavern, shaft.mineLevel - MineShaft.bottomOfMineLevel);
            }

            string mines = ParseToken(GameLocation.GetData(MinesLocationId)?.DisplayName) ?? "Mines";
            return WithLevel(mines, shaft.mineLevel);
        }

        private string GetVolcanoName(VolcanoDungeon volcano)
        {
            string volcanoName = ParseToken(VolcanoNameToken) ?? "Volcano";
            return WithLevel(volcanoName, volcano.level.Value);
        }

        private string WithLevel(string name, int level)
        {
            // Level 0 is the entrance floor of the Volcano, which the game's counter leaves blank.
            if (level <= 0 && !_config.ShowZeroAsL0)
                return name;

            // Two lines: "Skull Cavern - Level 10" is wider than the popup, and the game would
            // break it after "Level", leaving the number alone on the second line.
            return $"{name}{Environment.NewLine}{GetLevelText(level)}";
        }

        private string GetLevelText(int level)
        {
            string? text = null;
            try
            {
                text = Game1.content.LoadStringReturnNullIfNotFound(LevelStringPath, level.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                _monitor.Log($"Couldn't load the game's level text; using English. Details: {ex.Message}", LogLevel.Trace);
            }

            // The French text has a double space, so tidy the result
            return string.IsNullOrWhiteSpace(text)
                ? $"Level {level}"
                : NameFormatting.CollapseSpaces(text);
        }

        /// <summary>Resolve a tokenizable string like <c>[LocalizedText Strings\UI:Key]</c>, or return null if it's empty.</summary>
        private static string? ParseToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            string text = TokenParser.ParseText(token);
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        /// <summary>Whether a word is the ID of a character in the game or in a mod ("Marnie", "Olivia").</summary>
        private static bool IsCharacterName(string word)
        {
            IDictionary<string, CharacterData>? characters = Game1.characterData;
            if (characters == null)
                return false;

            if (characters.ContainsKey(word))
                return true;

            foreach (string id in characters.Keys)
            {
                if (id.Equals(word, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private string? TryGetTranslation(string key)
        {
            Translation translation = _i18n.Get(key);
            return translation.HasValue() ? translation.ToString() : null;
        }

        private void TrackMissingTranslation(string baseName, string rawName, string fallback)
        {
            string key = NameFormatting.BuildTranslationKey(baseName);
            if (_missingTranslations.TryAdd(key, fallback) && _config.EnableDebugLogging)
            {
                _monitor.Log($"Missing translation key: '{key}' (raw: '{rawName}'). Using: '{fallback}'", LogLevel.Debug);
            }
        }

        private string SanitizeRawName(string name)
        {
            string baseName = NameFormatting.StripGuidSuffix(name);

            // Handle the edge case where the name was only a GUID
            return baseName.Length > 0 ? baseName : GetUnknownString();
        }

        private string GetUnknownString()
        {
            Translation unknown = _i18n.Get("misc.unknownLocation");
            return unknown.HasValue() ? unknown.ToString() : "Unknown Location";
        }
    }
}
