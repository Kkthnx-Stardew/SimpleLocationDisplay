using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace SimpleLocationDisplay
{
    public class ModEntry : Mod
    {
        private ModConfig _config = new();
        private LocationNameService _nameService = null!; // Initialized in Entry
        private CheatsMenuWarpNames _warpNames = null!; // Initialized in Entry

        // State. Each split-screen player has their own HUD and location, so keep it per screen.
        private readonly PerScreen<HUDMessage?> _lastLocationMessage = new();
        private readonly PerScreen<string?> _lastLocationKey = new();
        private readonly PerScreen<GameLocation?> _pendingLocation = new(); // entered during a cutscene, not announced yet

        // Locations whose name lookup failed, so the warning is logged only once each
        private readonly HashSet<string> _failedLocationKeys = new();

        public override void Entry(IModHelper helper)
        {
            I18n.Init(helper.Translation);

            _config = helper.ReadConfig<ModConfig>() ?? new ModConfig();
            _config.Clamp();
            _warpNames = new CheatsMenuWarpNames(helper.GameContent, helper.ModRegistry, Monitor);
            _nameService = new LocationNameService(helper.Translation, helper.GameContent, _warpNames, _config, Monitor);

            helper.Events.Player.Warped += OnWarped;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
            helper.Events.Content.LocaleChanged += OnLocaleChanged;

            helper.ConsoleCommands.Add("debug_location", "Prints current location details for Simple Location Display.", OnDebugLocationCommand);
            helper.ConsoleCommands.Add("sld_dump_missing_translations", "Writes missing i18n keys to i18n/missing.en.json", (_, _) => _nameService.DumpMissingTranslations(Helper.Data));
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // Every mod is loaded by now
            _nameService.SetModIds(Helper.ModRegistry.GetAll().Select(mod => mod.Manifest.UniqueID));

            ConfigMenu.SetupConfigUI(this, Helper, _config);
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // Start with an empty list of overrides so content packs have something to edit
            if (e.NameWithoutLocale.IsEquivalentTo(LocationNameService.NamesAssetName))
            {
                e.LoadFrom(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), AssetLoadPriority.Exclusive);
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            if (!_config.EnableMod || !_config.ShowOnSaveLoad || !Context.IsWorldReady)
                return;

            GameLocation? location = Game1.player?.currentLocation;
            if (location != null)
                TryShowLocation(location);
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (Game1.eventUp && !Game1.isFestival())
            {
                // A cutscene can start a moment after the warp, once the popup is already up.
                // Take it down and show it again when the cutscene ends.
                HUDMessage? shown = _lastLocationMessage.Value;
                if (shown != null && Game1.hudMessages.Contains(shown))
                {
                    ClearLastMessage();
                    _lastLocationKey.Value = null;
                    _pendingLocation.Value = Game1.player?.currentLocation;
                }
                return;
            }

            // Cheap check on most ticks: only act when a popup was held back for a cutscene
            GameLocation? pending = _pendingLocation.Value;
            if (pending == null)
                return;

            _pendingLocation.Value = null;
            if (_config.EnableMod && Context.IsWorldReady && Game1.player?.currentLocation == pending)
                TryShowLocation(pending);
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            // Performance: Early exit checks
            if (!_config.EnableMod || !Context.IsWorldReady || !e.IsLocalPlayer || e.NewLocation == null)
                return;

            TryShowLocation(e.NewLocation);
        }

        private void TryShowLocation(GameLocation location)
        {
            // Events, festivals and minigames run in short-lived "Temp" locations
            if (location.IsTemporary)
                return;

            // Cutscenes in real locations: the game still draws HUD messages over them, so wait
            // for the event to end. The player often stays in the same place, so there is no new warp.
            if (Game1.eventUp && !Game1.isFestival())
            {
                _pendingLocation.Value = location;
                return;
            }
            _pendingLocation.Value = null;

            // Optimization: Don't spam the same location (e.g. warping within the same building).
            // Keyed on the raw location name rather than the display text, so two different
            // locations that share a display name still get their own popup.
            string key = location.NameOrUniqueName ?? location.Name ?? string.Empty;
            if (key.Length > 0 && key == _lastLocationKey.Value)
                return;

            // Places the player chose to hide, by internal name
            if (_config.IsHidden(key) || _config.IsHidden(NameFormatting.StripGuidSuffix(key)) || _config.IsHidden(location.Name))
                return;

            string? locationName = GetLocationNameSafely(location, key);
            if (string.IsNullOrWhiteSpace(locationName) || _config.IsHidden(locationName))
                return;

            ShowNotification(locationName);
            _lastLocationKey.Value = key;
        }

        /// <summary>Resolve the name, falling back to the raw one if another mod's location data makes the lookup fail.</summary>
        private string? GetLocationNameSafely(GameLocation location, string key)
        {
            try
            {
                return _nameService.GetLocationName(location);
            }
            catch (Exception ex)
            {
                // Say so once per location, not on every warp
                if (_failedLocationKeys.Add(key))
                {
                    Monitor.Log($"Couldn't work out the name of location '{key}', so its raw name is used instead. This is usually bad location data from another mod. Details: {ex.Message}", LogLevel.Warn);
                    Monitor.Log(ex.ToString(), LogLevel.Trace);
                }

                return NameFormatting.MakeHumanReadable(NameFormatting.StripGuidSuffix(key), _config.ShowZeroAsL0);
            }
        }

        private void ShowNotification(string text)
        {
            // Remove previous message to prevent stacking/clutter
            ClearLastMessage();

            HUDMessage message = HUDMessage.ForCornerTextbox(text);
            message.timeLeft = _config.NotificationDuration;
            Game1.addHUDMessage(message);
            _lastLocationMessage.Value = message;

            LogDebug($"Displayed location: {text}");
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            ClearLastMessage();
            _lastLocationMessage.ResetAllScreens();
            _lastLocationKey.ResetAllScreens();
            _pendingLocation.ResetAllScreens();

            // Clear caches so we don't hold stale data
            _nameService.ResetCache();
        }

        private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
        {
            // Another mod changed the warp list, so read it again next time
            if (e.NamesWithoutLocale.Any(name => name.IsEquivalentTo(CheatsMenuWarpNames.AssetName)))
                _warpNames.Reset();
        }

        private void OnLocaleChanged(object? sender, LocaleChangedEventArgs e)
        {
            // Names collected so far belong to the old language
            _nameService.ResetCache();
            _warpNames.Reset();
        }

        private void ClearLastMessage()
        {
            HUDMessage? message = _lastLocationMessage.Value;
            if (message == null)
                return;

            Game1.hudMessages.Remove(message);
            _lastLocationMessage.Value = null;
        }

        private void OnDebugLocationCommand(string command, string[] args)
        {
            GameLocation? location = Game1.player?.currentLocation ?? Game1.currentLocation;
            if (location == null)
            {
                Monitor.Log("No current location available.", LogLevel.Info);
                return;
            }

            string name = location.Name ?? "null";
            string uniqueName = location.NameOrUniqueName ?? "null";
            string displayName = location.GetDisplayName() ?? "null";
            string building = location.ParentBuilding?.buildingType.Value ?? "none";
            string resolved = _nameService.GetLocationName(location) ?? "(hidden)";

            Monitor.Log(
                $"Location Debug: Name='{name}', UniqueName='{uniqueName}', DisplayName='{displayName}', Building='{building}', Temporary={location.IsTemporary}, Resolved='{resolved}'",
                LogLevel.Info
            );
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
