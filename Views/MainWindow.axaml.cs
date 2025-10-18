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
            addressBar.DataContext = DataContext;
            var toggleButton = addressBar.FindControl<Button>("SidebarToggleButton");
            if (toggleButton != null)
            {
                toggleButton.Click += ToggleSidebar;
            }
        }

        var editHomePagePanel = this.FindControl<EditHomePagePanel>("EditHomePagePanel");
        if (editHomePagePanel != null)
        {
            editHomePagePanel.DataContext = DataContext;
        }
        
        var bookmarksPanel = this.FindControl<BookmarksPanel>("BookmarksPanel");
        if (bookmarksPanel != null)
        {
            bookmarksPanel.DataContext = DataContext;
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