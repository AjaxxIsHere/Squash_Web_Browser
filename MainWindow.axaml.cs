using Avalonia.Controls;
using Avalonia.Interactivity;
using Squash_Web_Browser.Controls;
using Squash_Web_Browser.ViewModels;

namespace Squash_Web_Browser;

public partial class MainWindow : Window
{
    private bool _isSidebarVisible = true;
    
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        
        var addressBar = this.FindControl<AddressBar>("AddressBar");
        if (addressBar != null)
        {
            var sidebarToggleButton = addressBar.FindControl<Button>("SidebarToggleButton");
            if (sidebarToggleButton != null)
            {
                sidebarToggleButton.Click += ToggleSidebar;
            }
        }
    }

    private void ToggleSidebar(object? sender, RoutedEventArgs e)
    {
        var mainGrid = this.FindControl<Grid>("MainGrid");
        var sidebar = this.FindControl<TabControl>("SidebarTabControl");

        _isSidebarVisible = !_isSidebarVisible;

        if (mainGrid != null && sidebar != null)
        {
            if (_isSidebarVisible)
            {
                mainGrid.ColumnDefinitions[0].Width = new GridLength(500);
                sidebar.IsVisible = true;
            }
            else
            {
                mainGrid.ColumnDefinitions[0].Width = new GridLength(0);
                sidebar.IsVisible = false;
            }
        }
    }
}