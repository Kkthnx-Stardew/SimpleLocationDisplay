namespace SimpleLocationDisplay.Tests
{
    public class ModConfigTests
    {
        public class Clamp
        {
            [Theory]
            [InlineData(0f, 1000f)]
            [InlineData(999f, 1000f)]
            [InlineData(3500f, 3500f)]
            [InlineData(10001f, 10000f)]
            [InlineData(float.MaxValue, 10000f)]
            public void Keeps_the_duration_in_the_menus_range(float input, float expected)
            {
                var config = new ModConfig { NotificationDuration = input };
                config.Clamp();
                Assert.Equal(expected, config.NotificationDuration);
            }

            [Fact]
            public void Tidies_the_hidden_list()
            {
                var config = new ModConfig { HiddenLocations = new List<string> { " Farm ", "", "  ", "farm", "Town" } };
                config.Clamp();
                Assert.Equal(new[] { "Farm", "Town" }, config.HiddenLocations);
            }

            [Fact]
            public void Replaces_a_null_hidden_list_from_a_hand_edited_file()
            {
                var config = new ModConfig { HiddenLocations = null! };
                config.Clamp();
                Assert.Empty(config.HiddenLocations);
            }
        }

        public class IsHidden
        {
            private static ModConfig WithHidden(params string[] names) => new() { HiddenLocations = names.ToList() };

            [Theory]
            [InlineData("Farm")]
            [InlineData("farm")]
            [InlineData("  FARM ")]
            public void Matches_ignoring_case_and_padding(string name)
            {
                Assert.True(WithHidden("Farm").IsHidden(name));
            }

            [Theory]
            [InlineData("Farm House")]
            [InlineData("")]
            [InlineData(null)]
            public void Does_not_match_other_names(string? name)
            {
                Assert.False(WithHidden("Farm").IsHidden(name));
            }

            [Fact]
            public void Hides_nothing_by_default()
            {
                Assert.False(new ModConfig().IsHidden("Farm"));
            }
        }

        public class HiddenLocationsText
        {
            [Fact]
            public void Round_trips_through_the_menu_text()
            {
                var config = new ModConfig();
                config.SetHiddenLocations("Farm, Pelican Town ,,Custom_Foo");
                Assert.Equal(new[] { "Farm", "Pelican Town", "Custom_Foo" }, config.HiddenLocations);
                Assert.Equal("Farm, Pelican Town, Custom_Foo", config.GetHiddenLocationsText());
            }

            [Fact]
            public void Empty_text_clears_the_list()
            {
                var config = new ModConfig { HiddenLocations = new List<string> { "Farm" } };
                config.SetHiddenLocations("  ");
                Assert.Empty(config.HiddenLocations);
                Assert.Equal("", config.GetHiddenLocationsText());
            }
        }
    }
}
