using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace SimpleLocationDisplay
{
    public class LocationNameService
    {
        private readonly ITranslationHelper _i18n;
        private readonly ModConfig _config;
        private readonly IMonitor _monitor;

        // Caches
        private readonly Dictionary<string, string?> _translationCache = new();
        private readonly Dictionary<string, string> _missingTranslations = new();

        // Compile regexes for performance
        private static readonly Regex GuidRegex = new(
            @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        private static readonly Regex LevelSuffixRegex = new(
            @"^(.+?)(\d+)$",
            RegexOptions.Compiled
        );

        private static readonly Regex CamelCaseRegex = new(
            "([a-z])([A-Z])",
            RegexOptions.Compiled
        );

        private static readonly Regex SpaceCollapseRegex = new(
            @"\s+",
            RegexOptions.Compiled
        );

        public LocationNameService(ITranslationHelper i18n, ModConfig config, IMonitor monitor)
        {
            _i18n = i18n;
            _config = config;
            _monitor = monitor;
        }

        public void ResetCache()
        {
            _translationCache.Clear();
            _missingTranslations.Clear();
        }

        public string GetLocationName(GameLocation location)
        {
            // 1. Priority: Game's official display name (handles 1.6 Data/Locations)
            string? displayName = location.GetDisplayName();
            if (!string.IsNullOrEmpty(displayName))
            {
                return displayName;
            }

            // 2. Fallback: Parse the raw unique name
            string rawName = location.NameOrUniqueName ?? "Unknown Location";
            if (string.IsNullOrEmpty(rawName)) rawName = GetUnknownString();

            string baseName = SanitizeRawName(rawName);

            // 3. Check for manual translation overrides in i18n
            // Key format: "location.My_Cool_Location"
            string translationKey = $"location.{baseName.Replace(" ", "_").Replace(".", "_")}";

            if (_translationCache.TryGetValue(translationKey, out string? cached))
            {
                if (cached != null) return cached;
            }
            else
            {
                // Try to fetch from i18n
                string translation = _i18n.Get(translationKey);
                if (!string.IsNullOrEmpty(translation) && !translation.StartsWith("(no translation:"))
                {
                    _translationCache[translationKey] = translation;
                    return translation;
                }

                // Mark as not found in cache so we don't look it up again
                _translationCache[translationKey] = null;
            }

            // 4. Fallback: Procedural generation (Human Readable)
            string fallback = MakeHumanReadable(baseName);

            // Log missing translation for dev tools
            if (!_missingTranslations.ContainsKey(translationKey))
            {
                _missingTranslations[translationKey] = fallback;
            }

            return fallback;
        }

        public void DumpMissingTranslations(string directoryPath)
        {
            if (_missingTranslations.Count == 0)
            {
                _monitor.Log("No missing translations encountered this session.", LogLevel.Info);
                return;
            }

            string i18nDir = Path.Combine(directoryPath, "i18n");
            Directory.CreateDirectory(i18nDir);
            string outPath = Path.Combine(i18nDir, "missing.en.json");

            var keys = _missingTranslations.Keys.ToList();
            keys.Sort();

            using var writer = new StreamWriter(outPath, false);
            writer.WriteLine("{");
            for (int i = 0; i < keys.Count; i++)
            {
                string k = keys[i];
                string v = _missingTranslations[k].Replace("\"", "\\\"");
                string comma = i < keys.Count - 1 ? "," : string.Empty;
                writer.WriteLine($"\t\"{k}\": \"{v}\"{comma}");
            }
            writer.WriteLine("}");

            _monitor.Log($"Wrote {_missingTranslations.Count} missing keys to 'i18n/missing.en.json'.", LogLevel.Info);
        }

        private string SanitizeRawName(string name)
        {
            if (string.IsNullOrEmpty(name)) return GetUnknownString();

            // Strip GUIDs (e.g. "_83827173-...")
            if (name.Length > 36)
            {
                string potentialGuid = name.Substring(name.Length - 36);
                if (GuidRegex.IsMatch(potentialGuid))
                {
                    string baseName = name.Substring(0, name.Length - 36);
                    // Handle edge case where name was ONLY a GUID
                    return baseName.Length < 2 ? GetUnknownString() : baseName;
                }
            }

            return name.Length < 2 ? GetUnknownString() : name;
        }

        private string MakeHumanReadable(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return GetUnknownString();

            // 1. Normalize "MineShaft10" -> "MineShaft L10"
            string levelNormalized = FormatLevelSuffix(raw);

            // 2. Replace separators with spaces
            string withSpaces = levelNormalized.Replace('_', ' ').Replace('.', ' ');

            // 3. Split CamelCase (e.g. "FrontierFarm" -> "Frontier Farm")
            withSpaces = CamelCaseRegex.Replace(withSpaces, "$1 $2");

            // 4. Collapse multiple spaces
            withSpaces = SpaceCollapseRegex.Replace(withSpaces, " ").Trim();

            // 5. Title Case
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(withSpaces.ToLowerInvariant());
        }

        private string FormatLevelSuffix(string input)
        {
            var match = LevelSuffixRegex.Match(input);
            if (!match.Success) return input;

            string basePart = match.Groups[1].Value;
            if (int.TryParse(match.Groups[2].Value, out int level))
            {
                // Config check: omit "L0" if configured
                if (level == 0 && !_config.ShowZeroAsL0)
                    return basePart;

                return $"{basePart} L{level}";
            }

            return input;
        }

        private string GetUnknownString()
        {
            string unk = _i18n.Get("misc.unknownLocation");
            return (!string.IsNullOrEmpty(unk) && !unk.StartsWith("(no translation:"))
                ? unk
                : "Unknown Location";
        }
    }
}