using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using GPSrvtTab.Extensions.SelectionExtensions;

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
            
            //Pick Elements You Want Circuited
            IList<Reference> secElements = uidoc.Selection.PickObjects(ObjectType.Element, new FixtureSelectionFilter(), 
                "Select Elements You Want Circuited");
            List<ElementId> ids = (from Reference r in secElements select r.ElementId).ToList();

            //Pick the Panel to Circuit to
            Reference panel = uidoc.Selection.PickObject(ObjectType.Element, new EquipmentSelectionFilter(), "Select Panel You Want To Circuit To");
            Element element = doc.GetElement(panel);
            
            using (Transaction t = new Transaction(doc, "Transaction"))
            {
                t.Start();
                
                //List<string> panelVerification = new List<string>();
                
                List<string> elecIdsUnCircuit = new List<string>();
                
                string panelConnectorType = "";
                
                foreach (Reference r in secElements)
                {
                    if (!(doc.GetElement(r) is FamilyInstance))
                    {
                        continue;
                    }
                    
                    FamilyInstance? fixtureInstance = doc.GetElement(r) as FamilyInstance;
                    
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
                     
#if REVIT2023 || REVIT2023 || REVIT2024 || REVIT2025
                     
                    var panelConnector = ElementGetConnector(panelFi);
                    
                    /*if (panelConnector.ElectricalSystemType.ToString() != fixtureConnector.ElectricalSystemType.ToString())
                    {
                        panelConnectorType = panelConnector.ElectricalSystemType.ToString();
                        panelVerification.Add(" Fixture Connector: " + fixtureConnector.ElectricalSystemType + " - " + fixtureInstance.Id);
                    }*/
#endif
                    
                    ConnectorSet connectorSet = fixtureInstance.MEPModel.ConnectorManager.Connectors;
                    
                    foreach (Connector connector in connectorSet)
                    {
                        if (elecsystem.Count == 0)
                        {
                            if (fixtureConnector != null)
                            {
                                ElectricalSystem newElectricalSystem = ElectricalSystem.Create(connector, 
                                    fixtureConnector.ElectricalSystemType);
                                
                                newElectricalSystem.SelectPanel(panelFi);
                            }
                        }
                    }
                }
                /*if (panelVerification.Count > 0)
                {
                    #if Revit2020 || Revit2021

                    TaskDialog.Show("Error", "Panel Does Not Have A Connector For Verification.\n " +
                    "Please Check The Panel To Verify Circuit Was Created");
                    #endif
                    
                    TaskDialog.Show("Could Not Verify Connector \n",  "Panel Connector: " + panelConnectorType + "\n"
                                                             + string.Join("\n", panelVerification));
                }*/

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