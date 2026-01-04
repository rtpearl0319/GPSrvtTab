using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ParameterRemapperUI
{
    [Transaction(TransactionMode.Manual)]
    public class ParameterRemapper : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            new MainWindow(uiDoc).Show();
            return Result.Succeeded;
        }
    }

}