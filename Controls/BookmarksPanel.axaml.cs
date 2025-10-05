using System.Collections.ObjectModel;
using System;
using Avalonia.Controls;

namespace Squash_Web_Browser.Controls;

public partial class BookmarksPanel : UserControl
{
    private TextBox? _nameTextBox;
    private TextBox? _urlTextBox;
    private Button? _addButton;

    private ListBox? _bookmarksListBox;
    private ObservableCollection<string> _bookmarkItems = new ObservableCollection<string>();
    private readonly Services.IStorageService _storageService = new Services.StorageService();

    public BookmarksPanel()
    {
        InitializeComponent();

        // Find controls by name (assumes you add x:Name to the TextBoxes and Button in XAML)
        _nameTextBox = this.FindControl<TextBox>("NameTextBox");
        _urlTextBox = this.FindControl<TextBox>("UrlTextBox");
        _addButton = this.FindControl<Button>("AddBookmarkButton");
        if (_addButton != null)
            _addButton.Click += AddButton_Click;

        _bookmarksListBox = this.FindControl<ListBox>("BookmarksList");
        if (_bookmarksListBox != null)
        {
            _bookmarksListBox.ItemsSource = _bookmarkItems;
        }
    }

    private void AddButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var name = _nameTextBox?.Text ?? string.Empty;
        var url = _urlTextBox?.Text ?? string.Empty;
        _storageService.SaveBookmark(name, url);

        // Print all bookmarks to the console after saving
        var allBookmarks = (_storageService as Services.StorageService)?.LoadAllBookmarks();
        if (allBookmarks != null)
        {
            Console.WriteLine("All Bookmarks:");
            foreach (var (bName, bUrl) in allBookmarks)
            {
                Console.WriteLine($"Name: {bName}, URL: {bUrl}");
            }
        }

        // Add "Hello World" to the ListBox
        _bookmarkItems.Add("Hello World");
    }
}
