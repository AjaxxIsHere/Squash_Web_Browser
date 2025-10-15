using Avalonia.Controls;
using Squash_Web_Browser.Models;
using Squash_Web_Browser.ViewModels;

namespace Squash_Web_Browser.Controls;

public partial class HistoryPanel : UserControl
{
    public HistoryPanel()
    {
        InitializeComponent();
    }

    private void HistoryList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is History selectedHistoryItem)
        {
            if (DataContext is MainWindowViewModel vm && vm.HistoryClickCommand.CanExecute(selectedHistoryItem))
            {
                vm.HistoryClickCommand.Execute(selectedHistoryItem);
            }
        }
    }
}

