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

    /* 
    Summary: This method handles the selection change event in the history list. When a user selects a history item, it checks if the DataContext is of type MainWindowViewModel and if the HistoryClickCommand can be executed with the selected history item. If so, it executes the command, allowing navigation to the selected URL.
    */
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

