using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace GPSrvtTab
{
    [Transaction(TransactionMode.Manual)]
    public class SheetRevision : IExternalCommand
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
                    if (sheet.LookupParameter("Seq " + revision.SequenceNumber) == null)
                    {
                        TaskDialog.Show("Error", $"'Seq {revision.SequenceNumber}' WAS NOT FOUND AS A SHEET PARAMETER\n \n" +
                                                 $"PLEASE CREATE A NEW PROJECT PARAMETER\n" +
                                                 $"Name: Seq {revision.SequenceNumber}\n" +
                                                 $"Discipline: Common\n" +
                                                 $"Type Of Parameter: Text\n" +
                                                 $"Group Parameter Under: Identity Data\n" +
                                                 $"Categories: Sheets");
                        return;
                    }

                    sheet.LookupParameter("Seq " + revision.SequenceNumber).Set("");
                }

                if (revIds.Count > 0)
                {
                    foreach (ElementId eid in revIds)
                    {
                        Element elem = document.GetElement(eid);
                        Revision rev = elem as Revision;

                        //TaskDialog.Show("Sheet Revision", sheet.LookupParameter($"Seq {rev.SequenceNumber.ToString()}").AsValueString());

                        sheet.LookupParameter("Seq " + rev.SequenceNumber).Set("●");
                    }
                }
            }

            TaskDialog.Show("Success", "Completed Adding/Removing Revision Values From Sheets");
        }
    }
}