using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

namespace SimpleLocationDisplay
{
    public class ModEntry : Mod
    {
        private ModConfig config = new ModConfig();
        private HUDMessage? lastLocationMessage;
        private string? lastLocationName;

        private static readonly Regex GuidRegex = new Regex(
            @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        private static readonly Dictionary<string, string?> TranslationCache = new Dictionary<string, string?>();
        private readonly Dictionary<string, string?> translationCache = new Dictionary<string, string?>();
        private readonly Dictionary<string, string> missingTranslations = new Dictionary<string, string>();

        public override void Entry(IModHelper helper)
        {
            config = helper.ReadConfig<ModConfig>() ?? new ModConfig();
            helper.Events.Player.Warped += OnWarped;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.ConsoleCommands.Add("debug_location", "Prints current location details.", OnDebugLocationCommand);
            helper.ConsoleCommands.Add("sld_dump_missing_translations", "Writes missing i18n keys seen this session with suggested values to i18n/missing.en.json", OnDumpMissingTranslationsCommand);
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            ConfigMenu.SetupConfigUI(this, Helper, config);
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (!config.EnableMod || e.NewLocation == null) return;
            if (!Context.IsWorldReady || !e.IsLocalPlayer) return;

            string locationName = GetLocationName(e.NewLocation);
            if (locationName == lastLocationName) return;

            if (lastLocationMessage != null && Game1.hudMessages.Contains(lastLocationMessage))
            {
                Game1.hudMessages.Remove(lastLocationMessage);
            }

            lastLocationMessage = HUDMessage.ForCornerTextbox(locationName);
            lastLocationMessage.timeLeft = config.NotificationDuration;
            Game1.hudMessages.Add(lastLocationMessage);
            lastLocationName = locationName;

            LogDebug($"Displayed location: {locationName}");
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            if (lastLocationMessage != null && Game1.hudMessages.Contains(lastLocationMessage))
            {
                Game1.hudMessages.Remove(lastLocationMessage);
            }

            lastLocationMessage = null;
            lastLocationName = null;
            translationCache.Clear();
            missingTranslations.Clear();
        }

        private void OnDebugLocationCommand(string command, string[] args)
        {
            if (Game1.currentLocation == null)
            {
                Monitor.Log("No current location available.", LogLevel.Info);
                return;
            }

            string name = Game1.currentLocation.Name ?? "null";
            string uniqueName = Game1.currentLocation.NameOrUniqueName ?? "null";
            string displayName = Game1.currentLocation.GetDisplayName() ?? "null";
            string translatedName = GetLocationName(Game1.currentLocation);

            Monitor.Log($"Location Debug: Name='{name}', UniqueName='{uniqueName}', DisplayName='{displayName}', TranslatedName='{translatedName}'", LogLevel.Info);
        }

        private string GetLocationName(GameLocation location)
        {
            string? displayName = location.GetDisplayName();
            if (!string.IsNullOrEmpty(displayName))
            {
                LogDebug($"Using GetDisplayName: {displayName}");
                return displayName;
            }

            string rawName = location.NameOrUniqueName ?? "Unknown Location";
            if (string.IsNullOrEmpty(rawName)) rawName = GetUnknownString();
            string baseName = SanitizeRawName(rawName);
            LogDebug($"GetDisplayName failed, using base name: {baseName}");

            string translationKey = $"location.{baseName.Replace(" ", "_").Replace(".", "_")}";
            if (!translationCache.TryGetValue(translationKey, out string? translation))
            {
                translation = Helper.Translation.Get(translationKey);
                if (!string.IsNullOrEmpty(translation) && !translation.StartsWith("(no translation:"))
                {
                    translationCache[translationKey] = translation;
                    LogDebug($"Found translation for {baseName}: {translation}");
                    return translation;
                }
                else
                {
                    translationCache[translationKey] = null; // Cache "no translation" as null
                }
            }
            else if (translation != null)
            {
                LogDebug($"Using cached translation for {baseName}: {translation}");
                return translation;
            }

            string fallback = MakeHumanReadable(baseName);
            LogDebug($"No translation found, using fallback name: {fallback}");
            if (!missingTranslations.ContainsKey(translationKey))
            {
                missingTranslations[translationKey] = fallback;
            }
            return fallback;
        }

        private string SanitizeRawName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return GetUnknownString();

            if (name.Length > 36)
            {
                string potentialGuid = name.Substring(name.Length - 36);
                if (GuidRegex.IsMatch(potentialGuid))
                {
                    string baseName = name.Substring(0, name.Length - 36);
                    LogDebug($"Sanitized raw name from '{name}' to '{baseName}'");
                    return baseName.Length < 2 ? GetUnknownString() : baseName;
                }
            }

            return name.Length < 2 ? GetUnknownString() : name;
        }

