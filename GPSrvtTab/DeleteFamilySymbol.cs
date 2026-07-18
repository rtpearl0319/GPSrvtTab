using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using GPSrvtTab.Extensions.FamilyLoadOptions;
using GPSrvtTab.Extensions.SelectionExtensions;
using GPSrvtTab.Extensions.Windows;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

namespace GPSrvtTab;

[Transaction(TransactionMode.Manual)]
public class DeleteFamilySymbol : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIApplication uiapp = commandData.Application;
        UIDocument uidoc = uiapp.ActiveUIDocument;
        Document doc = uidoc.Document;

        Reference pickedReference;
        try
        {
            //Pick The Element Whose Family Will Be Edited
            pickedReference = uidoc.Selection.PickObject(ObjectType.Element,
                new FamilyInstanceSelectionFilter(),
                "Select An Element To Edit Its Family's Annotation Symbols");
        }
        catch (OperationCanceledException)
        {
            return Result.Cancelled;
        }

        if (doc.GetElement(pickedReference) is not FamilyInstance familyInstance)
        {
            TaskDialog.Show("Delete Annotation Symbols", "Selected element is not a family instance.");
            return Result.Cancelled;
        }

        Family family = familyInstance.Symbol.Family;

        if (!family.IsEditable)
        {
            TaskDialog.Show("Delete Annotation Symbols", $"{family.Name} cannot be edited.");
            return Result.Failed;
        }

        Document familyDoc = doc.EditFamily(family);

        List<FamilySymbol> annotationSymbols = new FilteredElementCollector(familyDoc)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .Where(symbol => symbol.Category is { CategoryType: CategoryType.Annotation })
            .ToList();

        if (annotationSymbols.Count == 0)
        {
            TaskDialog.Show("Delete Annotation Symbols", $"No annotation symbols were found in {family.Name}.");
            familyDoc.Close(false);
            return Result.Cancelled;
        }

        DeleteFamilySymbolWindow window = new DeleteFamilySymbolWindow(annotationSymbols);
        bool? dialogResult = window.ShowDialog();

        if (dialogResult != true || window.SelectedSymbols.Count == 0)
        {
            familyDoc.Close(false);
            return Result.Cancelled;
        }

        using (Transaction t = new Transaction(familyDoc, "Delete Annotation Symbols"))
        {
            t.Start();

            foreach (FamilySymbol symbol in window.SelectedSymbols)
            {
                if (familyDoc.GetElement(symbol.Id) != null)
                {
                    familyDoc.Delete(symbol.Id);
                }
            }

            t.Commit();
        }

        //Load the edited family back into the project without saving the family file,
        //overwriting the existing version but keeping the project's parameter values.
        familyDoc.LoadFamily(doc, new OverwriteFamilyLoadOptions());
        familyDoc.Close(false);

        return Result.Succeeded;
    }
}
