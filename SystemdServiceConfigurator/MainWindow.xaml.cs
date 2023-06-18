using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using SystemdServiceConfigurator.Helpers;
using SystemdServiceConfigurator.Pages;
using SystemdServiceConfigurator.ViewModels;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace SystemdServiceConfigurator
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        // or Symbol.Document for one of them
        private const Symbol UnsavedFileIcon = Symbol.OpenFile;
        private const Symbol SavedFileIcon = Symbol.Save;

        private nint _hWnd => WindowNative.GetWindowHandle(this);
        private readonly AppWindow _appWindow;

        public void InitializeWith(object target)
        {
            InitializeWithWindow.Initialize(target, _hWnd);
        }

        public ObservableCollection<TabViewItem> Tabs { get; } = new();
        private IReadOnlyList<IStorageItem>? _activationFiles;

        public MainWindow(IReadOnlyList<IStorageItem>? activationFiles = null)
        {
            this._activationFiles = activationFiles;
            this.InitializeComponent();

            _appWindow = GetAppWindowForCurrentWindow();
            if (_appWindow.ClientSize.Width > 1400)
            {
                _appWindow.ResizeClient(new SizeInt32(1400, _appWindow.ClientSize.Height));
            }
            if (_appWindow.ClientSize.Height > 800)
            {
                _appWindow.ResizeClient(new SizeInt32(_appWindow.ClientSize.Width, 800));
            }

            this.SystemBackdrop = new MicaBackdrop();

            // Check to see if customization is supported.
            // Currently only supported on Windows 11.
            if (AppWindowTitleBar.IsCustomizationSupported())
            {                
                CustomizeTitleBar();
            }

            // load start page aka home tab
            var homeTab = new TabViewItem
            {
                IsClosable = false,
                IconSource = new SymbolIconSource { Symbol = Symbol.Home },
                Header = "Home",
                IsSelected = true,
                IsHoldingEnabled = false,
                CanDrag = false
            };

            Frame homeFrame = new()
            {
                Padding = new Thickness(15, 10, 15, 10)
            };
            homeTab.Content = homeFrame;
            homeFrame.Navigate(typeof(HomePage));

            Tabs.Add(homeTab);
        }

        #region System configuration

        private void CustomizeTitleBar()
        {
            var titleBar = _appWindow.TitleBar;

            // does cause overlapping problems with the buttons by TabView
            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.PreferredHeightOption = TitleBarHeightOption.Standard;

            //SetTitleBar(CustomDragRegion);
            //CustomDragRegion.MinWidth = 188;
            // CustomDragRegion.MinWidth = 188 + 20;

            AppTitle.Text = Package.Current.DisplayName;

            SetTitleBarColors(titleBar);
        }

        private void SetTitleBarColors(AppWindowTitleBar titleBar)
        {
            // set active window colors
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonPressedBackgroundColor = Colors.Transparent;

            // set inactive window colors
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(_hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        private WindowsSystemDispatcherQueueHelper? _mWsdqHelper; // See separate sample below for implementation
        private SystemBackdropConfiguration? _mConfigurationSource;

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (_mConfigurationSource != null)
                _mConfigurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            this.Activated -= Window_Activated;
            _mConfigurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (_mConfigurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }

        private void SetConfigurationSourceTheme()
        {
            if (_mConfigurationSource == null)
                return;

            _mConfigurationSource.Theme = ((FrameworkElement)Content).ActualTheme switch
            {
                ElementTheme.Dark => SystemBackdropTheme.Dark,
                ElementTheme.Light => SystemBackdropTheme.Light,
                ElementTheme.Default => SystemBackdropTheme.Default,
                _ => _mConfigurationSource.Theme
            };
        }

#endregion

        private async void TabView_AddTabButtonClick(TabView? sender, object args)
        {
            // Tabs.Add(new TabViewItemData());
            // show ContentDialog for open file or create new
            ContentDialog dialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = "Starting new tab",
                PrimaryButtonText = "Create new",
                SecondaryButtonText = "Open existing",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = "Do you want to create a new systemd service or edit an existing file?"
            };

            var result = await dialog.ShowAsync();

            switch (result)
            {
                // create new file
                case ContentDialogResult.Primary:
                    var newTab = new TabViewItem
                    {
                        IconSource = new SymbolIconSource { Symbol = UnsavedFileIcon },
                        Header = "New Service",
                        IsSelected = true,
                        DataContext = new SystemdServiceViewModel()
                    };

                    Frame frame = new();
                    newTab.Content = frame;
                    frame.Navigate(typeof(SystemdEditPage), null, new EntranceNavigationTransitionInfo());

                    Tabs.Add(newTab);

                    break;

                // edit existing
                case ContentDialogResult.Secondary:
                    // open local file dialog
                    FileOpenPicker picker = new();
                    // Initialize the file picker with the current window handle (m_hWnd)
                    InitializeWithWindow.Initialize(picker, _hWnd);

                    // configure file-picker
                    picker.ViewMode = PickerViewMode.List;
                    picker.SuggestedStartLocation = PickerLocationId.Downloads;
                    // TODO: consider adding support for other systemd files like .target
                    picker.FileTypeFilter.Add(".service");

                    var file = await picker.PickSingleFileAsync();
                    if (file != null)
                    {
                        EditExistingFile(file);
                    }
                    else
                    {
                        // ugly Windows 10 style
                        /*MessageDialog errDialog = new("No file was selected");
                        InitializeWithWindow.Initialize(errDialog, m_hWnd);
                        await errDialog.ShowAsync();*/
                        WarningTeachingTip.Subtitle = "No file was selected. Try again...";
                        WarningTeachingTip.XamlRoot = this.Content.XamlRoot;
                        WarningTeachingTip.IsOpen = true;
                    }

                    break;

                case ContentDialogResult.None:
                default:
                    Debug.WriteLine("Canceled <NewTab> operation");
                    return;
            }

            // adjust drag region as new tabs don't trigger SizeChanged on TabView
            CalculateCustomDragRegion();
        }

        private bool _ctrlNCurrentlyInvoked = false;

        private void CtrlN_OnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (_ctrlNCurrentlyInvoked) return;
            _ctrlNCurrentlyInvoked = true;

            if (!DispatcherQueue.HasThreadAccess)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    TabView_AddTabButtonClick(null, args);

                    _ctrlNCurrentlyInvoked = false;
                });
            }
            else
            {
                TabView_AddTabButtonClick(null, args);
                _ctrlNCurrentlyInvoked = false;
            }
        }

        /// <summary>
        /// Don't call directly from other than EditExistingFile!
        /// </summary>
        /// <param name="file"></param>
        /// <param name="noAutoNavigation"></param>
        private void OpenNewTabForExistingFile(StorageFile file, bool noAutoNavigation)
        {
            var newTab = new TabViewItem
            {
                IconSource = new SymbolIconSource { Symbol = SavedFileIcon },
                Header = file.DisplayName,
                IsSelected = true,
                DataContext = new SystemdServiceViewModel(file)
            };

            Frame frame = new();
            newTab.Content = frame;
            frame.Navigate(typeof(SystemdEditPage), file, new EntranceNavigationTransitionInfo());

            Tabs.Add(newTab);

            // TODO: navigate to home page
            if (noAutoNavigation)
            {
            }
        }

        /// <summary>
        /// Thread management for OpenNewTabForExistingFile(...).
        /// </summary>
        /// <param name="file"></param>
        /// <param name="noAutoNavigation"></param>
        public void EditExistingFile(StorageFile file, bool noAutoNavigation = false)
        {
            if (!DispatcherQueue.HasThreadAccess)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    OpenNewTabForExistingFile(file, noAutoNavigation);
                });
            }

            OpenNewTabForExistingFile(file, noAutoNavigation);
        }

        private async void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            if (((SymbolIconSource)args.Tab.IconSource).Symbol == UnsavedFileIcon)
            {
                ContentDialog dialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                    Title = "Unsaved changes",
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Discard changes",
                    DefaultButton = ContentDialogButton.Primary,
                    Content = "The current tab contains unsaved changes. Do you want to save them?"
                };
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    // save changes
                    if (args.Item is SystemdServiceViewModel vm)
                    {
                        if (await vm.SaveYourSelfAsync(DispatcherQueue))
                        {
                            WarningTeachingTip.Subtitle = "File saved successfully";
                            WarningTeachingTip.XamlRoot = this.Content.XamlRoot;
                            WarningTeachingTip.IsOpen = true;
                        }
                    } 
                }
            }

            // close tab
            Tabs.Remove(args.Tab);
        }

        private void TabView_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_activationFiles == null)
            {
                return;
            }
            
            bool noAutoNavigation = _activationFiles.Count > 1;
            foreach (var file in _activationFiles)
            {
                try
                {
                    // avoid navigating to opened file if more than one files were transmitted
                    EditExistingFile((StorageFile)file, noAutoNavigation);
                }
                catch (InvalidCastException ex)
                {
                    Debug.WriteLine("InvalidCastException: " + ex.Message);
                }
            }
            
            // free memory (remove references so GC can remove objects)
            _activationFiles = null;
        }

        private void CalculateCustomDragRegion()
        {
            return;
            /*var transform = CustomDragRegion.TransformToVisual(Content);
            Point relativePosition = transform.TransformPoint(new Point(0, 0));
            var rect = new RectInt32((int)relativePosition.X, (int)relativePosition.Y,
                (int)CustomDragRegion.ActualWidth, (int)CustomDragRegion.ActualHeight);
            _appWindow.TitleBar.SetDragRectangles(new[] { rect });*/
            var transform = CustomDragRegion.TransformToVisual(Content);


            Point topLeft = transform.TransformPoint(new Point(0, 0));
            Point bottomRight = transform.TransformPoint(new Point(CustomDragRegion.ActualWidth, CustomDragRegion.ActualHeight));

            // SizeToContent int x = (int)Math.Round(topLeft.X) + 115;
            int x = (int)Math.Round(((FrameworkElement)MainTabView.TabStripFooter).ActualWidth);
            //int x = _appWindow.Size.Width - (int)Math.Round(CustomDragRegion.ActualWidth);
            int y = (int)Math.Round(topLeft.Y);
            //int width = (int)Math.Round(bottomRight.X - topLeft.X);
            int width = _appWindow.Size.Width - x;
            int height = (int)Math.Round(bottomRight.Y - topLeft.Y);

            // (int)Math.Round(CustomDragRegion.ActualWidth)+100
            _appWindow.TitleBar.SetDragRectangles(new[] { new RectInt32(x, y, width, height) });
        }

        private void Window_SizeChanged(object sender, WindowSizeChangedEventArgs args) => CalculateCustomDragRegion();

        private void MainTabView_TabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args) => CalculateCustomDragRegion();

        private void MainTabView_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }
    }
}
