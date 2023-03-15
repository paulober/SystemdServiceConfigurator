using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using SystemdServiceConfigurator.Helpers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Printing.PrintSupport;
using Windows.Storage;
using Microsoft.UI;
using SystemdServiceConfigurator.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SystemdServiceConfigurator.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SystemdEditPage : Page
    {
        [Obsolete]
        private StorageFile? _file;

        private SystemdServiceViewModel GetContext() => (SystemdServiceViewModel)DataContext;

        public SystemdEditPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _file = (StorageFile)e.Parameter;

            base.OnNavigatedTo(e);
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            // save changes if autoSave setting is enabled
            if (_file != null && SettingsHelper.IsAutoSaveEnabled())
            {
                await FileIO.WriteTextAsync(_file, "Content", Windows.Storage.Streams.UnicodeEncoding.Utf8);
            }
            // if setting autocreate on autosave is enabled open save dialog (maybe; could be an anoying "feature")

            base.OnNavigatingFrom(e);
        }

        private void CancelBtn_OnClick(object sender, RoutedEventArgs e)
        {
            // TODO: implement it
            throw new NotImplementedException();
        }

        private async void SaveBtn_OnClick(object sender, RoutedEventArgs e)
        {
            if (await GetContext().SaveToSourceOrAskUserAsync(DispatcherQueue))
            {
                SaveStatusLabel.Text = "Saved ✔️";
                SaveStatusLabel.Foreground = new SolidColorBrush(Colors.Green);
            }
        }

        private void AddEnvironmentVarButton_Click(object sender, RoutedEventArgs e)
        {
            GetContext().ServiceData.ServiceEnvironmentVars ??= new System.Collections.ObjectModel.ObservableCollection<KeyValuePair<string, string>>();

            string varKey = "New Var 1";
            uint currentIdx = 1;

            while (GetContext().ServiceData.ServiceEnvironmentVars!.Any(kv => kv.Key == varKey))
            {
                currentIdx++;
                varKey = "New Var " + currentIdx.ToString();
            }

            GetContext().ServiceData.ServiceEnvironmentVars?.Add(new KeyValuePair<string, string>(varKey, "New Data"));

            //dataGrid.SelectedIndex = GetContext().ServiceData.ServiceEnvironmentVars?.Count - 1 ?? 0;
            dataGrid.ScrollIntoView(GetContext().ServiceData.ServiceEnvironmentVars?.Last(), dataGrid.Columns[0]);
        }

        private void DeleteEnvironmentVarMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // TODO: maybe add undo button in future
            if (sender is MenuFlyoutItem mfi)
            {
                if (mfi.Text == "Delete")
                {
                    // assume that the opening of the menuflyout selected the element
                    GetContext().ServiceData.ServiceEnvironmentVars?.RemoveAt(dataGrid.SelectedIndex);
                }
            }
        }
    }
}
