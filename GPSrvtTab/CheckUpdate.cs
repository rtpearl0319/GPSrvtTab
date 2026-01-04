using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;
using TaskDialogResult = Autodesk.Revit.UI.TaskDialogResult;

namespace GPSrvtTab;

[Transaction(TransactionMode.ReadOnly)]
public class CheckUpdateCommand : IExternalCommand 
{
        
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
            
        // Check for updates from the wrapper
        bool hasUpdate = GpsTab.GetCheckUpdateHandler().CheckForUpdate();

        if (hasUpdate)
        {
            // TaskDialog.Show("Revit", "A new update is available!");
            
            TaskDialog dialog = new TaskDialog("Update Available");
            dialog.MainInstruction = "There Is An Update Available, Would You Like To Install?";
            dialog.CommonButtons = TaskDialogCommonButtons.Yes |
                                   TaskDialogCommonButtons.No;
            
            TaskDialogResult result = dialog.Show();

            if (result != TaskDialogResult.Yes)
            {
                return Result.Succeeded;
            }
            
            // Tell Wrapper to update on shutdown
            GpsTab.GetCheckUpdateHandler().UpdateOnShutdown();
            TaskDialog.Show("Revit", "GPSrvtTab Will Be Updated After Revit Is Closed");
        }
        else
        {
            TaskDialog.Show("Revit", "You are using the latest version.");
        }

        return Result.Succeeded; 
    }
}