using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace GPSrvtTab.Extensions.SelectionExtensions
{
    public class FixtureSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            #if REVIT2022 || REVIT2021 || REVIT2020
            return elem.Category.Name == "Data Devices" ||
                   elem.Category.Name == "Security Devices" ||
                   elem.Category.Name == "Communication Devices" ||
                   elem.Category.Name == "Electrical Fixtures";
#else
                return elem.Category.BuiltInCategory == BuiltInCategory.OST_DataDevices || 
                       elem.Category.BuiltInCategory == BuiltInCategory.OST_SecurityDevices ||
                       elem.Category.BuiltInCategory == BuiltInCategory.OST_CommunicationDevices ||
                       elem.Category.BuiltInCategory == BuiltInCategory.OST_ElectricalFixtures;
#endif
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}