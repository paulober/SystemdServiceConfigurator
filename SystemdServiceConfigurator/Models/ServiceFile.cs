using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace SystemdServiceConfigurator.Models;

internal static class StringExtensions
{
    public static int SafeIndexOf(this string line, char c)
    {
        var idx = line.IndexOf(c);
        return idx == -1 ? 0 : idx;
    }
}

public class ServiceFile
{
    private static readonly string[] SectionNames = {
        "[Unit]",
        "[Service]",
        "[Install]"
    };
    
    private static readonly Dictionary<string, string> Names = new()
    {
        {nameof(UnitDescription), "Description"},
        {nameof(UnitWants), "Wants"},
        {nameof(UnitAfter), "After"},
        
        {nameof(ServiceEnvironmentVars), "Environment"},
        {nameof(ServiceType), "Type"},
        {nameof(ServiceExecStart), "ExecStart"},
        {nameof(ServiceExecStop), "ExecStop"},
        {nameof(ServiceRestart), "Restart"},
        {nameof(ServiceUser), "User"},
        {nameof(ServiceGroup), "Group"},
        
        {nameof(InstallWantedBy), "WantedBy"}
    };

    // Unit
    public string UnitDescription { get; set; }
    public string? UnitWants { get; set; }
    public string? UnitAfter { get; set; }

    // Service
    public ObservableCollection<KeyValuePair<string, string>>? ServiceEnvironmentVars { get; set; }

    public string? ServiceType { get; set; }
    public string? ServiceExecStart { get; set; }
    public string? ServiceExecStop { get; set; }
    public string? ServiceRestart { get; set; }
    public string? ServiceUser { get; set; }
    public string? ServiceGroup { get; set; }
    
    // Install
    public string? InstallWantedBy { get; set; }

    public ServiceFile(string description = "")
    {
        UnitDescription = description;
        ServiceEnvironmentVars = new ObservableCollection<KeyValuePair<string, string>>();
    }

