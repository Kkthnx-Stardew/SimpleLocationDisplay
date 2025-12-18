using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace SimpleLocationDisplay
{
    public class ModEntry : Mod
    {
        private ModConfig _config = new();
        private LocationNameService _nameService = null!; // Initialized in Entry

        // State
        private HUDMessage? _lastLocationMessage;
        private string? _lastLocationName;

        public override void Entry(IModHelper helper)
        {
            _config = helper.ReadConfig<ModConfig>() ?? new ModConfig();
            _nameService = new LocationNameService(helper.Translation, _config, Monitor);

            helper.Events.Player.Warped += OnWarped;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;

            helper.ConsoleCommands.Add("debug_location", "Prints current location details.", OnDebugLocationCommand);
            helper.ConsoleCommands.Add("sld_dump_missing_translations", "Writes missing i18n keys to i18n/missing.en.json", (_, _) => _nameService.DumpMissingTranslations(Helper.DirectoryPath));
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            ConfigMenu.SetupConfigUI(this, Helper, _config);
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            // Performance: Early exit checks
            if (!_config.EnableMod || !Context.IsWorldReady || !e.IsLocalPlayer || e.NewLocation == null)
                return;

            string locationName = _nameService.GetLocationName(e.NewLocation);

            // Optimization: Don't spam the same name (e.g. warping within the same building)
            if (locationName == _lastLocationName) return;

            ShowNotification(locationName);
        }

        private void ShowNotification(string text)
        {
            // Remove previous message to prevent stacking/clutter
            if (_lastLocationMessage != null && Game1.hudMessages.Contains(_lastLocationMessage))
            {
                Game1.hudMessages.Remove(_lastLocationMessage);
            }

            _lastLocationMessage = HUDMessage.ForCornerTextbox(text);
            _lastLocationMessage.timeLeft = _config.NotificationDuration;
            Game1.hudMessages.Add(_lastLocationMessage);

            _lastLocationName = text;
            LogDebug($"Displayed location: {text}");
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            if (_lastLocationMessage != null && Game1.hudMessages.Contains(_lastLocationMessage))
            {
                Game1.hudMessages.Remove(_lastLocationMessage);
            }

            _lastLocationMessage = null;
            _lastLocationName = null;

            // Clear caches so we don't hold stale data
            _nameService.ResetCache();
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
            string translatedName = _nameService.GetLocationName(Game1.currentLocation);

            Monitor.Log($"Location Debug: Name='{name}', UniqueName='{uniqueName}', DisplayName='{displayName}', Resolved='{translatedName}'", LogLevel.Info);
        }

        private void LogDebug(string message)
        {
            if (_config.EnableDebugLogging)
            {
                Monitor.Log(message, LogLevel.Debug);
            }
        }
    }
}