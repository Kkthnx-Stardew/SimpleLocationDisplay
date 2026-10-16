using System.Text.Json;

namespace SimpleLocationDisplay.Tests
{
    /// <summary>Checks on the files that ship with the mod, which can break without any code changing.</summary>
    public class ShippedFilesTests
    {
        private static string ModFolder
        {
            get
            {
                DirectoryInfo? dir = new(AppContext.BaseDirectory);
                while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SimpleLocationDisplay.sln")))
                    dir = dir.Parent;

                Assert.NotNull(dir);
                return Path.Combine(dir!.FullName, "SimpleLocationDisplay");
            }
        }

        private static Dictionary<string, string> ReadStrings(string path)
        {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { AllowTrailingCommas = true });
            return doc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? "");
        }

        public static IEnumerable<object[]> TranslationFiles()
        {
            string i18n = Path.Combine(ModFolder, "i18n");
            foreach (string file in Directory.GetFiles(i18n, "*.json").OrderBy(f => f))
                yield return new object[] { Path.GetFileName(file) };
        }

        public class Translations
        {
            [Theory]
            [MemberData(nameof(TranslationFiles), MemberType = typeof(ShippedFilesTests))]
            public void Is_valid_json_with_no_blank_values(string fileName)
            {
                Dictionary<string, string> strings = ReadStrings(Path.Combine(ModFolder, "i18n", fileName));

                Assert.NotEmpty(strings);
                Assert.All(strings, pair => Assert.False(string.IsNullOrWhiteSpace(pair.Value), $"'{pair.Key}' is blank in {fileName}"));
            }

            [Theory]
            [MemberData(nameof(TranslationFiles), MemberType = typeof(ShippedFilesTests))]
            public void Only_uses_keys_that_exist_in_the_default_file(string fileName)
            {
                HashSet<string> defaultKeys = ReadStrings(Path.Combine(ModFolder, "i18n", "default.json")).Keys.ToHashSet();
                Dictionary<string, string> strings = ReadStrings(Path.Combine(ModFolder, "i18n", fileName));

                string[] unknown = strings.Keys.Where(key => !defaultKeys.Contains(key)).ToArray();
                Assert.True(unknown.Length == 0, $"{fileName} has keys that default.json doesn't: {string.Join(", ", unknown)}");
            }

            [Fact]
            public void Default_file_has_every_key_the_code_asks_for()
            {
                Dictionary<string, string> strings = ReadStrings(Path.Combine(ModFolder, "i18n", "default.json"));

                string[] required =
                {
                    "location.Cellar", "location.skullCavern", "location.quarryMine", "misc.unknownLocation",
                    "gmcm.title", "gmcm.description", "gmcm.enable.name", "gmcm.duration.name", "gmcm.saveload.name",
                    "gmcm.hidden.name", "gmcm.hidden.tooltip", "gmcm.debug.name", "gmcm.zero.name"
                };
                Assert.All(required, key => Assert.True(strings.ContainsKey(key), $"default.json is missing '{key}'"));
            }

            [Fact]
            public void The_ten_other_game_languages_and_spanish_each_have_a_file_with_the_dungeon_words()
            {
                string[] languages = { "de", "es", "fr", "hu", "it", "ja", "ko", "pt", "ru", "tr", "zh" };
                foreach (string language in languages)
                {
                    Dictionary<string, string> strings = ReadStrings(Path.Combine(ModFolder, "i18n", language + ".json"));
                    foreach (string key in new[] { "location.skullCavern", "location.Cellar", "location.quarryMine" })
                        Assert.True(strings.ContainsKey(key), $"{language}.json is missing '{key}'");
                }
            }
        }

        public class Manifest
        {
            private static JsonElement Read()
            {
                string path = Path.Combine(ModFolder, "manifest.json");
                using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { AllowTrailingCommas = true });
                return doc.RootElement.Clone();
            }

            [Fact]
            public void Takes_its_version_from_the_project_file()
            {
                Assert.Equal("%ProjectVersion%", Read().GetProperty("Version").GetString());
            }

            [Fact]
            public void Requires_at_least_the_game_version_that_has_the_members_the_code_calls()
            {
                // GameLocation.ParentBuilding and the LoadStringReturnNullIfNotFound substitution overload
                // were both added in Stardew Valley 1.6.9, so the mod can't run on anything older.
                string minimum = Read().GetProperty("MinimumGameVersion").GetString()!;
                Assert.True(Version.Parse(minimum) >= new Version(1, 6, 9), $"MinimumGameVersion is {minimum}");
            }

            [Fact]
            public void Lists_both_download_sites_for_update_checks()
            {
                string[] keys = Read().GetProperty("UpdateKeys").EnumerateArray().Select(e => e.GetString()!).ToArray();
                Assert.Contains(keys, key => key.StartsWith("Nexus:", StringComparison.Ordinal));
                Assert.Contains(keys, key => key.StartsWith("CurseForge:", StringComparison.Ordinal));
            }

            [Fact]
            public void Treats_Generic_Mod_Config_Menu_as_optional()
            {
                JsonElement dependency = Read().GetProperty("Dependencies").EnumerateArray()
                    .Single(d => d.GetProperty("UniqueID").GetString() == "spacechase0.GenericModConfigMenu");
                Assert.False(dependency.GetProperty("IsRequired").GetBoolean());
            }
        }
    }
}
