using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace ParameterRemapperUI
{
    public partial class MainWindow : Window
    {
        public UIDocument uiDoc;
        
        public MainWindow(UIDocument uiDoc)
        {
            this.uiDoc = uiDoc;
            InitializeComponent();
            this.Topmost = true;
        }
        private void SelectElement(object sender, RoutedEventArgs e)
        {
            ParamListView1.Items.Clear();
            ParamListView2.Items.Clear();
            ParamListView3.Items.Clear();
            ParamListView4.Items.Clear();
            ParamListView5.Items.Clear();
            Reference pickedObj = uiDoc.Selection.PickObject(ObjectType.Element);
            Element selectedElem = uiDoc.Document.GetElement(pickedObj);
            
            foreach (Parameter param in selectedElem.ParametersMap)
            {
                 ParamListView1.Items.Add(param.Definition.Name);
                 ParamListView2.Items.Add(param.Definition.Name);
                 ParamListView3.Items.Add(param.Definition.Name);
                 ParamListView4.Items.Add(param.Definition.Name);
                 ParamListView5.Items.Add(param.Definition.Name);
                 
                 // ParamValueListView.Items.Add(selectedElem.LookupParameter(param.Definition.Name).AsValueString());
            }
        }
    }
}