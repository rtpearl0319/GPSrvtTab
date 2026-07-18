using System.Windows;
using Autodesk.Revit.DB;

namespace GPSrvtTab.Extensions.Windows;

public partial class DeleteFamilySymbolWindow : Window
{
    private readonly List<SelectableFamilySymbol> _items;

    public List<FamilySymbol> SelectedSymbols { get; private set; } = new();

    public DeleteFamilySymbolWindow(List<FamilySymbol> annotationSymbols)
    {
        InitializeComponent();

        _items = annotationSymbols
            .OrderBy(symbol => symbol.Family.Name)
            .ThenBy(symbol => symbol.Name)
            .Select(symbol => new SelectableFamilySymbol(symbol))
            .ToList();

        SymbolListBox.ItemsSource = _items;
    }

    private void DeleteButton_OnClick(object sender, RoutedEventArgs e)
    {
        SelectedSymbols = _items.Where(item => item.IsSelected).Select(item => item.Symbol).ToList();

        if (SelectedSymbols.Count == 0)
        {
            MessageBox.Show("Select at least one annotation symbol to delete.", "Delete Annotation Symbols");
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

public class SelectableFamilySymbol
{
    public FamilySymbol Symbol { get; }
    public string Name { get; }
    public bool IsSelected { get; set; }

    public SelectableFamilySymbol(FamilySymbol symbol)
    {
        Symbol = symbol;
        Name = $"{symbol.Family.Name} : {symbol.Name}";
    }
}
