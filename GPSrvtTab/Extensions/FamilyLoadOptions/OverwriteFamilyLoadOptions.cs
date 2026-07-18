using Autodesk.Revit.DB;

namespace GPSrvtTab.Extensions.FamilyLoadOptions
{
    // Overwrites the existing family version but keeps overwriteParameterValues false
    // so instance/type parameter values already set in the project are preserved.
    public class OverwriteFamilyLoadOptions : IFamilyLoadOptions
    {
        public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
        {
            overwriteParameterValues = false;
            return true;
        }

        public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source,
            out bool overwriteParameterValues)
        {
            source = FamilySource.Family;
            overwriteParameterValues = false;
            return true;
        }
    }
}