        private void LogDebug(string message)
        {
            if (config.EnableDebugLogging)
            {
                Monitor.Log(message, LogLevel.Debug);
            }
        }

        private string MakeHumanReadable(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return GetUnknownString();

            // first, normalize trailing numeric floor suffixes to " L{n}" (optionally omitting L0)
            string levelNormalized = FormatLevelSuffix(raw);

            // replace underscores and dots with spaces
            string withSpaces = levelNormalized.Replace('_', ' ').Replace('.', ' ');
            // insert spaces between camelCase or PascalCase boundaries
            withSpaces = Regex.Replace(withSpaces, "([a-z])([A-Z])", "$1 $2");
            // collapse multiple spaces
            withSpaces = Regex.Replace(withSpaces, "\\s+", " ").Trim();
            // title case
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(withSpaces.ToLowerInvariant());
        }

        // Convert names like "VolcanoDungeon0" or "MineShaft12" into "VolcanoDungeon L0" / "MineShaft L12".
        // If level is 0 and config.ShowZeroAsL0 is false, we omit the "L0" and return just the base name.
        private string FormatLevelSuffix(string input)
        {
            var match = Regex.Match(input, @"^(.+?)(\d+)$");
            if (!match.Success)
                return input;

            string basePart = match.Groups[1].Value;
            int level;
            if (!int.TryParse(match.Groups[2].Value, out level))
                return input;

            if (level == 0 && !config.ShowZeroAsL0)
                return basePart; // show just the location name for level 0

            return basePart + " L" + level.ToString();
        }

        // Return localized fallback for unknown location
        private string GetUnknownString()
        {
            string unk = Helper.Translation.Get("misc.unknownLocation");
            if (!string.IsNullOrEmpty(unk) && !unk.StartsWith("(no translation:"))
                return unk;
            return "Unknown Location";
        }

        // Write missing translation keys encountered this session to i18n/missing.en.json
        private void OnDumpMissingTranslationsCommand(string command, string[] args)
        {
            if (missingTranslations.Count == 0)
            {
                Monitor.Log("No missing translations encountered this session.", LogLevel.Info);
                return;
            }

            string i18nDir = Path.Combine(Helper.DirectoryPath, "i18n");
            Directory.CreateDirectory(i18nDir);
            string outPath = Path.Combine(i18nDir, "missing.en.json");

            var keys = new List<string>(missingTranslations.Keys);
            keys.Sort();

            using (var writer = new StreamWriter(outPath, false))
            {
                writer.WriteLine("{");
                for (int i = 0; i < keys.Count; i++)
                {
                    string k = keys[i];
                    string v = missingTranslations[k].Replace("\"", "\\\"");
                    string comma = i < keys.Count - 1 ? "," : string.Empty;
                    writer.WriteLine($"\t\"{k}\": \"{v}\"{comma}");
                }
                writer.WriteLine("}");
            }

            Monitor.Log($"Wrote {missingTranslations.Count} missing keys to 'i18n/missing.en.json'.", LogLevel.Info);
        }
    }
}