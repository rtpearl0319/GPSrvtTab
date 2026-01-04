using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;

namespace GPSrvtTab
{
    //Begin Transaction and Transaction Mode to Manual
    [Transaction(TransactionMode.Manual)]
    //**********Create External Command**********
    public class CircuitLoadRenamer : IExternalCommand
    {
        //**********Execute the command**********
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //Get the current document and application
            var uiapp = commandData.Application;
            var app = uiapp.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;
            //Use FilteredElementCollector to get all the elements of the Built In Category
            //Once you create a new FilteredElementCollector, you can search for elements in the specified category
            var allElements = GetElements(
                doc, 
                BuiltInCategory.OST_ElectricalFixtures, 
                BuiltInCategory.OST_SecurityDevices,
                BuiltInCategory.OST_DataDevices
            );
            //Create a new transaction
            using (var t = new Transaction(doc, "Change Parameter Value"))
            {
                //Start the transaction
                t.Start();
                //Loop through the elements in the collection
                foreach (var electricalSystem in GetElectricalSystems(doc, allElements))
                {
                    UpdateParameters(electricalSystem);
                }
                //Commit the transaction
                t.Commit();
            }
            return Result.Succeeded;
        }
        private IList<Element> GetElements(Document doc, params BuiltInCategory[] categories)
        {
            var elements = new List<Element>();

            foreach (var category in categories)
            {
                var collector = new FilteredElementCollector(doc).OfCategory(category).ToElements();
                elements.AddRange(collector);
            }
            return elements;
        }
        private List<ElectricalSystem> GetElectricalSystems(Document doc, IList<Element> elements)
        {
            var electricalSystems = new List<ElectricalSystem>();
            
            foreach (Element element in elements)
            {
                if (!(element is FamilyInstance familyInstance))
                {
                    continue;
                }

                    #if REVIT2020 || REVIT2021 
                     foreach (ElectricalSystem electricalSystem in familyInstance.MEPModel.ElectricalSystems)
                     {
                     #else
                     foreach (ElectricalSystem electricalSystem in familyInstance.MEPModel.GetElectricalSystems())
                     {
                    #endif  
                    if (electricalSystem.Elements.Size != 1)
                    {
                        TaskDialog.Show("Error", "Circuit system must have exactly one element" + '\n' + "Element ID: " + familyInstance.Id);
                        continue;
                    }
                    electricalSystems.Add(electricalSystem);
                }
            }
            return electricalSystems;
        }
        
        private void UpdateParameters(ElectricalSystem electricalSystem)
        {
            if (electricalSystem.Elements.Size != 1)
            {
                TaskDialog.Show("Error", "Electrical system must have exactly one element");
                return;
            }
            var elementIterator = electricalSystem.Elements.ForwardIterator();
            elementIterator.MoveNext();
            var familyInstance = elementIterator.Current as FamilyInstance;
            
            //Parameters to add for load name
            //familyInstance.LookupParameter for Instance Parameters
            //familyInstance.Symbol.LookupParameter for Type Parameters
            var paramGPSiteCode = familyInstance.Symbol.LookupParameter("GP_SiteCode");
            var paramGPSuiteName = familyInstance.LookupParameter("GP_SuiteName");
            var paramGPLocation = familyInstance.LookupParameter("GP_Location");
            var paramGPDevLabel = familyInstance.Symbol.LookupParameter("GP_DeviceLabel");
            var paramGPRoomDesc = familyInstance.LookupParameter("GP_RoomDescription");
            var paramGPDevCom = familyInstance.LookupParameter("GP_DeviceComments");
            
            //Build the parameter string
            var parameterValue = BuildParameterString(paramGPSiteCode, paramGPSuiteName, 
                paramGPLocation, paramGPDevLabel, paramGPRoomDesc, paramGPDevCom);
            //Set the LoadName property of the electrical system
            electricalSystem.LoadName = parameterValue;
        }
        //**********Build the parameter string**********
        private string BuildParameterString(params Parameter[] parameters) {
            //Create a new StringBuilder
            var combined = new StringBuilder();
            //Loop through the parameters
            foreach (var parameter in parameters) {
                //Check if the parameter is not null
                if (parameter != null) {
                    combined.Append(parameter.AsString());
                    combined.Append("_");
                }
            }
            return combined.ToString().Replace("__", "").TrimStart('_').TrimEnd('_');
            //Return the combined string
        }
    }
}