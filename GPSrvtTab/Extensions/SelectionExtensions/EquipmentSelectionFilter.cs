using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace GPSrvtTab.Extensions.SelectionExtensions
{
    public class EquipmentSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            #if REVIT2022 || REVIT2020 || REVIT2021
                return elem.Category.Name == "Electrical Equipment";
            #else
                return elem.Category.BuiltInCategory == BuiltInCategory.OST_ElectricalEquipment;
            #endif
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}