using StardewModdingAPI;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace SimpleLocationDisplay
{
    /// <summary>
    /// Reads location names from the warp list of CJB Cheats Menu, when that mod is installed.
    /// </summary>
    /// <remarks>
    /// Mods that add locations often add a warp for them, each with a translated display name.
    /// The list is a Content Patcher-editable asset whose entry type is internal to CJB Cheats Menu,
    /// so the entries are read by property name instead of by type. If anything about that changes,
    /// the lookup turns itself off and the popup falls back to the next name source.
    /// </remarks>
    public class CheatsMenuWarpNames
    {
        public const string ModId = "CJBok.CheatsMenu";
        public const string AssetName = "Mods/CJBok.CheatsMenu/Warps";

        private readonly IGameContentHelper _content;
        private readonly IModRegistry _mods;
        private readonly IMonitor _monitor;

        private Dictionary<string, string>? _names;
        private bool _disabled;

        public CheatsMenuWarpNames(IGameContentHelper content, IModRegistry mods, IMonitor monitor)
        {
            _content = content;
            _mods = mods;
            _monitor = monitor;
        }

        /// <summary>Forget the loaded list, so it's read again the next time it's needed.</summary>
        public void Reset()
        {
            _names = null;
        }

        /// <summary>Get the display name of the warp that targets a location.</summary>
        /// <param name="locationName">The internal location name.</param>
        /// <returns>The name, or null if CJB Cheats Menu isn't installed or has no single clear name for the location.</returns>
        public string? GetName(string locationName)
        {
            if (_disabled || !_mods.IsLoaded(ModId))
                return null;

            _names ??= Load();
            return _names != null && _names.TryGetValue(locationName, out string? name)
                ? name
                : null;
        }

        private Dictionary<string, string>? Load()
        {
            try
            {
                if (_content.Load<object>(AssetName) is not IEnumerable warps)
                    throw new InvalidOperationException("The warp list isn't a list.");

                // Group the display names by target location. A location with several different
                // names is skipped, since those entries name spots inside it, not the location.
                var byLocation = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (object? warp in warps)
                {
                    if (warp == null)
                        continue;

                    string? location = ReadProperty(warp, "Location");
                    string? displayName = ReadProperty(warp, "DisplayName");
                    if (string.IsNullOrWhiteSpace(location) || string.IsNullOrWhiteSpace(displayName))
                        continue;

                    if (!byLocation.TryGetValue(location, out HashSet<string>? names))
                        byLocation[location] = names = new HashSet<string>(StringComparer.Ordinal);
                    names.Add(displayName.Trim());
                }

                var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach ((string location, HashSet<string> names) in byLocation)
                {
                    if (names.Count == 1)
                        result[location] = names.First();
                }

                return result;
            }
            catch (Exception ex)
            {
                _disabled = true;
                _monitor.Log($"Couldn't read the CJB Cheats Menu warp list, so its names won't be used. Details: {ex.Message}", LogLevel.Trace);
                return null;
            }
        }

        private static string? ReadProperty(object target, string name)
        {
            PropertyInfo? property = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            return property?.GetValue(target) as string;
        }
    }
}
