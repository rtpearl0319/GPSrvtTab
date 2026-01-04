using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using GPSrvtTabWrapper;

namespace GPSrvtTab
{
    public class GpsTab : IExternalApplication
    {
        
        // Gets set by the wrapper
        private static ICheckUpdate wrapperCheckUpdateHandler;
        
        
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
            
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
            
            RibbonPanel elecRibbonPanel = application.CreateRibbonPanel(tabName, "Electrical Circuiting");
            RibbonPanel sheetRibbonPanel = application.CreateRibbonPanel(tabName, "Sheet Revisions");
            RibbonPanel updateRibbonPanel = application.CreateRibbonPanel(tabName, "Update");
            
            //-------------------------------------------------------------------------------------------------------
            
            PushButtonData elecCircuitData = new PushButtonData("cmdElectricalCircuit", 
                "Create Circuit", thisAssemblyPath, "GPSrvtTab.ElectricalCircuit");
            
            PushButton elecCircuitButton = elecRibbonPanel.AddItem(elecCircuitData) as PushButton;
            elecCircuitButton.ToolTip = "Select Elements then Select Panel To Circuit To";
            elecCircuitButton.LargeImage = LoadEmbeddedIcon(assembly, "GPSrvtTab.Resources.Receptacle32.png");
            
            PushButtonData circuitRenamer = new PushButtonData("cmdCircuitLoadRenamer",
                "Rename Circuits", thisAssemblyPath, "GPSrvtTab.CircuitLoadRenamer");
            
            PushButton circuitRenamerButton = elecRibbonPanel.AddItem(circuitRenamer) as PushButton;
            circuitRenamerButton.ToolTip = "Rename All Circuits Based On Parameter Values";
            circuitRenamerButton.LargeImage = LoadEmbeddedIcon(assembly, "GPSrvtTab.Resources.Concat32.png");
            
            //-------------------------------------------------------------------------------------------------------
            
            PushButtonData SheetRevision = new PushButtonData("cmdSheetRevision",
                "Sheet Revision", thisAssemblyPath, "GPSrvtTab.SheetRevision");
            
            SheetRevision.Image = LoadEmbeddedIcon(assembly, "GPSrvtTab.Resources.SheetRev16.png");
            
            PushButtonData AwsSheetRevision = new PushButtonData("cmdAwsSheetRevision",
                "AWS Revision", thisAssemblyPath, "GPSrvtTab.AwsSheetRevision");
            
            AwsSheetRevision.Image = LoadEmbeddedIcon(assembly, "GPSrvtTab.Resources.AwsRev16.png");
            
            var sheetStack = sheetRibbonPanel.AddStackedItems(SheetRevision, AwsSheetRevision);

            sheetStack[0].ToolTip = "Adds Dots For Schedule Issuance based On Clouds On Sheet";
            sheetStack[1].ToolTip = "Set AWS Shared Sheet Revision Checkbox";
            
            //-------------------------------------------------------------------------------------------------------
            
            PushButtonData CheckUpdate = new PushButtonData("cmdCheckUpdate",
                "Check for updates", thisAssemblyPath, "GPSrvtTab.CheckUpdateCommand");
            
            PushButton CheckUpdateButton = updateRibbonPanel.AddItem(CheckUpdate) as PushButton;
            CheckUpdateButton.ToolTip = "Check For New Update";
            CheckUpdateButton.LargeImage = LoadEmbeddedIcon(assembly, "GPSrvtTab.Resources.Download32.png");
        }
        
        //-------------------------------------------------------------------------------------------------------
        
        public static BitmapImage LoadEmbeddedIcon(Assembly assembly, string name)
        {
            
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

        //DO NOT DELETE... LIKE LITERALLY EVER
        public static void SetCheckUpdateHandler(ICheckUpdate checkUpdateHandler)
        {
            wrapperCheckUpdateHandler = checkUpdateHandler;
        }
        
        public static ICheckUpdate GetCheckUpdateHandler()
        {
            return wrapperCheckUpdateHandler;
        }
        
    }
}