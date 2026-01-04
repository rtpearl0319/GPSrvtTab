using System.Diagnostics;
using System.Reflection;
using Autodesk.Revit.UI;

namespace GPSrvtTabWrapper;

public class CheckUpdateHandler : ICheckUpdate
{
    private bool shouldUpdateOnShutdown;
    
    
    public bool CheckForUpdate()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var tempFolderPath = Path.GetTempPath();

        string exePath = App.ExtractExe(tempFolderPath, "GPSrvtTabDLLMover.exe");

        using var stream = assembly.GetManifestResourceStream($"{typeof(App).Namespace}.Resources.GPSrvtTabDLLMover.exe");
        if (stream == null)
        {
            TaskDialog.Show("Error", $"Resource GPSrvtTabDLLMover not found.");
            throw new InvalidOperationException($"Resource GPSrvtTabDLLMover not found.");
        }

        using (var fileStream = new FileStream(exePath, FileMode.Create, FileAccess.Write))
        {
            stream.CopyTo(fileStream);
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                Arguments = "-repo=\"GPSrvtTab\" -owner=\"rtpearl0319\" -dll=\"GPSrvtTabWrapper.dll\" -check-update",
                FileName = exePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();

        // Utilize `StartsWith` to ignore any trailing new line characters for example: "true\n"
        return process.StandardOutput.ReadToEnd().StartsWith("true");
    }
    
    public bool ShouldUpdateOnShutdown()
    {
        return shouldUpdateOnShutdown;
    }

    public void UpdateOnShutdown()
    {
        shouldUpdateOnShutdown = true;
    }
}
