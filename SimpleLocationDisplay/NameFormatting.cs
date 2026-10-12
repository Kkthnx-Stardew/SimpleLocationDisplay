using System.Globalization;
using System.Text.RegularExpressions;

namespace SimpleLocationDisplay
{
    /// <summary>
    /// String helpers that turn raw location names into readable text.
    /// Nothing here touches the game, so the logic can be unit tested on its own.
    /// </summary>
    public static class NameFormatting
    {
        // Content Patcher's older custom locations feature required this prefix.
        private const string LegacyPrefix = "Custom_";

        // Trailing GUID, optionally preceded by a separator (e.g. "Coop2_83827173-...").
        private static readonly Regex GuidSuffixRegex = new(
            @"[_-]?[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        // Only names that look like leveled areas ("DeepDungeon12", "OldMine3").
        private static readonly Regex LevelSuffixRegex = new(
            @"^(?<base>.+?(?:Mine|Shaft|Dungeon|Floor|Level|Quarry))(?<level>\d+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        // A number that sticks to the letters before it: "IslandNorthCave1", "BertHouse2ndFloor".
        private static readonly Regex LetterDigitBoundaryRegex = new(
            @"(?<=[A-Za-z])(?=\d)",
            RegexOptions.Compiled
        );

        // "ARVHouse" -> "ARV House": a run of capitals followed by a capitalized word.
        private static readonly Regex AcronymBoundaryRegex = new(
            "([A-Z]+)([A-Z][a-z])",
            RegexOptions.Compiled
        );

        // "FrontierFarm" -> "Frontier Farm"
        private static readonly Regex CamelCaseRegex = new(
            "([a-z])([A-Z])",
            RegexOptions.Compiled
        );

        private static readonly Regex SpaceCollapseRegex = new(
            @"\s+",
            RegexOptions.Compiled
        );

        /// <summary>Remove a trailing GUID (and the separator before it) from a unique location name.</summary>
        /// <returns>The remaining name, or an empty string if the name was only a GUID.</returns>
        public static string StripGuidSuffix(string name)
        {
            Match match = GuidSuffixRegex.Match(name);
            if (!match.Success)
                return name;

            return name.Substring(0, match.Index).TrimEnd('_', '-', ' ');
        }

        /// <summary>
        /// Remove the prefix content packs put in front of location names: the owning mod's unique ID
        /// (<c>Author.ModName_Cave</c>), or the older <c>Custom_</c> prefix.
        /// </summary>
        /// <param name="name">The location name, without any GUID suffix.</param>
        /// <param name="modIds">The unique IDs of the loaded mods.</param>
        /// <returns>The name without its prefix, or the original name if it has none or nothing would be left.</returns>
        public static string StripModPrefix(string name, IReadOnlyList<string> modIds)
        {
            // The longest matching ID wins, since one mod's ID can start with another's.
            string bestId = string.Empty;
            foreach (string id in modIds)
            {
                if (id.Length > bestId.Length
                    && name.Length > id.Length + 1
                    && name[id.Length] == '_'
                    && name.StartsWith(id, StringComparison.OrdinalIgnoreCase))
                {
                    bestId = id;
                }
            }

            if (bestId.Length > 0)
                return name.Substring(bestId.Length + 1);

            if (name.Length > LegacyPrefix.Length && name.StartsWith(LegacyPrefix, StringComparison.OrdinalIgnoreCase))
                return name.Substring(LegacyPrefix.Length);

            return name;
        }

        /// <summary>Build the i18n key used to override the name of a location.</summary>
        /// <example><c>"Deluxe Coop"</c> becomes <c>"location.Deluxe_Coop"</c>.</example>
        public static string BuildTranslationKey(string baseName)
        {
            return "location." + baseName.Replace(' ', '_').Replace('.', '_');
        }

        /// <summary>Replace runs of whitespace with a single space and trim the ends.</summary>
        public static string CollapseSpaces(string text)
        {
            return SpaceCollapseRegex.Replace(text, " ").Trim();
        }

        /// <summary>Turn a raw internal name like <c>Custom_FrontierFarm_HiddenCave</c> into <c>Custom Frontier Farm Hidden Cave</c>.</summary>
        /// <param name="raw">The raw location name, without any GUID suffix.</param>
        /// <param name="showZeroLevel">Whether a trailing level of 0 is shown as "L0" instead of being dropped.</param>
        /// <param name="isPersonName">Tells whether a word is a character's name, so "MarnieShed" can become "Marnie's Shed". Leave null to skip that.</param>
        public static string MakeHumanReadable(string raw, bool showZeroLevel, Func<string, bool>? isPersonName = null)
        {
            // 1. Normalize "DeepDungeon10" -> "DeepDungeon L10", or "Cave1" -> "Cave 1"
            string text = TryFormatLevelSuffix(raw, showZeroLevel, out string leveled)
                ? leveled
                : LetterDigitBoundaryRegex.Replace(raw, " ");

            // 2. Replace separators with spaces
            text = text.Replace('_', ' ').Replace('.', ' ');

            // 3. Split acronyms and CamelCase
            text = AcronymBoundaryRegex.Replace(text, "$1 $2");
            text = CamelCaseRegex.Replace(text, "$1 $2");

            // 4. Collapse multiple spaces
            text = CollapseSpaces(text);

            // 5. Drop a word repeated by a mod's label ("Ridgeside Ridgeside Village")
            var words = new List<string>();
            foreach (string word in text.Split(' '))
            {
                if (words.Count > 0 && string.Equals(words[^1], word, StringComparison.OrdinalIgnoreCase))
                    continue;

                words.Add(word);
            }

            // 6. Possessives: "Marnie Shed" -> "Marnie's Shed", "Gunthers Room" -> "Gunther's Room"
            if (isPersonName != null)
            {
                for (int i = 0; i < words.Count - 1; i++)
                {
                    if (PlaceNouns.Contains(words[i + 1]))
                        words[i] = MakePossessive(words[i], isPersonName);
                }
            }

            // 7. Title Case, keeping short all-caps words ("ARV") and words with digits ("2nd", "10A") as written.
            // Invariant casing: the current culture would turn "i" into a dotted capital in Turkish.
            TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;
            for (int i = 0; i < words.Count; i++)
            {
                string word = words[i];
                if (IsAcronym(word) || word.Any(char.IsDigit))
                    continue;

                // Keep an apostrophe-s we added lowercase ("Marnie's", not "Marnie'S")
                int apostrophe = word.IndexOf('\'');
                words[i] = apostrophe < 0
                    ? textInfo.ToTitleCase(word.ToLowerInvariant())
                    : textInfo.ToTitleCase(word.Substring(0, apostrophe).ToLowerInvariant()) + word.Substring(apostrophe).ToLowerInvariant();
            }

            return string.Join(' ', words);
        }

        // Words that name a place that belongs to someone: "Andy's House", "Lewis' Basement".
        private static readonly HashSet<string> PlaceNouns = new(StringComparer.OrdinalIgnoreCase)
        {
            "House", "Home", "Room", "Bedroom", "Shed", "Cellar", "Basement", "Garage", "Barn", "Cabin",
            "Cottage", "Office", "Shop", "Store", "Hideaway", "Property", "Orchard", "Backyard", "Garden",
            "Greenhouse", "Workshop", "Lab", "Hut", "Den", "Study", "Studio", "Farm", "Ranch", "Clinic", "Cave"
        };

        private static string MakePossessive(string word, Func<string, bool> isPersonName)
        {
            // "Lewis" -> "Lewis'", "Marnie" -> "Marnie's"
            if (isPersonName(word))
                return word.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? word + "'" : word + "'s";

            // "Gunthers" -> "Gunther's"
            if (word.Length > 2 && word.EndsWith("s", StringComparison.OrdinalIgnoreCase) && isPersonName(word[..^1]))
                return word[..^1] + "'s";

            return word;
        }

        private static bool IsAcronym(string word)
        {
            if (word.Length < 2 || word.Length > 4)
                return false;

            foreach (char c in word)
            {
                if (c < 'A' || c > 'Z')
                    return false;
            }

            return true;
        }

        private static bool TryFormatLevelSuffix(string input, bool showZeroLevel, out string formatted)
        {
            formatted = input;

            Match match = LevelSuffixRegex.Match(input);
            if (!match.Success)
                return false;

            string basePart = match.Groups["base"].Value;
            if (!int.TryParse(match.Groups["level"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int level))
                return false;

            formatted = level == 0 && !showZeroLevel
                ? basePart
                : $"{basePart} L{level}";
            return true;
        }
    }
}
