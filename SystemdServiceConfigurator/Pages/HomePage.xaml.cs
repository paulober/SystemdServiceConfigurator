using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.AccessCache;
using SystemdServiceConfigurator.Helpers;
using System;
using Windows.Storage.Pickers;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using WinRT.Interop;
using CommunityToolkit.WinUI;
using System.Reflection;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel;

namespace SystemdServiceConfigurator.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        private static readonly string DefaultFolderToken = "DefaultSystemdServiceOutputLocation";
        
        private string DefaultFolderPath
        {
            get;
            set;
        } = "C:\\PathToDefaultFolder";

        private string _versionDescription;

        public string VersionDescription
        {
            get => _versionDescription;
            set => _versionDescription = value;
        }

        public HomePage()
        {
            this.Loaded += async (sender, args) => await LoadDefaultFolder();
            _versionDescription = GetVersionDescription();
            this.InitializeComponent();
        }

        private static string GetVersionDescription()
        {
            Version version;

            if (RuntimeHelper.IsMSIX)
            {
                var packageVersion = Package.Current.Id.Version;

                version = new Version(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
            }
            else
            {
                version = Assembly.GetExecutingAssembly().GetName().Version!;
            }

            return $"{"AppDisplayName".GetLocalized()} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision} - by paulober";
        }

        private async Task LoadDefaultFolder()
        {
            var token = SettingsHelper.GetDefaultFolderFutureAccessToken();

            if (token == string.Empty)
            {
                DefaultFolderPath = ApplicationData.Current.LocalCacheFolder.Path;
            }
            else
            {
                var folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);
            }

            await Task.CompletedTask;
        }

        private async void ChangeDefaultFolder_OnClick(object sender, RoutedEventArgs e)
        {
            FolderPicker picker = new()
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                StorageApplicationPermissions.FutureAccessList.AddOrReplace(DefaultFolderToken, folder);
                SettingsHelper.SetDefaultFolderFutureAccessToken(DefaultFolderToken);

                DefaultFolderPath = folder.Path;
            }
        }

        private void ShortenContentFromTitleBarButton_Click(object sender, RoutedEventArgs e)
        {
            var tb = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(WindowNative.GetWindowHandle(this))).TitleBar;
            tb.ExtendsContentIntoTitleBar = !tb.ExtendsContentIntoTitleBar;
        }
    }
}
