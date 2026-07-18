using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GPSrvtTab.Extensions.Windows;

namespace GPSrvtTab;

[Transaction(TransactionMode.Manual)]
public class DeleteProjectParameters : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIApplication uiapp = commandData.Application;
        UIDocument uidoc = uiapp.ActiveUIDocument;
        Document doc = uidoc.Document;

        BindingMap bindingMap = doc.ParameterBindings;

        List<Definition> projectParameters = new List<Definition>();
        DefinitionBindingMapIterator iterator = bindingMap.ForwardIterator();
        while (iterator.MoveNext())
        {
            projectParameters.Add(iterator.Key);
        }

        if (projectParameters.Count == 0)
        {
            TaskDialog.Show("Delete Project Parameters", "No project parameters were found in this model.");
            return Result.Cancelled;
        }

        DeleteProjectParametersWindow window = new DeleteProjectParametersWindow(projectParameters);
        bool? dialogResult = window.ShowDialog();

        if (dialogResult != true || window.SelectedParameters.Count == 0)
        {
            return Result.Cancelled;
        }

        List<string> notRemoved = new List<string>();

        using (Transaction t = new Transaction(doc, "Delete Project Parameters"))
        {
            t.Start();

            foreach (Definition definition in window.SelectedParameters)
            {
                // Since Revit 2017, a project parameter is backed by a ParameterElement in the
                // document. BindingMap.Remove only clears the category binding, so the element
                // (and therefore the parameter itself) must be deleted directly to remove it.
                if (definition is InternalDefinition internalDefinition
                    && internalDefinition.Id != ElementId.InvalidElementId
                    && doc.GetElement(internalDefinition.Id) != null)
                {
                    doc.Delete(internalDefinition.Id);
                }
                else if (!bindingMap.Remove(definition))
                {
                    notRemoved.Add(definition.Name);
                }
            }

            t.Commit();
        }

        if (notRemoved.Count > 0)
        {
            TaskDialog.Show("Delete Project Parameters",
                "The following parameter(s) could not be removed. They may still be referenced by a schedule, " +
                $"a type/family formula, or a global parameter:\n\n{string.Join("\n", notRemoved)}");
        }

        return Result.Succeeded;
    }
}