    public static async Task<ServiceFile> ParseAsync(StorageFile storageFile)
    {
        var serviceFile = new ServiceFile();
        var lines = await FileIO.ReadLinesAsync(storageFile);
        var inUnitSection = false;
        var inServiceSection = false;
        var inInstallSection = false;

        foreach (var line in lines)
        {
            if (line.StartsWith(SectionNames[0]))
            {
                inServiceSection = false;
                inInstallSection = false;

                inUnitSection = true;
                continue;
            }
            else if (line.StartsWith(SectionNames[1]))
            {
                inUnitSection = false;
                inInstallSection = false;

                inServiceSection = true;
                continue;
            }
            else if (line.StartsWith(SectionNames[2]))
            {
                inUnitSection = false;
                inServiceSection = false;

                inInstallSection = true;
                continue;
            }
            else if (line.StartsWith("["))
            {
                inUnitSection = false;
                inServiceSection = false;
                inInstallSection = false;
                continue;
            }

            if (inUnitSection)
            {
                // TODO: save .lengths hardcoded to save compute time
                if (line.StartsWith(Names[nameof(UnitDescription)] + "="))
                {
                    // Substring as alternative
                    serviceFile.UnitDescription = line[(Names[nameof(UnitDescription)] + "=").Length..];
                }
                else if (line.StartsWith(Names[nameof(UnitWants)] + "="))
                {
                    serviceFile.UnitWants = line[(Names[nameof(UnitWants)] + "=").Length..];
                }
                else if (line.StartsWith(Names[nameof(UnitAfter)] + "="))
                {
                    serviceFile.UnitAfter = line[(Names[nameof(UnitAfter)] + "=").Length..];
                }
            }
            else if (inServiceSection)
            {
                /* over 10 times slower than the switch
                if (line.StartsWith("Environment="))
                {
                    var content = line["Environment=".Length..].Split('=');
                    if (content.Length == 2)
                    {
                        serviceFile.ServiceEnvironmentVars ??= new Dictionary<string, string>();

                        serviceFile.ServiceEnvironmentVars?.Add(content.First(), content[1]);
                    }
                    else
                    {
                        Debug.WriteLine("Bad environment entry!");
                    }
                }
                else if (line.StartsWith("ServiceType="))
                {
                    serviceFile.ServiceType = line["ServiceType=".Length..];
                }
                else if (line.StartsWith("ServiceExecStart="))
                {
                    serviceFile.ServiceExecStart = line["ServiceExecStart=".Length..];
                }*/

                // if using line.Split('=').First() would be much slower than if-else-else-if blocks
                switch (line[..line.SafeIndexOf('=')])
                {
                    case "Environment":
                        var content = line[(Names[nameof(ServiceEnvironmentVars)]+"=").Length..].Split('=');
                        if (content.Length == 2)
                        {
                            serviceFile.ServiceEnvironmentVars ??= new ObservableCollection<KeyValuePair<string, string>>();
                            serviceFile.ServiceEnvironmentVars?.Add(new KeyValuePair<string, string>(content.First(), content[1]));
                        }
                        else
                        {
                            Debug.WriteLine("Bad environment entry!");
                        }
                        break;
                    case "Type":
                        serviceFile.ServiceType = line[(Names[nameof(ServiceType)]+"=").Length..];
                        break;
                    case "ExecStart":
                        serviceFile.ServiceExecStart = line[(Names[nameof(ServiceExecStart)]+"=").Length..];
                        break;
                    case "ExecStop":
                        serviceFile.ServiceExecStop = line[(Names[nameof(ServiceExecStop)]+"=").Length..];
                        break;
                    case "Restart":
                        serviceFile.ServiceRestart = line[(Names[nameof(ServiceRestart)]+"=").Length..];
                        break;
                    case "User":
                        serviceFile.ServiceUser = line[(Names[nameof(ServiceUser)]+"=").Length..];
                        break;
                    case "Group":
                        serviceFile.ServiceGroup = line[(Names[nameof(ServiceGroup)]+"=").Length..];
                        break;
                }
            }
            else if (inInstallSection)
            {
                if (line.StartsWith(Names[nameof(InstallWantedBy)]+"="))
                {
                    serviceFile.InstallWantedBy = line[(Names[nameof(InstallWantedBy)]+"=").Length..];
                }
            }
        }

        return serviceFile;
    }

    private void AddIfNotNull(ref List<string> output, string fieldName)
    {
        var field = GetType().GetProperty(fieldName)?.GetValue(this);
        
        switch (field)
        {
            case null:
                return;
            case string val:
                output.Add(Names[fieldName] + "=" + val);
                break;
            case Dictionary<string, string> dict:
                output.AddRange(dict.Select(entry => Names[fieldName] + "=" + entry.Key + "=" + entry.Value));
                break;
        }
    }

    private void AddIfNotNullRange(ref List<string> output, params string[] fields)
    {
        foreach (var field in fields)
        {
            AddIfNotNull(ref output, field);
        }
    }

    public string[] Export()
    {
        List<string> output = new ()
        {
            SectionNames[0], // unit
            Names[nameof(UnitDescription)] + "=" + UnitDescription
        };

        AddIfNotNullRange(ref output, nameof(UnitAfter), nameof(UnitWants));

        // for readability
        output.Add(string.Empty);

        output.Add(SectionNames[1]); // service
        
        AddIfNotNullRange(ref output, 
            nameof(ServiceEnvironmentVars), 
            nameof(ServiceType), 
            nameof(ServiceExecStart), 
            nameof(ServiceExecStop),
            nameof(ServiceRestart),
            nameof(ServiceUser),
            nameof(ServiceGroup));

        // for readability
        output.Add(string.Empty);

        output.Add(SectionNames[2]);
        
        AddIfNotNull(ref output, nameof(InstallWantedBy));
        
        // add trailing empty string to have an empty line at the end of the file
        output.Add(string.Empty);

        return output.ToArray();
    }
}
