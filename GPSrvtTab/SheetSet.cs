using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace GPSrvtTab
{
    [Transaction(TransactionMode.Manual)]
    public class SheetSet : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            
            using Transaction t = new Transaction(doc, "Print Set from Schedule");
            t.Start();

            ElementId matchingPrintSet = null;

            if (doc.ActiveView.Category.Name != "Sheets")
            {
                TaskDialog.Show("Error", "Please run this command from a sheet view.");
                return Result.Failed;
            }

            ViewSchedule selectedSchedule;
            try
            {
                selectedSchedule = PickSchedule(uidoc);
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
            
            FilteredElementCollector projectPrintSets = new FilteredElementCollector(doc).OfClass(typeof(ViewSheetSet));
            foreach (var printSet in projectPrintSets)
            {
                if (printSet.Name == selectedSchedule.Name)
                {
                    matchingPrintSet = printSet.Id;
                }
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

            t.Commit();
            
            PrintSetSave(doc, sheetsInSchedule, selectedSchedule, matchingPrintSet);
            return Result.Succeeded;
        }
        
        public ViewSchedule PickSchedule(UIDocument uidoc)
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
#if REVIT2020 || REVIT2021 || REVIT2022 || REVIT2023
                if (definition.GetField(orderedFieldIds[i]).ParameterId.IntegerValue == (int)paramId)
                {
                    return i;
                }
#else
                if (definition.GetField(orderedFieldIds[i]).ParameterId.Value == (int)paramId)
                {
                    return i;
                }
#endif
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

        public bool PrintSetSave(Document doc, List<ViewSheet> sheetsInSchedule, ViewSchedule selectedSchedule, ElementId printSetId)
        {
            ViewSet myViewSet = new ViewSet();
            foreach (ViewSheet view in sheetsInSchedule)
                myViewSet.Insert(view);

            PrintManager printManager = doc.PrintManager;
            printManager.SelectNewPrintDriver("Bluebeam PDF");
            printManager.Apply();
            printManager.CombinedFile = true;
            printManager.PrintRange = PrintRange.Select;

            // Handle an existing set with the same name BEFORE saving
            if (printSetId != null)
            {
                TaskDialog tdPrompt = new TaskDialog("Print Set Exists")
                {
                    MainInstruction = "There is already a print set with this name.",
                    MainContent = "\nDo you want to replace it?",
                    CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No
                };
                if (tdPrompt.Show() != TaskDialogResult.Yes)
                    return false;

                using (Transaction txDel = new Transaction(doc, "Delete Previous Print Set"))
                {
                    txDel.Start();
                    doc.Delete(printSetId);
                    txDel.Commit();
                }
            }

            using (Transaction tx = new Transaction(doc, "Print Set"))
            {
                tx.Start();
                ViewSheetSetting viewSheetSetting = printManager.ViewSheetSetting; // acquired fresh, after any delete
                viewSheetSetting.CurrentViewSheetSet.Views = myViewSet;
                viewSheetSetting.SaveAs(selectedSchedule.Name);
                tx.Commit();
            }

            TaskDialog.Show("Success", "Print set saved.");
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