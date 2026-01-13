using Autodesk.Internal.Windows;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using GPSrvtTab.Extensions.SelectionExtensions;
using InvalidOperationException = Autodesk.Revit.Exceptions.InvalidOperationException;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

namespace GPSrvtTab
{
    [Transaction(TransactionMode.Manual)]
    public class ElectricalCircuit : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            Reference panel;
            IList<Reference> selectedElements;
            Element element;
            try
            {
                //Pick Elements You Want Circuited
                selectedElements = uidoc.Selection.PickObjects(ObjectType.Element,
                    new FixtureSelectionFilter(),
                    "Select Elements You Want Circuited");
                List<ElementId> ids = (from Reference r in selectedElements select r.ElementId).ToList();

                if (selectedElements == null || selectedElements.Count == 0)
                {
                    return Result.Cancelled;
                }

                //Pick the Panel to Circuit to
                panel = uidoc.Selection.PickObject(ObjectType.Element, new EquipmentSelectionFilter(),
                    "Select Panel You Want To Circuit To");
                element = doc.GetElement(panel.ElementId);
            }
            catch(OperationCanceledException)
            {
                return Result.Cancelled;
            }
            
            using (Transaction t = new Transaction(doc, "Transaction"))
            {
                t.Start();
                
                List<string> elecIdsUnCircuit = new List<string>();
                
                string panelConnectorType = "";
                
                foreach (Reference r in selectedElements)
                {
                    if (!(doc.GetElement(r) is FamilyInstance))
                    {
                        continue;
                    }
                    
                    
                    FamilyInstance? fixtureInstance = doc.GetElement(r.ElementId) as FamilyInstance;
                    
                    var elecsystem = new List<ElectricalSystem>();
                    
#if REVIT2020 || REVIT2021 
                        foreach (ElectricalSystem elecSys in fixtureInstance.MEPModel.ElectricalSystems)
                        {
#else
                    foreach (ElectricalSystem elecSys in fixtureInstance?.MEPModel.GetElectricalSystems()!)
                    {
#endif 
                        elecsystem.Add(elecSys);
                    }
                    
                    foreach (var elementElec in elecsystem)
                    {
                        if (elementElec.PanelName != "")
                        {
                            elecIdsUnCircuit.Add(elementElec.Id.ToString());
                        }
                        //Add Elements to List
                    }
                    
                    var fixtureConnector = ElementGetConnector(fixtureInstance);
                    
                    FamilyInstance? panelFi = element as FamilyInstance;
                    
                    ConnectorSet connectorSet = fixtureInstance.MEPModel.ConnectorManager.Connectors;
                    
                    foreach (Connector connector in connectorSet)
                    {
                        
                        if (elecsystem.Count == 0 && connector.Domain == Domain.DomainElectrical)
                        {
                            ElectricalSystem newElectricalSystem = ElectricalSystem.Create(connector, 
                                connector.ElectricalSystemType);

                            try
                            {
                                newElectricalSystem.SelectPanel(panelFi);
                            }
                            catch(InvalidOperationException)
                            {
                                TaskDialog.Show("Error", $"{fixtureInstance.Symbol.Family.Name} cannot be connected to {panelFi.Symbol.Family.Name}");
                                return Result.Failed;
                            }                                                                    
                        }
                        
                    }
                }
                
                if (elecIdsUnCircuit.Count > 0) 
                    TaskDialog.Show("Circuited Elements", "Elements Are Already Circuited\n"+ string.Join
                        (", ", elecIdsUnCircuit));
                
                t.Commit();
            }
            return Result.Succeeded;
        }
        public Connector? ElementGetConnector(FamilyInstance? familyInstance)
        {
            var familyInstanceEnumerator = familyInstance?.MEPModel.ConnectorManager.UnusedConnectors.GetEnumerator();
            using var familyInstanceEnumerator1 = familyInstanceEnumerator as IDisposable;

            if(!familyInstanceEnumerator!.MoveNext())
            {
                return null;
            }
            Connector? familyInstanceConnector = familyInstanceEnumerator.Current as Connector;
        
            return familyInstanceConnector;
        }
    }
}