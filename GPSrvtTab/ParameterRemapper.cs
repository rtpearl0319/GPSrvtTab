using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
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

    public class ParameterRemapperEventHandler : IExternalEventHandler
    {
        private List<MainWindow.ParamInfoUpdate> paramInfoUpdates;
        private Document doc;
        
        public ParameterRemapperEventHandler(Document doc)
        {
            this.doc = doc;
        }
        
        public void Execute(UIApplication app)
        {
            using (Transaction t = new Transaction(doc, "Transaction"))
            {
                t.Start();
                foreach (MainWindow.ParamInfoUpdate paramInfoUpdate in paramInfoUpdates)
                {
                    paramInfoUpdate.GetElectricalSystem().LoadName = paramInfoUpdate.GetNewValue();
                }
                t.Commit();
            }
            TaskDialog.Show("Submitted", "Circuits have been renamed");
        }

        public string GetName()
        {
            return "ParameterRemapper";
        }

        public void SetParamInfoUpdates(List<MainWindow.ParamInfoUpdate> paramInfoUpdates)
        {
            this.paramInfoUpdates =  paramInfoUpdates;
        }
    }
}