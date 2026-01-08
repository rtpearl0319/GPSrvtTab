using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
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

            if (doc.ActiveView.Category.Name != "Sheets")
            {
                TaskDialog.Show("Error", "Please run this command from a sheet view.");
                return Result.Failed;
            }

            // Require the user to pick a schedule element in the model (placed schedule on a sheet)
            ViewSchedule selectedSchedule = null;
            try
            {
                uidoc.Selection.GetElementIds();
                Reference pickedRef = uidoc.Selection.PickObject(ObjectType.Element, new ScheduleSelectionFilter(),
                    "Select a schedule on a sheet");
                Element el = doc.GetElement(pickedRef);

                if (el is ViewSchedule vs)
                {
                    selectedSchedule = vs;
                }
                else if (el is ScheduleSheetInstance ssi)
                {
                    // user picked the placed schedule on a sheet; resolve to the underlying ViewSchedule
                    Element scheduleView = doc.GetElement(ssi.ScheduleId);
                    if (scheduleView is ViewSchedule vs2)
                    {
                        selectedSchedule = vs2;
                    }
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }

            if (selectedSchedule == null)
            {
                TaskDialog.Show("Info", "No schedule selected. Please select a schedule element placed on a sheet.");
                return Result.Failed;
            }

            // now start the transaction and run the existing logic using selectedSchedule
            using (Transaction t = new Transaction(doc, "Print Set from Schedule"))
            {
                t.Start();

                // --- Insert adapted schedule-processing logic here, using 'selectedSchedule' ---
                ScheduleDefinition definition = selectedSchedule.Definition;

                int sheetNumberColIndex = -1;
                ElementId sheetNumParamId = new ElementId(BuiltInParameter.SHEET_NUMBER);

                IList<ScheduleFieldId> orderedFieldIds = definition.GetFieldOrder();

                for (int i = 0; i < orderedFieldIds.Count; i++)
                {
                    ScheduleField field = definition.GetField(orderedFieldIds[i]);
                    if (field.ParameterId == sheetNumParamId)
                    {
                        sheetNumberColIndex = i;
                        break;
                    }
                }

                if (sheetNumberColIndex == -1)
                {
                    TaskDialog.Show("Info",
                        $"Schedule '{selectedSchedule.Name}' does not contain a Sheet Number field.");
                    t.RollBack();
                    return Result.Failed;
                }

                // Pre-collect all sheets into a dictionary for fast lookup (sheet numbers are unique)
                Dictionary<string, ViewSheet> sheetDict = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>()
                    .ToDictionary(vs => vs.SheetNumber, vs => vs);

                // Read the body section data
                TableData tableData = selectedSchedule.GetTableData();
                TableSectionData bodySection = tableData.GetSectionData(SectionType.Body);

                if (bodySection == null || bodySection.NumberOfRows == 0)
                {
                    TaskDialog.Show("Info", $"Schedule '{selectedSchedule.Name}' has no data rows.");
                    t.RollBack();
                    return Result.Failed;
                }

                List<ViewSheet> sheetsInSchedule = new List<ViewSheet>();

                for (int row = 0; row < bodySection.NumberOfRows; row++)
                {
                    string sheetNumberText = selectedSchedule.GetCellText(SectionType.Body, row, sheetNumberColIndex);
                    if (sheetDict.TryGetValue(sheetNumberText, out var viewSheet))
                    {
                        sheetsInSchedule.Add(viewSheet);
                    }
                }

                if (sheetsInSchedule.Count == 0)
                {
                    TaskDialog.Show("Info", $"No sheets found in schedule '{selectedSchedule.Name}'.");
                    t.RollBack();
                    return Result.Failed;
                }

                ViewSet myViewSet = new ViewSet();
                foreach (ViewSheet view in sheetsInSchedule)
                {
                    myViewSet.Insert(view);
                }

                PrintManager printManager = doc.PrintManager;
                printManager.CombinedFile = true;
                printManager.PrintRange = PrintRange.Select;

                ViewSheetSetting viewSheetSetting = printManager.ViewSheetSetting;

                // Check for existing saved view sheet sets with the same name via reflection (robust to API version differences)
                var vssObj = viewSheetSetting;

                // Assign the views we collected
                viewSheetSetting.CurrentViewSheetSet.Views = myViewSet;

                // If the CurrentViewSheetSet exposes a Name property set it (some API versions use this when saving)
                var currentSet = viewSheetSetting.CurrentViewSheetSet;
                var nameProp = currentSet.GetType()
                    .GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
                if (nameProp != null && nameProp.CanWrite)
                {
                    nameProp.SetValue(currentSet, selectedSchedule.Name);
                }

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
                        MainInstruction = "Saving the print set failed.",
                        MainContent = "\nDo you want to delete the existing print set?",
                        CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No
                    };

                    TaskDialogResult promptResult = tdPrompt.Show();
                    if (promptResult == TaskDialogResult.No)
                    {
                        t.RollBack();
                        return Result.Cancelled;
                    }

                    vssObj.Delete();

                    TaskDialog.Show("Removed", "Previous view sheet set deleted.");
                }

                t.Commit();
                return Result.Succeeded;
            }
        }
    }

    // Simple selection filter to allow picking a ViewSchedule element in the model (if present)
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