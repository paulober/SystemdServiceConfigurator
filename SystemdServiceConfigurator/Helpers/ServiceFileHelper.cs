using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using SystemdServiceConfigurator.Models;

namespace SystemdServiceConfigurator.Helpers
{
    internal static class ServiceFileHelper
    {
        internal static async Task<bool> SaveAsAsync(this ServiceFile service, StorageFile dest)
        {
            var data = service.Export();

            try
            {
                await FileIO.WriteLinesAsync(dest, data, UnicodeEncoding.Utf8);

                return true;
            }
            catch (IOException ex)
            {
                await Task.CompletedTask;
                return false;
            }
        }
    }
}
