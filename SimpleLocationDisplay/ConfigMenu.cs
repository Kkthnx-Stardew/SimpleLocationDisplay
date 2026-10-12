using StardewModdingAPI;

namespace SimpleLocationDisplay
{
    public static class ConfigMenu
    {
        public static void SetupConfigUI(ModEntry modEntry, IModHelper helper, ModConfig config)
        {
            var configMenuApi = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenuApi == null)
            {
                modEntry.Monitor.Log("Generic Mod Config Menu not found. Edit config.json or install GMCM for in-game settings.", LogLevel.Trace);
                return;
            }

            configMenuApi.Register(
                mod: modEntry.ModManifest,
                reset: () =>
                {
                    config.EnableMod = true;
                    config.NotificationDuration = 3500f;
                    config.EnableDebugLogging = false;
                    config.ShowZeroAsL0 = false;
                    config.ShowOnSaveLoad = true;
                    config.HiddenLocations = new List<string>();
                },
                save: () =>
                {
                    config.Clamp();
                    helper.WriteConfig(config);
                },
                titleScreenOnly: false
            );

            configMenuApi.AddSectionTitle(
                mod: modEntry.ModManifest,
                text: () => I18n.Gmcm_Title(),
                tooltip: () => I18n.Gmcm_Description()
            );

            configMenuApi.AddBoolOption(
                mod: modEntry.ModManifest,
                name: () => I18n.Gmcm_Enable_Name(),
                tooltip: () => I18n.Gmcm_Enable_Tooltip(),
                getValue: () => config.EnableMod,
                setValue: value => config.EnableMod = value
            );

            configMenuApi.AddNumberOption(
                mod: modEntry.ModManifest,
                getValue: () => config.NotificationDuration,
                setValue: value => config.NotificationDuration = value,
                name: () => I18n.Gmcm_Duration_Name(),
                tooltip: () => I18n.Gmcm_Duration_Tooltip(),
                min: 1000f,
                max: 10000f,
                interval: 500f,
                formatValue: milliseconds => $"{milliseconds / 1000f:0.0} s"
            );

            configMenuApi.AddBoolOption(
                mod: modEntry.ModManifest,
                name: () => I18n.Gmcm_Saveload_Name(),
                tooltip: () => I18n.Gmcm_Saveload_Tooltip(),
                getValue: () => config.ShowOnSaveLoad,
                setValue: value => config.ShowOnSaveLoad = value
            );

            configMenuApi.AddTextOption(
                mod: modEntry.ModManifest,
                name: () => I18n.Gmcm_Hidden_Name(),
                tooltip: () => I18n.Gmcm_Hidden_Tooltip(),
                getValue: config.GetHiddenLocationsText,
                setValue: config.SetHiddenLocations
            );

            configMenuApi.AddBoolOption(
                mod: modEntry.ModManifest,
                name: () => I18n.Gmcm_Debug_Name(),
                tooltip: () => I18n.Gmcm_Debug_Tooltip(),
                getValue: () => config.EnableDebugLogging,
                setValue: value => config.EnableDebugLogging = value
            );

            configMenuApi.AddBoolOption(
                mod: modEntry.ModManifest,
                name: () => I18n.Gmcm_Zero_Name(),
                tooltip: () => I18n.Gmcm_Zero_Tooltip(),
                getValue: () => config.ShowZeroAsL0,
                setValue: value => config.ShowZeroAsL0 = value
            );
        }
    }
}
