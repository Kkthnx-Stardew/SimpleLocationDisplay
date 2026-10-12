using System.Globalization;
using SimpleLocationDisplay;

namespace SimpleLocationDisplay.Tests
{
    public class NameFormattingTests
    {
        private const string Guid = "83827173-aaaa-bbbb-cccc-1234567890ab";

        public class StripGuidSuffix
        {
            [Theory]
            [InlineData("Coop2_" + Guid, "Coop2")]
            [InlineData("Cabin-" + Guid, "Cabin")]
            [InlineData("Shed" + Guid, "Shed")]
            [InlineData("Coop2_83827173-AAAA-BBBB-CCCC-1234567890AB", "Coop2")]
            [InlineData("FarmHouse", "FarmHouse")]
            [InlineData("Custom_Cave_2", "Custom_Cave_2")]
            public void Removes_the_guid_and_its_separator(string input, string expected)
            {
                Assert.Equal(expected, NameFormatting.StripGuidSuffix(input));
            }

            [Fact]
            public void Returns_empty_when_the_name_is_only_a_guid()
            {
                Assert.Equal("", NameFormatting.StripGuidSuffix(Guid));
            }

            [Fact]
            public void Leaves_a_guid_in_the_middle_alone()
            {
                string input = Guid + "_Cabin";
                Assert.Equal(input, NameFormatting.StripGuidSuffix(input));
            }
        }

        public class BuildTranslationKey
        {
            [Theory]
            [InlineData("FarmHouse", "location.FarmHouse")]
            [InlineData("Deluxe Coop", "location.Deluxe_Coop")]
            [InlineData("Author.Mod_Cave", "location.Author_Mod_Cave")]
            public void Replaces_spaces_and_dots_with_underscores(string input, string expected)
            {
                Assert.Equal(expected, NameFormatting.BuildTranslationKey(input));
            }
        }

        public class StripModPrefix
        {
            private static readonly string[] NoMods = Array.Empty<string>();

            [Fact]
            public void Removes_a_loaded_mods_unique_id()
            {
                var mods = new[] { "Kkthnx.Example" };
                Assert.Equal("HiddenCave", NameFormatting.StripModPrefix("Kkthnx.Example_HiddenCave", mods));
            }

            [Fact]
            public void Ignores_case_when_matching_the_id()
            {
                var mods = new[] { "Kkthnx.Example" };
                Assert.Equal("Cave", NameFormatting.StripModPrefix("kkthnx.example_Cave", mods));
            }

            [Fact]
            public void Prefers_the_longest_matching_id()
            {
                var mods = new[] { "Author.Mod", "Author.Mod_Extra" };
                Assert.Equal("Cave", NameFormatting.StripModPrefix("Author.Mod_Extra_Cave", mods));
            }

            [Fact]
            public void Requires_an_underscore_after_the_id()
            {
                var mods = new[] { "Author.Mod" };
                Assert.Equal("Author.ModCave", NameFormatting.StripModPrefix("Author.ModCave", mods));
            }

            [Fact]
            public void Removes_the_legacy_custom_prefix()
            {
                Assert.Equal("ARVHouse", NameFormatting.StripModPrefix("Custom_ARVHouse", NoMods));
            }

            [Theory]
            [InlineData("FarmHouse")]
            [InlineData("Custom_")]
            [InlineData("Customize")]
            public void Leaves_other_names_alone(string name)
            {
                Assert.Equal(name, NameFormatting.StripModPrefix(name, NoMods));
            }

            [Fact]
            public void Keeps_the_name_when_nothing_would_be_left()
            {
                var mods = new[] { "Author.Mod" };
                Assert.Equal("Author.Mod_", NameFormatting.StripModPrefix("Author.Mod_", mods));
            }
        }

        public class CollapseSpaces
        {
            [Theory]
            [InlineData("Niveau  12", "Niveau 12")]
            [InlineData("  Level 3 ", "Level 3")]
            [InlineData("A \t B", "A B")]
            public void Uses_single_spaces_and_trims(string input, string expected)
            {
                Assert.Equal(expected, NameFormatting.CollapseSpaces(input));
            }
        }

        public class MakeHumanReadable
        {
            [Theory]
            [InlineData("Farm", "Farm")]
            [InlineData("FrontierFarm", "Frontier Farm")]
            [InlineData("Frontier_Farm_Hidden_Cave", "Frontier Farm Hidden Cave")]
            [InlineData("Custom_FrontierFarm_HiddenCave", "Custom Frontier Farm Hidden Cave")]
            [InlineData("Some.Mod_Name", "Some Mod Name")]
            [InlineData("A__B", "A B")]
            public void Splits_separators_and_camel_case(string input, string expected)
            {
                Assert.Equal(expected, NameFormatting.MakeHumanReadable(input, showZeroLevel: false));
            }

            [Theory]
            [InlineData("ARVHouse", "ARV House")]
            [InlineData("SBV_RipleyHouse", "SBV Ripley House")]
            public void Keeps_short_acronyms_in_capitals(string input, string expected)
            {
                Assert.Equal(expected, NameFormatting.MakeHumanReadable(input, showZeroLevel: false));
            }

