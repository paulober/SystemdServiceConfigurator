using Windows.Storage;

namespace SystemdServiceConfigurator.Helpers
{
    internal static class SettingsHelper
    {
        private static readonly string SetupSettingsKey = "IsSetup";
        private static readonly string AutoSaveSettingsKey = "AutoSave";
        private static readonly string DefaultFolderFutureAccessTokenSettingsKey = "DefaultFolderFutureAccessToken";

        private static ApplicationDataContainer LocalSettings => ApplicationData.Current.LocalSettings;

        private static bool IsSetup() => LocalSettings.Values.ContainsKey(SetupSettingsKey);

        internal static bool IsAutoSaveEnabled() => LocalSettings.Values[AutoSaveSettingsKey].Equals(true);

        internal static string GetDefaultFolderFutureAccessToken() 
            => LocalSettings.Values.TryGetValue(DefaultFolderFutureAccessTokenSettingsKey, out var value) 
                ? (string)value : string.Empty;

        internal static void SetDefaultFolderFutureAccessToken(string token) 
            => LocalSettings.Values[DefaultFolderFutureAccessTokenSettingsKey] = token;

        /// <summary>
        /// Reset all local settings and set them to their defaults.
        /// </summary>
        private static void ResetToDefaultSettings()
        {
            // reset settings
            LocalSettings.Values.Clear();

            // set defaults
            LocalSettings.Values[AutoSaveSettingsKey] = true;

            // set setup settings key
            LocalSettings.Values[SetupSettingsKey] = true;
        }

        /// <summary>
        /// Setup default settings if settings don't already exists locally.
        /// </summary>
        internal static void SetupSettings()
        {
            if (!IsSetup())
            {
                ResetToDefaultSettings();
            }
        }
    }
}
