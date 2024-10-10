namespace GPSrvtTabInstaller;

using System;
using WixSharp;

class Program
{
    public static void Main(string[] args)
    {
        var project = new Project("GPSrvtTab",
            
            new Dir(@"%ProgramFiles64Folder%\GPSrvtTab",
                new File(@"Files\GPSrvtTab.dll")),
            new Dir(@"%CommonAppDataFolder%\Autodesk\Revit\Addins\2021",
                new File(@"Files\GPSTab.addin")),
            new Dir(@"%CommonAppDataFolder%\Autodesk\Revit\Addins\2022",
                new File(@"Files\GPSTab.addin")),
            new Dir(@"%CommonAppDataFolder%\Autodesk\Revit\Addins\2023",
                new File(@"Files\GPSTab.addin")),
            new Dir(@"%CommonAppDataFolder%\Autodesk\Revit\Addins\2024",
                new File(@"Files\GPSTab.addin"))
            
        );
        
        project.LicenceFile = "Files\\License.rtf";
        project.GUID = new Guid("4A12B7E4-54C1-46EF-9C4B-10CCB407AF07");
    
        Compiler.BuildMsi(project);
    }
}