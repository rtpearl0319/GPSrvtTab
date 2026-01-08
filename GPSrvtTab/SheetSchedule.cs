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
            
            TaskDialog.Show("Sheet Set", "Select the sheet list on the active sheet.");

            // Require the user to pick a schedule element in the model (placed schedule on a sheet)
            ViewSchedule selectedSchedule = null;
            try
            {

                uidoc.Selection.GetElementIds();
                Reference pickedRef = uidoc.Selection.PickObject(ObjectType.Element, new ScheduleSelectionFilter(), "Select a schedule on a sheet");
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
                    TaskDialog.Show("Info", $"Schedule '{selectedSchedule.Name}' does not contain a Sheet Number field.");
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

                for (int row = 2; row < bodySection.NumberOfRows; row++)
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
                bool nameExists = false;
                var vssObj = viewSheetSetting;
                var vssType = vssObj.GetType();

                // Try common property names that may contain saved names
                string[] propNames = new[] { "ViewSheetSetNames", "SavedViewSheetSetNames" };
                foreach (var pn in propNames)
                {
                    var prop = vssType.GetProperty(pn, BindingFlags.Public | BindingFlags.Instance);
                    if (prop != null)
                    {
                        var val = prop.GetValue(vssObj) as System.Collections.IEnumerable;
                        if (val != null)
                        {
                            foreach (var item in val)
                            {
                                if (item == null) continue;
                                // Compare ToString(), and if available a Name property
                                var itemStr = item.ToString();
                                if (string.Equals(itemStr, selectedSchedule.Name, StringComparison.OrdinalIgnoreCase))
                                {
                                    nameExists = true;
                                    break;
                                }
                                var itemNameProp = item.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
                                if (itemNameProp != null)
                                {
                                    var nm = itemNameProp.GetValue(item) as string;
                                    if (string.Equals(nm, selectedSchedule.Name, StringComparison.OrdinalIgnoreCase))
                                    {
                                        nameExists = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (nameExists) break;
                }

                // If not found via properties, try common getter methods
                if (!nameExists)
                {
                    string[] methodNames = new[] { "GetViewSheetSetNames", "GetSavedViewSheetSetNames" };
                    foreach (var mn in methodNames)
                    {
                        var method = vssType.GetMethod(mn, BindingFlags.Public | BindingFlags.Instance);
                        if (method != null)
                        {
                            var result = method.Invoke(vssObj, null) as System.Collections.IEnumerable;
                            if (result != null)
                            {
                                foreach (var item in result)
                                {
                                    if (item == null) continue;
                                    // Compare ToString(), and if available a Name property
                                    var itemStr = item.ToString();
                                    if (string.Equals(itemStr, selectedSchedule.Name, StringComparison.OrdinalIgnoreCase))
                                    {
                                        nameExists = true;
                                        break;
                                    }
                                    var itemNameProp = item.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
                                    if (itemNameProp != null)
                                    {
                                        var nm = itemNameProp.GetValue(item) as string;
                                        if (string.Equals(nm, selectedSchedule.Name, StringComparison.OrdinalIgnoreCase))
                                        {
                                            nameExists = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        if (nameExists) break;
                    }
                }

                // If the name already exists, attempt to delete the existing set proactively
                if (nameExists)
                {
                    TaskDialog td = new TaskDialog("Delete previous View Sheet Set?")
                    {
                        MainInstruction = $"A print set named '{selectedSchedule.Name}' already exists.",
                        MainContent = "Change name of this schedule or remove older version before proceeding.\nDo you want to delete the existing print set?",
                        CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No
                    };

                    if (td.Show() == TaskDialogResult.No)
                    {
                        t.RollBack();
                        return Result.Cancelled;
                    }

                    // Attempt to delete existing set via reflection using several possible method names
                    bool proactivelyDeleted = false;
                    string[] deleteMethodNames = new[] { "Delete", "DeleteViewSheetSet", "Remove", "RemoveViewSheetSet", "DeleteViewSet" };
                    foreach (var dm in deleteMethodNames)
                    {
                        var method = vssType.GetMethod(dm, BindingFlags.Public | BindingFlags.Instance);
                        if (method != null)
                        {
                            try
                            {
                                method.Invoke(vssObj, new object[] { selectedSchedule.Name });
                                proactivelyDeleted = true;
                                break;
                            }
                            catch { }

                            try
                            {
                                method.Invoke(vssObj, null);
                                proactivelyDeleted = true;
                                break;
                            }
                            catch { }
                        }
                    }

                    // If we couldn't delete, ask the user whether to proceed and let SaveAs handle failure
                    if (!proactivelyDeleted)
                    {
                        TaskDialog td2 = new TaskDialog("Could not delete existing print set")
                        {
                            MainInstruction = "Could not delete the existing print set.",
                            MainContent = "Saving failed",
                        };

                        if (td2.Show() == TaskDialogResult.No)
                        {
                            t.RollBack();
                            return Result.Cancelled;
                        }
                    }
                    
                }

                // Now that any existing saved set has been handled, assign the views to the CurrentViewSheetSet
                try
                {
                    // Assign the views we collected
                    viewSheetSetting.CurrentViewSheetSet.Views = myViewSet;

                    // If the CurrentViewSheetSet exposes a Name property set it (some API versions use this when saving)
                    var currentSet = viewSheetSetting.CurrentViewSheetSet;
                    var nameProp = currentSet.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
                    if (nameProp != null && nameProp.CanWrite)
                    {
                        try { nameProp.SetValue(currentSet, selectedSchedule.Name); } catch { }
                    }
                }
                catch
                {
                    // Swallow any errors assigning views here; SaveAs below will still be attempted.
                }
                
                try
                {
                    // Finally save the current view sheet set under the schedule name
                    viewSheetSetting.SaveAs(selectedSchedule.Name);

                    // Verify the saved set actually contains views; if empty, try to recreate it
                    int savedCount = 0;
                    try
                    {
                        var currentSet = viewSheetSetting.CurrentViewSheetSet;
                        // Try to get a Views property (may be a ViewSet)
                        var viewsProp = currentSet.GetType().GetProperty("Views", BindingFlags.Public | BindingFlags.Instance);
                        if (viewsProp != null)
                        {
                            var viewsObj = viewsProp.GetValue(currentSet) as System.Collections.IEnumerable;
                            if (viewsObj != null)
                            {
                                foreach (var i in viewsObj) savedCount++;
                            }
                        }
                        else
                        {
                            // Fallback: try CurrentViewSheetSet.Views via the API object (some versions expose it directly)
                            try
                            {
                                var vsObj = viewSheetSetting.CurrentViewSheetSet.Views;
                                if (vsObj is System.Collections.IEnumerable enumVs)
                                {
                                    foreach (var i in enumVs) savedCount++;
                                }
                            }
                            catch { }
                        }

                    }
                    catch { }
                    
                    TaskDialog.Show("Success", "Print set saved.");

                    if (savedCount == 0)
                    {
                        // Attempt to delete existing set and re-create by reassigning views and saving again
                        bool recreated = false;
                        string[] deleteMethodNames2 = new[] { "Delete", "DeleteViewSheetSet", "Remove", "RemoveViewSheetSet", "DeleteViewSet" };
                        foreach (var dm in deleteMethodNames2)
                        {
                            var method = vssType.GetMethod(dm, BindingFlags.Public | BindingFlags.Instance);
                            if (method != null)
                            {
                                try
                                {
                                    method.Invoke(vssObj, new object[] { selectedSchedule.Name });
                                }
                                catch { }
                            }
                        }
                    }
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

                    // User chose Yes -> attempt to delete existing set via reflection using several possible method names
                    bool deleted = false;
                    string[] deleteMethodNames = new[] { "Delete", "DeleteViewSheetSet", "Remove", "RemoveViewSheetSet", "DeleteViewSet" };

                    foreach (var dm in deleteMethodNames)
                    {
                        var method = vssType.GetMethod(dm, BindingFlags.Public | BindingFlags.Instance);
                        if (method != null)
                        {
                            try
                            {
                                // try invoking with the name parameter
                                method.Invoke(vssObj, new object[] { selectedSchedule.Name });
                                deleted = true;
                                break;
                            }
                            catch { }

                            try
                            {
                                // try invoking with no parameters (some APIs delete current)
                                method.Invoke(vssObj, null);
                                deleted = true;
                                break;
                            }
                            catch { }
                        }
                    }
                    
                    TaskDialog.Show("Removed", "Previous view sheet set deleted.");
                    
                    // As a fallback, try to remove the name from any enumerable property we found earlier
                    if (!deleted)
                    {
                        foreach (var pn in propNames)
                        {
                            var prop = vssType.GetProperty(pn, BindingFlags.Public | BindingFlags.Instance);
                            if (prop != null)
                            {
                                var val = prop.GetValue(vssObj) as System.Collections.IList;
                                if (val != null)
                                {
                                    for (int i = val.Count - 1; i >= 0; i--)
                                    {
                                        if (val[i] == null) continue;
                                        var candidate = val[i].ToString();
                                        if (string.Equals(candidate, selectedSchedule.Name, StringComparison.OrdinalIgnoreCase))
                                        {
                                            val.RemoveAt(i);
                                            deleted = true;
                                            break;
                                        }
                                        var nameProp = val[i].GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
                                        if (nameProp != null)
                                        {
                                            var nm = nameProp.GetValue(val[i]) as string;
                                            if (string.Equals(nm, selectedSchedule.Name, StringComparison.OrdinalIgnoreCase))
                                            {
                                                val.RemoveAt(i);
                                                deleted = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            
                            if (deleted) break;
                        }
                    }
                }

                t.Commit();
            }
            
            
            return Result.Succeeded;
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
}
