namespace SimpleLocationDisplay
{
    public class ModConfig
    {
        public bool EnableMod { get; set; } = true;
        public float NotificationDuration { get; set; } = 3500f;
        public bool EnableDebugLogging { get; set; }
        public bool ShowZeroAsL0 { get; set; }
        public bool ShowOnSaveLoad { get; set; } = true;

        /// <summary>Locations that never get a popup. Each entry is an internal location name or the text the popup would show.</summary>
        public List<string> HiddenLocations { get; set; } = new();

        /// <summary>Keeps hand-edited values inside the range the config menu allows and tidies the hidden list.</summary>
        public void Clamp()
        {
            if (NotificationDuration < 1000f)
                NotificationDuration = 1000f;
            else if (NotificationDuration > 10000f)
                NotificationDuration = 10000f;

            // A hand-edited file can hold null, blanks or padding
            HiddenLocations = (HiddenLocations ?? new List<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Whether a location name (internal or as shown) is on the hidden list. Ignores case.</summary>
        public bool IsHidden(string? name)
        {
            if (string.IsNullOrWhiteSpace(name) || HiddenLocations.Count == 0)
                return false;

            string trimmed = name.Trim();
            foreach (string hidden in HiddenLocations)
            {
                if (string.Equals(hidden, trimmed, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>Turn the comma-separated text from the config menu into the hidden list.</summary>
        public void SetHiddenLocations(string? commaSeparated)
        {
            HiddenLocations = (commaSeparated ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            Clamp();
        }

        /// <summary>The hidden list as the comma-separated text shown in the config menu.</summary>
        public string GetHiddenLocationsText()
        {
            return string.Join(", ", HiddenLocations);
        }
    }
}
