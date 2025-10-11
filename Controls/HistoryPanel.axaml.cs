using Avalonia.Controls;
using Avalonia.Input;
using Squash_Web_Browser.Models;
using Squash_Web_Browser.Services;
using Squash_Web_Browser.ViewModels;

namespace Squash_Web_Browser.Controls;

public partial class HistoryPanel : UserControl
{
    private readonly IStorageService _storageService = new StorageService();
    private ListBox? _historyListBox;
    public HistoryPanel()
    {
        InitializeComponent();
        _historyListBox = this.FindControl<ListBox>("HistoryList");
        if (_historyListBox != null)
        {
            LoadHistory();
            _historyListBox.PointerPressed += HistoryListBoxOnPointerPressed;
        }
    }

    private void HistoryListBoxOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_historyListBox != null && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && _historyListBox.SelectedItem is History selectedHistory)
        {
            if (this.DataContext is MainWindowViewModel vm)
            {
                vm.Address = selectedHistory.Url;
                vm.FetchHtmlCommand.Execute(null);
            }
        }
    }

    private void LoadHistory()
    {
        if(_historyListBox == null) return;
        var history = _storageService.LoadHistory();
        _historyListBox.ItemsSource = history;
    }
}
