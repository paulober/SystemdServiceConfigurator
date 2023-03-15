using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SystemdServiceConfigurator.Helpers;
using SystemdServiceConfigurator.Models;
using Windows.Foundation.Metadata;
using WinRT.Interop;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SystemdServiceConfigurator.ViewModels;

public class SystemdServiceViewModel : ObservableRecipient
{
    private StorageFile? _sourceFile;
    private ServiceFile _serviceFile;

    public string? GetFileName() => _sourceFile?.DisplayName;

    public ServiceFile ServiceData
    {
        get => _serviceFile;
        set => SetProperty(ref _serviceFile, value);
    }

    public string ServiceName
    {
        get => (GetFileName() ?? "New") + " Service";
    }

    /*
    public List<KeyValuePair<string, string>> EnvironmentVars
    {
        get
        {
            if (_serviceFile == null || _serviceFile.ServiceEnvironmentVars == null)
            {
                return new List<KeyValuePair<string, string>>();
            }

            return _serviceFile.ServiceEnvironmentVars.ToList<KeyValuePair<string, string>>();
        }

        set
        {
            if (_serviceFile == null)
            {
                return;
            }

            _serviceFile.ServiceEnvironmentVars ??= new Dictionary<string, string>();

            foreach (var val in value)
            {
                if (_serviceFile.ServiceEnvironmentVars.ContainsKey(val.Key))
                {
                    _serviceFile.ServiceEnvironmentVars[val.Key] = val.Value;
                }
                else
                {
                    _serviceFile.ServiceEnvironmentVars.Add(val.Key, val.Value);
                }
            }
        }
    }*/

    public SystemdServiceViewModel(StorageFile? storageFile = null)
    {
        if (storageFile != null)
        {
            var task = Task.Run(() => ServiceFile.ParseAsync(storageFile));
            task.Wait();
            if (task.IsCompleted)
            {
                _sourceFile = storageFile;
                _serviceFile = task.Result;
                return;
            }
        }
        
        _serviceFile = new ServiceFile();
    }

    public async Task<bool> SaveYourSelfAsync(StorageFile dest)
    {
        return await _serviceFile.SaveAsAsync(dest);
    }

    private async Task<StorageFile?> SaveYourSelfWithPickerAsync()
    {
        FileSavePicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            FileTypeChoices =
                {
                    {"Systemd unit service configuration", new List<string>
                    {
                        ".service"
                    }}
                },
            DefaultFileExtension = ".service",
            // ReSharper disable once StringLiteralTypo
            SuggestedFileName = "myservice"
        };

        ((App)Application.Current).InitializeWithMainWindow(picker);

        try
        {
            // !IMPORTANT! UI Thread area => blocking UI
            var hiImBlockingTheHoleBusiness = picker.PickSaveFileAsync();
            hiImBlockingTheHoleBusiness.AsTask().Wait();

            return hiImBlockingTheHoleBusiness.GetResults();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Creation of FileSavePicker failed: {ex.Message}");
        }
        return null;
    }

    public async Task<bool> SaveYourSelfAsync(DispatcherQueue queue)
    {
        StorageFile? dest = null;
        
        if (!queue.HasThreadAccess)
        {
            queue.TryEnqueue(() =>
            {
                var task = Task.Run(SaveYourSelfWithPickerAsync);
                task.Wait();
                dest = task.Result;
            });
        }
        else
        {
            dest = await SaveYourSelfWithPickerAsync();
        }

        // TODO: maybe handle result with feedback to user
        if (dest != null)
        {
            return await this.SaveYourSelfAsync(dest);
        }
        
        await Task.CompletedTask;
        return false;
    }

    public async Task<bool> SaveInDefaultAsync(string name, bool overwrite = false)
    {
        var fat = SettingsHelper.GetDefaultFolderFutureAccessToken();
        var destFolder = fat != string.Empty 
            ? await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(fat) 
            : ApplicationData.Current.LocalCacheFolder;

        var destFile = await destFolder.CreateFileAsync(name,
            overwrite ? CreationCollisionOption.ReplaceExisting : CreationCollisionOption.GenerateUniqueName);

        // TODO: maybe handle result with feedback to user
        var result = await this.SaveYourSelfAsync(destFile);
        
        await Task.CompletedTask;
        return result;
    }

    /// <summary>
    /// Don't use as it generates random file in cache. Not good practice.
    /// </summary>
    /// <returns></returns>
    [Deprecated("Use SaveToSourceOrAskUserAsync instead!", DeprecationType.Remove, 1, Platform.Windows)]
    public async Task<bool> SaveToSourceOrDefaultAsync()
    {
        if (_sourceFile != null)
        {
            return await SaveYourSelfAsync(_sourceFile);
        }
        
        return await SaveInDefaultAsync("MyService.service", false);
    }

    public async Task<bool> SaveToSourceOrAskUserAsync(DispatcherQueue queue)
    {
        if (_sourceFile != null)
        {
            return await SaveYourSelfAsync(_sourceFile);
        }

        return await SaveYourSelfAsync(queue);
    }
}