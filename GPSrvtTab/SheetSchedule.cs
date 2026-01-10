using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace GPSrvtTab
{
    [Transaction(TransactionMode.Manual)]
    public class SheetSchedule : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            
            using Transaction t = new Transaction(doc, "Print Set from Schedule");
            t.Start();

            if (doc.ActiveView.Category.Name != "Sheets")
            {
                TaskDialog.Show("Error", "Please run this command from a sheet view.");
                return Result.Failed;
            }

            /*ICollection<ElementId> selectionId = uidoc.Selection.GetElementIds();
            List<ViewSchedule> currentSelectedSchedule = new List<ViewSchedule>();
            if (selectionId.Count > 0)
            {
                foreach (ElementId id in selectionId)
                {
                    Element elem = doc.GetElement(id);
                    ViewSchedule schedule = elem as ViewSchedule;
                    if (schedule != null)
                    {
                        currentSelectedSchedule.Add(schedule);
                    }
                }
            }*/

            ViewSchedule selectedSchedule;
            try
            {
                selectedSchedule = pickSchedule(uidoc);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                TaskDialog.Show("error", "Operation cancelled.");
                return Result.Cancelled;
            }

            if (selectedSchedule == null)
            {
                TaskDialog.Show("Info", "No schedule selected. Please select a schedule element placed on a sheet.");
                return Result.Failed;
            }

            ScheduleDefinition definition = selectedSchedule.Definition;

            int sheetNumberColIndex = FindParameterColumnIndex(definition, BuiltInParameter.SHEET_NUMBER);
            if (sheetNumberColIndex == -1)
            {
                TaskDialog.Show("Info",
                    $"Schedule '{selectedSchedule.Name}' does not contain a Sheet Number field.");
                return Result.Failed;
            }

            List<ViewSheet> sheetsInSchedule = GetAllSheetsAtColumn(doc, selectedSchedule, sheetNumberColIndex);
            if (sheetsInSchedule.Count == 0)
            {
                TaskDialog.Show("Info", $"No sheets found in schedule '{selectedSchedule.Name}'.");
                return Result.Failed;
            }

            if (!PrintSetSave(doc, sheetsInSchedule, selectedSchedule))
            {
                TaskDialog.Show("error", "Failed to save print set");
            }

            /*ViewSet myViewSet = new ViewSet();
            foreach (ViewSheet view in sheetsInSchedule)
            {
                myViewSet.Insert(view);
            }

            PrintManager printManager = doc.PrintManager;
            printManager.CombinedFile = true;
            printManager.PrintRange = PrintRange.Select;

            ViewSheetSetting viewSheetSetting = printManager.ViewSheetSetting;
                
            var vssObj = viewSheetSetting; // Check for existing saved view sheet sets with the same name via reflection (robust to API version differences)
            viewSheetSetting.CurrentViewSheetSet.Views = myViewSet; // Assign the views we collected
                
            try
            {
                // Finally save the current view sheet set under the schedule name
                viewSheetSetting.SaveAs(selectedSchedule.Name);
                TaskDialog.Show("Success", "Print set saved.");  
            }
            catch (Exception)
            {

                TaskDialog tdPrompt = new TaskDialog("Save Print Set Failed")
                {
                    MainInstruction = "There is already a print set with this name.",
                    MainContent = "\nDo you want to delete the existing print set?",
                    CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No
                };

                if (tdPrompt.Show() == TaskDialogResult.No)
                {
                    return Result.Cancelled;
                }

                vssObj.Delete();
                TaskDialog.Show("Removed", "Previous print set deleted.");
            }*/

            t.Commit();
            return Result.Succeeded;
        }
        
        public ViewSchedule pickSchedule(UIDocument uidoc)
        {
            Reference pickedRef = uidoc.Selection.PickObject(ObjectType.Element, new ScheduleSelectionFilter(),
                "Select a schedule on a sheet"); // Require the user to pick a schedule element in the model (placed schedule on a sheet)
            Element el = uidoc.Document.GetElement(pickedRef);

            if (el is ViewSchedule vs)
            {
                return vs;
            }
            if (el is ScheduleSheetInstance ssi)
            {
                // user picked the placed schedule on a sheet; resolve to the underlying ViewSchedule
                Element scheduleView = uidoc.Document.GetElement(ssi.ScheduleId);
                if (scheduleView is ViewSchedule vs2)
                {
                    return vs2;
                }
            }
            return null;
        }

        public int FindParameterColumnIndex(ScheduleDefinition definition, BuiltInParameter paramId)
        {
            IList<ScheduleFieldId> orderedFieldIds = definition.GetFieldOrder();

            for (int i = 0; i < orderedFieldIds.Count; i++)
            {
                if (definition.GetField(orderedFieldIds[i]).ParameterId.IntegerValue == (int)paramId)
                {
                    return i;
                }
            }
            return -1;
        }

        public List<ViewSheet> GetAllSheetsAtColumn(Document doc, ViewSchedule viewSchedule, int sheetNumberColIndex)
        {
            Dictionary<string, ViewSheet> sheetDict = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .ToDictionary(vs => vs.SheetNumber, vs => vs);
            
            TableData tableData = viewSchedule.GetTableData();
            TableSectionData bodySection = tableData.GetSectionData(SectionType.Body);
            
            List<ViewSheet> sheetsInSchedule = new List<ViewSheet>();
            
            if (bodySection == null || bodySection.NumberOfRows == 0)
            {
                return sheetsInSchedule;
            }

            for (int row = 0; row < bodySection.NumberOfRows; row++)
            {
                string sheetNumberText = viewSchedule.GetCellText(SectionType.Body, row, sheetNumberColIndex);
                if (sheetDict.TryGetValue(sheetNumberText, out var viewSheet))
                {
                    sheetsInSchedule.Add(viewSheet);
                }
            }
            return sheetsInSchedule;
        }

        public bool PrintSetSave(Document doc, List<ViewSheet> sheetsInSchedule, ViewSchedule selectedSchedule)
        {
            
            ViewSet myViewSet = new ViewSet();
            foreach (ViewSheet view in sheetsInSchedule)
            {
                myViewSet.Insert(view);
            }

            PrintManager printManager = doc.PrintManager;
            printManager.CombinedFile = true;
            printManager.PrintRange = PrintRange.Select;

            ViewSheetSetting viewSheetSetting = printManager.ViewSheetSetting;
            viewSheetSetting.CurrentViewSheetSet.Views = myViewSet; // Assign the views we collected
            
            try
            {
                // Finally save the current view sheet set under the schedule name
                viewSheetSetting.SaveAs(selectedSchedule.Name);
                TaskDialog.Show("Success", "Print set saved.");  
            }
            catch (Exception)
            {
                
                TaskDialog tdPrompt = new TaskDialog("Save Print Set Failed")
                {
                    MainInstruction = "There is already a print set with this name.",
                    MainContent = "\nDo you want to delete the existing print set?",
                    CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No
                };

                if (tdPrompt.Show() == TaskDialogResult.No)
                {
                    return false;
                }

                viewSheetSetting.Delete();
                TaskDialog.Show("Removed", "Previous print set deleted.");

                try
                {
                    // Finally save the current view sheet set under the schedule name
                    viewSheetSetting.SaveAs(selectedSchedule.Name);
                    TaskDialog.Show("Success", "Print set saved.");
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return true;
        }
    }

    public class ScheduleSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is ViewSchedule || elem is ScheduleSheetInstance;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
    
}