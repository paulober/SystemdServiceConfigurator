using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using SystemdServiceConfigurator.Helpers;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.Windows.ApplicationModel.WindowsAppRuntime;
using Microsoft.Windows.AppLifecycle;
using AppInstance = Microsoft.Windows.AppLifecycle.AppInstance;
using static System.Net.WebRequestMethods;
using Microsoft.UI.Dispatching;

namespace SystemdServiceConfigurator
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            // Hook up the Activated event, to allow for this instance of the app
            // getting reactivated as a result of multi-instance redirection.
            AppInstance.GetCurrent().Activated += OnActivated;

            // Windows App runtime setup
            if (DeploymentManager.GetStatus().Status != DeploymentStatus.Ok
                || SystemInformation.Instance.IsFirstRun
                || SystemInformation.Instance.IsAppUpdated)
            {
                Debug.WriteLine($"[App]: DeploymentManager status: {DeploymentManager.GetStatus().Status}");

                var initializeTask = Task.Run(DeploymentManager.Initialize);
                initializeTask.Wait();

                if (initializeTask.Result.Status == DeploymentStatus.Ok)
                {
                    Debug.WriteLine("[App]: DeploymentManager - Installed Windows App Runtime successfully!");
                }
            }
            
            this.InitializeComponent();
        }

        private void OnActivated(object? sender, AppActivationArguments e)
        {
            // use e and not AppInstance.GetCurrent().GetActivatedEventArgs() to get updated (redirected) args of other instance
            if (e.Kind == ExtendedActivationKind.File)
            {
                if (e.Data is IFileActivatedEventArgs fileArgs)
                {
                    //fileArgs.Files
                    try
                    {
                        foreach (var file in fileArgs.Files)
                        {
                            _mWindow?.EditExistingFile((StorageFile)file);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("OnActivated - open 'edit existing files' crashed: " + ex.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // TODO: check if super call is required and do before or after other stuff?
            base.OnLaunched(args);

            var activatedEventArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

            IReadOnlyList<IStorageItem>? files = null;
            if (activatedEventArgs.Kind == ExtendedActivationKind.File)
            {
                if (activatedEventArgs.Data is IFileActivatedEventArgs fileArgs)
                {
                    files = fileArgs.Files;
                }
            }
            
            if (args.UWPLaunchActivatedEventArgs.PreviousExecutionState == ApplicationExecutionState.Terminated)
            {
                // TODO: show you app crashed, maybe get some infos and submit them
            }

            // TODO: maybe get name to fill into default author in settings
            // args.UWPLaunchActivatedEventArgs.ServiceUser.

            // check settings
            SettingsHelper.SetupSettings();

            _mWindow = files != null ? new MainWindow(files) : new MainWindow();
            _mWindow.Activate();
        }

        private MainWindow? _mWindow;

        public void InitializeWithMainWindow(object target) => _mWindow?.InitializeWith(target);
    }
}
