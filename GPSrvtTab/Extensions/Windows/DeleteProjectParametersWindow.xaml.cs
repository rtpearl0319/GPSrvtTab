using System.Windows;
using Autodesk.Revit.DB;

namespace GPSrvtTab.Extensions.Windows;

public partial class DeleteProjectParametersWindow : Window
{
    private readonly List<SelectableParameter> _items;

    public List<Definition> SelectedParameters { get; private set; } = new();

    public DeleteProjectParametersWindow(List<Definition> projectParameters)
    {
        InitializeComponent();

        _items = projectParameters
            .OrderBy(definition => definition.Name)
            .Select(definition => new SelectableParameter(definition))
            .ToList();

        ParameterListBox.ItemsSource = _items;
    }

    private void DeleteButton_OnClick(object sender, RoutedEventArgs e)
    {
        SelectedParameters = _items.Where(item => item.IsSelected).Select(item => item.Definition).ToList();

        if (SelectedParameters.Count == 0)
        {
            MessageBox.Show("Select at least one project parameter to delete.", "Delete Project Parameters");
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public class SelectableParameter
{
    public Definition Definition { get; }
    public string Name { get; }
    public bool IsSelected { get; set; }

    public SelectableParameter(Definition definition)
    {
        Definition = definition;
        Name = definition.Name;
    }
}
