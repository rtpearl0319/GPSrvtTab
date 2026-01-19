using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using ComboBox = System.Windows.Controls.ComboBox;
using Grid = System.Windows.Controls.Grid;
using TextBox = System.Windows.Controls.TextBox;

namespace ParameterRemapperUI
{
    public partial class MainWindow : Window
    {
        public UIDocument uiDoc;

        private ParameterRemapperEventHandler parameterEventHandler;
        private ExternalEvent parameterExternalEvent;

        public MainWindow(UIDocument uiDoc)
        {
            this.uiDoc = uiDoc;

            this.parameterEventHandler = new ParameterRemapperEventHandler(uiDoc.Document);
            this.parameterExternalEvent = ExternalEvent.Create(parameterEventHandler);

            InitializeComponent();
            this.Topmost = true;

            List<ComboBox> comboBoxes = new List<ComboBox>();
            List<TextBox> separators = new List<TextBox>();

            foreach (UIElement optionGridChild in OptionGrid.Children)
            {
                if (Grid.GetColumn(optionGridChild) == 1)
                {
                    var currentComboBox = optionGridChild as ComboBox;
                    if (currentComboBox != null)
                    {
                        comboBoxes.Add(currentComboBox);
                    }
                }

                if (Grid.GetColumn(optionGridChild) == 2)
                {
                    var currentSeparator = optionGridChild as TextBox;
                    if (currentSeparator != null)
                    {
                        separators.Add(currentSeparator);
                    }
                }
            }

            var parameters = GetSharedElementParameters(GetElements(this.uiDoc.Document,
                BuiltInCategory.OST_ElectricalFixtures,
                BuiltInCategory.OST_SecurityDevices,
                BuiltInCategory.OST_DataDevices));

            foreach (var param in parameters)
            {
                foreach (ComboBox cb in comboBoxes)
                {
                    cb.Items.Add(param);
                }
            }
        }

        private IList<Element> GetElements(Document doc, params BuiltInCategory[] categories)
        {
            var elements = new List<Element>();

            foreach (var category in categories)
            {
                var collector = new FilteredElementCollector(doc).OfCategory(category).ToElements();
                elements.AddRange(collector);
            }
            return elements;
        }

        private ISet<string> GetSharedElementParameters(IList<Element> elements)
        {
            var parameterNamesFound = new HashSet<string>();
            var parameterNamesShared = new SortedSet<string>();

            foreach (Element elem in elements)
            {
                foreach (Parameter param in elem.Parameters)
                {
                    if (!parameterNamesFound.Add(param.Definition.Name))
                    {
                        parameterNamesShared.Add(param.Definition.Name);
                    }
                }
            }
            parameterNamesShared.Add("");
            return parameterNamesShared;
        }

        private List<ElectricalSystem> GetElectricalSystems(IList<Element> elements)
        {
            var electricalSystems = new List<ElectricalSystem>();

            foreach (Element element in elements)
            {
                if (!(element is FamilyInstance familyInstance))
                {
                    continue;
                }

#if REVIT2020 || REVIT2021
                     foreach (ElectricalSystem electricalSystem in familyInstance.MEPModel.ElectricalSystems)
                     {
#else
                foreach (ElectricalSystem electricalSystem in familyInstance.MEPModel.GetElectricalSystems())
                {
#endif
                    if (electricalSystem.Elements.Size != 1)
                    {
                        TaskDialog.Show("Error",
                            "Circuit system must have exactly one element" + '\n' + "Element ID: " + familyInstance.Id);
                        continue;
                    }

                    electricalSystems.Add(electricalSystem);
                }
            }
            return electricalSystems;
        }

        public ParamInfo[] WpfParameterInfos()
        {
            var paramInfos = new ParamInfo[OptionGrid.RowDefinitions.Count - 2];
            for (var i = 0; i < paramInfos.Length; i++)
            {
                paramInfos[i] = new ParamInfo(); // Set default value in array
            }

            foreach (UIElement optionGridChild in OptionGrid.Children)
            {
                if (Grid.GetColumn(optionGridChild) == 1)
                {
                    if (optionGridChild is ComboBox currentComboBox)
                    {
                        paramInfos[Grid.GetRow(optionGridChild)].name = currentComboBox.Text;
                    }
                }
                else if (Grid.GetColumn(optionGridChild) == 2)
                {
                    if (optionGridChild is TextBox currentSeparator)
                    {
                        paramInfos[Grid.GetRow(optionGridChild)].separator = currentSeparator.Text;
                    }
                }
            }
            return paramInfos;
        }

        private void OnDropDownClosedOnSelectionChanged(object sender, EventArgs e)
        {
            Concatination.Text = JoinParamInfos(WpfParameterInfos(), null);
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Concatination.Text = JoinParamInfos(WpfParameterInfos(), null);
        }

        private void SubmitButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var allElements = GetElements(
                    uiDoc.Document,
                    BuiltInCategory.OST_ElectricalFixtures,
                    BuiltInCategory.OST_SecurityDevices,
                    BuiltInCategory.OST_DataDevices
                );


                var paramUpdates = new List<ParamInfoUpdate>();

                //Loop through the elements in the collection
                foreach (var electricalSystem in GetElectricalSystems(allElements))
                {
                    if (electricalSystem == null)
                    {
                        continue;
                    }

                    if (electricalSystem.Elements.Size != 1)
                    {
                        TaskDialog.Show("Error", "Electrical system must have exactly one element");
                        return;
                    }

                    var elementIterator = electricalSystem.Elements.ForwardIterator();
                    elementIterator.MoveNext();
                    var familyInstance = elementIterator.Current as FamilyInstance;

                    paramUpdates.Add(new ParamInfoUpdate(electricalSystem,
                        JoinParamInfos(WpfParameterInfos(), familyInstance)));
                }

                parameterEventHandler.SetParamInfoUpdates(paramUpdates);
                parameterExternalEvent.Raise();
            }
            catch(Exception)
            {
                TaskDialog.Show("Error", "Failed to edit circuit names");
            }
        }

        public string JoinParamInfos(ParamInfo[] paramInfos, FamilyInstance familyInstance)
        {
            StringBuilder result = new StringBuilder();
            string previousSeparator = string.Empty;

            foreach (var paramInfo in paramInfos)
            {
                if (string.IsNullOrEmpty(paramInfo.name))
                {
                    continue;
                }

                result.Append(previousSeparator);
                previousSeparator = string.Empty;

                var newValue = string.Empty;

                if (familyInstance != null)
                {
                    var parameter = familyInstance.LookupParameter(paramInfo.name);

                    if (parameter == null)
                    {
                        parameter = familyInstance.Symbol.LookupParameter(paramInfo.name);
                    }

                    if (parameter != null)
                    {
                        newValue = parameter.AsValueString();
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(paramInfo.name))
                    {
                        newValue = $"[{paramInfo.name}]";
                    }
                }
                result.Append(newValue);
                
                if (!string.IsNullOrEmpty(newValue))
                {
                    previousSeparator = paramInfo.separator;
                }
            }
            return result.ToString();
        }

        // Builder pattern
        public class ParamInfo
        {
            public string name, separator;
        }

        public class ParamInfoUpdate
        {
            private ElectricalSystem electricalSystem;
            private string newValue;

            public ParamInfoUpdate(ElectricalSystem electricalSystem, string newValue)
            {
                this.electricalSystem = electricalSystem;
                this.newValue = newValue;
            }

            public ElectricalSystem GetElectricalSystem()
            {
                return electricalSystem;
            }

            public string GetNewValue()
            {
                return newValue;
            }
        }
    }
}

    