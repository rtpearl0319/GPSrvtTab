using System;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace GPSrvtTab
{
    public class GpsTab : IExternalApplication
    {
        
        public Result OnStartup(UIControlledApplication application)
        {
            var versionNumber = application.ControlledApplication.VersionNumber;
            
            AddRibbonPanel(application);
            return Result.Succeeded;
        }
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
        static void AddRibbonPanel(UIControlledApplication application)
        {
            var tabName = "GPS Tools";
            application.CreateRibbonTab(tabName);
            var assembly = Assembly.GetExecutingAssembly();
            
            RibbonPanel ribbonPanel = application.CreateRibbonPanel(tabName, "Electrical Circuiting");
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
            
            PushButtonData elecCircuitData = new PushButtonData("cmdElectricalCircuit", 
                "Create Circuit", thisAssemblyPath, "GPSrvtTab.ElectricalCircuit");
            
            PushButton elecCircuitButton = ribbonPanel.AddItem(elecCircuitData) as PushButton;
            elecCircuitButton.ToolTip = "Select Elements then Select Panel To Circuit To";
            elecCircuitButton.LargeImage = LoadEmbeddedIcon(assembly, "GPSrvtTab.Resources.ElectricalCircuit.png");
            
            PushButtonData circuitRenamer = new PushButtonData("cmdCircuitLoadRenamer",
                "Rename Circuits", thisAssemblyPath, "GPSrvtTab.CircuitLoadRenamer");
            
            PushButton circuitRenamerButton = ribbonPanel.AddItem(circuitRenamer) as PushButton;
            circuitRenamerButton.ToolTip = "Rename All Circuits Based On Parameter Values";
            circuitRenamerButton.LargeImage = LoadEmbeddedIcon(assembly, "GPSrvtTab.Resources.RenamerIcon.png");
        }
        public static BitmapImage LoadEmbeddedIcon(Assembly assembly, string name)
        {
            /*const string resourceName = "GPSrvtTab.Resources.ElectricalCircuit.png";
            const string renamerIcon = "GPSrvtTab.Resources.RenamerIcon.png";*/
            
            // Open the resource stream
            using (var stream = assembly.GetManifestResourceStream(name))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = stream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // Optional: Makes the image thread-safe.
                return bitmapImage;
            }
        }
        
        // private static ImageSource LoadSvgImage(Assembly assembly, string name)
        // {
        //     using (var stream = assembly.GetManifestResourceStream(name))
        //     {
        //         if (stream == null)
        //             throw new FileNotFoundException($"Resource '{name}' not found.");
        //
        //         // Create a WPF Drawing to hold the SVG
        //         var svgConverter = new FileSvgReader(new WpfDrawingSettings());
        //         var drawing = svgConverter.Read(stream);
        //         var drawingImage = new DrawingImage(drawing);
        //         drawingImage.Freeze(); // Optional: Makes the image thread-safe.
        //         return drawingImage;
        //     }
        // }
    }
}