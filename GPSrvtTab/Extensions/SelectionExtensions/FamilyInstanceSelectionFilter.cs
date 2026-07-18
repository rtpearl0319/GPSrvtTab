using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace GPSrvtTab.Extensions.SelectionExtensions
{
    public class FamilyInstanceSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is FamilyInstance;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}