            // Names taken from real location mods
            [Theory]
            [InlineData("Ridgeside_BertHouse2ndFloor", "Ridgeside Bert House 2nd Floor")]
            [InlineData("Ridgeside_3Bros2ndFloor", "Ridgeside 3Bros 2nd Floor")]
            [InlineData("SBV_Mines10A", "SBV Mines 10A")]
            [InlineData("SBV_Mines5B", "SBV Mines 5B")]
            [InlineData("SBV_Mines12", "SBV Mines 12")]
            [InlineData("Ridgeside_RidgesideVillage", "Ridgeside Village")]
            [InlineData("Ridgeside_RSVGreenhouse1", "Ridgeside RSV Greenhouse 1")]
            [InlineData("Ridgeside_Ridge_KennethDate_ON", "Ridgeside Ridge Kenneth Date ON")]
            [InlineData("EastScarp_MeadowShed_Ruins", "East Scarp Meadow Shed Ruins")]
            [InlineData("ZCCC_FoodCourt", "ZCCC Food Court")]
            public void Reads_names_from_real_mods_cleanly(string input, string expected)
            {
                Assert.Equal(expected, NameFormatting.MakeHumanReadable(input, showZeroLevel: false));
            }

            private static readonly HashSet<string> People = new(StringComparer.OrdinalIgnoreCase)
            {
                "Marnie", "Andy", "Lewis", "Sandy", "Gunther", "Hank", "Wizard", "Leo"
            };

            private static bool IsPerson(string word) => People.Contains(word);

            [Theory]
            [InlineData("MarnieShed", "Marnie's Shed")]
            [InlineData("Custom_AndyHouse", "Custom Andy's House")]
            [InlineData("LewisBasement", "Lewis' Basement")]
            [InlineData("GunthersRoom", "Gunther's Room")]
            [InlineData("HanksGarage", "Hank's Garage")]
            [InlineData("SandyHouse", "Sandy's House")]
            [InlineData("WizardBasement", "Wizard's Basement")]
            public void Adds_a_possessive_when_a_character_name_is_followed_by_a_place_word(string input, string expected)
            {
                Assert.Equal(expected, NameFormatting.MakeHumanReadable(input, showZeroLevel: false, IsPerson));
            }

            [Theory]
            [InlineData("AndyIsland")]       // not a place word
            [InlineData("LeoTreeHouse")]     // the person is not right before a place word
            [InlineData("WitchHut")]         // not a character
            [InlineData("Ranch")]            // no person at all
            public void Leaves_other_names_without_a_possessive(string input)
            {
                string without = NameFormatting.MakeHumanReadable(input, showZeroLevel: false);
                Assert.Equal(without, NameFormatting.MakeHumanReadable(input, showZeroLevel: false, IsPerson));
                Assert.DoesNotContain("'", without);
            }

            [Fact]
            public void Skips_possessives_when_no_character_check_is_given()
            {
                Assert.Equal("Marnie Shed", NameFormatting.MakeHumanReadable("MarnieShed", showZeroLevel: false));
            }

            [Fact]
            public void Title_cases_long_all_caps_words()
            {
                Assert.Equal("Bugland", NameFormatting.MakeHumanReadable("BUGLAND", showZeroLevel: false));
            }

            [Theory]
            [InlineData("DeepDungeon12", "Deep Dungeon L12")]
            [InlineData("OldMine3", "Old Mine L3")]
            [InlineData("Some_Quarry7", "Some Quarry L7")]
            public void Adds_a_level_to_names_that_end_in_a_dungeon_word_and_a_number(string input, string expected)
            {
                Assert.Equal(expected, NameFormatting.MakeHumanReadable(input, showZeroLevel: false));
            }

            [Fact]
            public void Drops_level_zero_unless_asked_to_show_it()
            {
                Assert.Equal("Deep Dungeon", NameFormatting.MakeHumanReadable("DeepDungeon0", showZeroLevel: false));
                Assert.Equal("Deep Dungeon L0", NameFormatting.MakeHumanReadable("DeepDungeon0", showZeroLevel: true));
            }

            [Theory]
            [InlineData("Coop2", "Coop 2")]
            [InlineData("Cabin5", "Cabin 5")]
            [InlineData("Cave3", "Cave 3")]
            [InlineData("IslandNorthCave1", "Island North Cave 1")]
            [InlineData("Cave_2", "Cave 2")]
            public void Separates_trailing_numbers_on_other_names(string input, string expected)
            {
                Assert.Equal(expected, NameFormatting.MakeHumanReadable(input, showZeroLevel: false));
            }

            [Fact]
            public void Does_not_use_the_current_cultures_casing_rules()
            {
                CultureInfo original = CultureInfo.CurrentCulture;
                try
                {
                    CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                    Assert.Equal("Inn Room", NameFormatting.MakeHumanReadable("InnRoom", showZeroLevel: false));
                }
                finally
                {
                    CultureInfo.CurrentCulture = original;
                }
            }
        }
    }
}
