using System.Diagnostics;

namespace GPSrvtTabWrapper;

using System.Reflection;
using Autodesk.Revit.UI;

internal class App : IExternalApplication
{
    private const string RevitAppClass = "GPSrvtTab.GpsTab";
    private const string DefaultVersion = "R25";
    private const string DllName = "GPSrvtTabVersion";

    private IExternalApplication? _dllInstance;

    public Result OnStartup(UIControlledApplication application)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = DllVersion(application);
        
        var dllName = $"{DllName}{version}";
        var tempFolderPath = Path.GetTempPath();
        var dllOutPath = Path.Combine(tempFolderPath, dllName + ".dll");

        string exePath;
        
        try
        {
            exePath = ExtractExe(tempFolderPath, "zstdWrapperGO.exe");
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Extraction Error", ex.ToString());
            return Result.Failed;
        }

        try
        {
            DecompressDll(assembly, exePath, dllOutPath, dllName);
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Decompression Error", ex.ToString());
            return Result.Failed;
        }
        finally
        {
            if (File.Exists(exePath))
            {
                File.Delete(exePath);
            }
        }

        if (!File.Exists(dllOutPath))
        {
            TaskDialog.Show("Error", $"DLL not found at {dllOutPath}");
            return Result.Failed;
        }
        
        _dllInstance = Assembly.LoadFrom(dllOutPath)
            .CreateInstance(RevitAppClass) as IExternalApplication;

        if (_dllInstance == null)
        {
            TaskDialog.Show("Error", "Failed to create instance of the add-in class.");
            return Result.Failed;
        }

        try
        {
            _dllInstance.OnStartup(application);
        }
        catch (Exception ex)
        {
            TaskDialog.Show("OnStartup Error", ex.ToString());
            return Result.Failed;
        }

        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        _dllInstance?.OnShutdown(application);
        return Result.Succeeded;
    }

    private string DllVersion(UIControlledApplication application)
    {
        return application.ControlledApplication.VersionNumber switch
        {
            "2014" => "R14",
            "2015" => "R15",
            "2016" => "R16",
            "2017" => "R17",
            "2018" => "R18",
            "2019" => "R19",
            "2020" => "R20",
            "2021" => "R21",
            "2022" => "R22",
            "2023" => "R23",
            "2024" => "R24",
            "2025" => "R25",
            _ => DefaultVersion
        };
    }
    
    private string ExtractExe(string tempFolderPath, string exeName)
    {
        var exePath = Path.Combine(tempFolderPath, exeName);
        
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(App).Namespace}.Resources.{exeName}");
        if (stream == null)
        {
            TaskDialog.Show("Error", $"Resource {exeName} not found.");
            throw new InvalidOperationException($"Resource {exeName} not found.");
        }
        
        using var fileStream = new FileStream(exePath, FileMode.Create, FileAccess.Write);
        stream.CopyTo(fileStream);
        
        return exePath;
    }
    
    private void DecompressDll(Assembly assembly, string zstdExePath, string dllOutPath, string dllName)
    {
        
        var dllZstdFilePath = dllOutPath + ".zst";
        
        using var stream = assembly.GetManifestResourceStream($"{typeof(App).Namespace}.Resources.{dllName}.zst"); 
        if (stream == null)
        {
            TaskDialog.Show("Error", $"Resource {dllName}.zst not found.");
            throw new InvalidOperationException($"Resource {dllName}.zst not found.");
        }
        
        using (var fileStream = new FileStream(dllZstdFilePath, FileMode.Create, FileAccess.Write)) {
            stream.CopyTo(fileStream);
        }
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                Arguments = $"-decompress -in=\"{dllZstdFilePath}\" -out=\"{dllOutPath}\"",
                FileName = zstdExePath,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        
        process.Start();
        process.WaitForExit();
    }
}