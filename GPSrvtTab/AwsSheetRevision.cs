using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace GPSrvtTab;

[Transaction(TransactionMode.Manual)]
public class AwsSheetRevision : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIApplication uiapp = commandData.Application;
        UIDocument uidoc = uiapp.ActiveUIDocument;
        Application app = uiapp.Application;
        Document doc = uidoc.Document;
            
        using (Transaction t = new Transaction(doc, "Revisions On Sheet"))
        {
            t.Start();
            
            FilteredElementCollector sheetCollector = new FilteredElementCollector(doc).OfClass(typeof(ViewSheet));

            FilteredElementCollector revisionCollector = new FilteredElementCollector(doc).OfClass(typeof(Revision));

            Utilities utility = new Utilities(doc, sheetCollector, revisionCollector);
                
            utility.CheckRevParam();

            t.Commit();
        }
        return Result.Succeeded;
    }
    
    public class Utilities
    {
        Document document;
        FilteredElementCollector sheetCollector;
        FilteredElementCollector revisionCollector;

        public Utilities(Document document, FilteredElementCollector sheetCollector,
            FilteredElementCollector revisionCollector)
        {
            this.document = document;
            this.sheetCollector = sheetCollector;
            this.revisionCollector = revisionCollector;
        }
        public void CheckRevParam()
        {
            foreach (ViewSheet sheet in sheetCollector)
            {
                IList<ElementId> revIds = sheet.GetAllRevisionIds();

                foreach (Revision revision in revisionCollector)
                {
                    if (sheet.LookupParameter(revision.Description + " - SP") == null)
                    {
                        TaskDialog.Show("Error", $"Sheets Do Not Contain A Parameter For {revision.Description} - SP");
                        return;
                    }

                    sheet.LookupParameter(revision.Description + " - SP").Set(0);
                }

                if (revIds.Count > 0)
                {
                    foreach (ElementId eid in revIds)
                    {
                        Element elem = document.GetElement(eid);
                        Revision rev = elem as Revision;

                        sheet.LookupParameter(rev.Description + " - SP").Set(1);
                    }
                }
            }

            TaskDialog.Show("Success", "Completed Adding/Removing Revision Values From Sheets");
        }
    }
}