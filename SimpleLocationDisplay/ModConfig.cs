namespace SimpleLocationDisplay
{
    public class ModConfig
    {
        public bool EnableMod { get; set; } = true;
        public float NotificationDuration { get; set; } = 3500f;
        public bool EnableDebugLogging { get; set; } = false;
        public bool ShowZeroAsL0 { get; set; } = false;
        public bool ShowOnSaveLoad { get; set; } = true;

        /// <summary>Keeps hand-edited values inside the range the config menu allows.</summary>
        public void Clamp()
        {
            if (NotificationDuration < 1000f)
                NotificationDuration = 1000f;
            else if (NotificationDuration > 10000f)
                NotificationDuration = 10000f;
        }
    }
}
