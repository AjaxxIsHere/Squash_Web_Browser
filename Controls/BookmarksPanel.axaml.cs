using System;
using Avalonia.Controls;
using Squash_Web_Browser.Models;

namespace Squash_Web_Browser.Controls;

/*
Summary: This code defines a BookmarksPanel control for a web browser application using Avalonia UI framework. The panel allows users to manage bookmarks, including adding, editing, deleting, and selecting bookmarks to navigate to their URLs. The key functionalities include:

- Adding a new bookmark with validation for URL format.
- Deleting existing bookmarks and refreshing the displayed list.
- Editing bookmarks by loading their data into input fields and removing them from storage.

Methods:
- AddButton_Click: Handles adding a new bookmark after validating the URL.
- DeleteBookmark_Click: Handles deleting a bookmark and refreshing the list.
- EditBookmark_Click: Handles editing a bookmark by loading its data into input fields and deleting it
- InputFields_OnTextChanged: Manages the state of the Add button based on input field content.
- Bookmark_PointerPressed: Navigates to the bookmark's URL when clicked.

*/
public partial class BookmarksPanel : UserControl
{
    private TextBox? _nameTextBox;
    private TextBox? _urlTextBox;
    private Button? _addButton;
    private readonly Services.IStorageService _storageService = new Services.StorageService();

    public BookmarksPanel()
    {
        InitializeComponent();

        _nameTextBox = this.FindControl<TextBox>("NameTextBox");
        _urlTextBox = this.FindControl<TextBox>("UrlTextBox");
        _addButton = this.FindControl<Button>("AddBookmarkButton");
        if (_addButton != null)
            _addButton.Click += AddButton_Click;

        // Bookmarks list is bound to the VM's ObservableCollection via XAML (ItemsSource="{Binding Bookmarks}").
        // We must NOT overwrite ItemsSource from code-behind.
    }

    private void DeleteBookmark_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Bookmark bookmark)
        {
            _storageService.DeleteBookmark(bookmark.Id);
            // Refresh the VM's ObservableCollection so the binding updates
            ReloadVmBookmarks();
        }
    }

    private void EditBookmark_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Prevent editing if textboxes have content
        if (!string.IsNullOrWhiteSpace(_nameTextBox?.Text) || !string.IsNullOrWhiteSpace(_urlTextBox?.Text))
        {
            return;
        }

        if (sender is Button { DataContext: Bookmark bookmark })
        {
            // Load bookmark data into textboxes
            if (_nameTextBox != null) _nameTextBox.Text = bookmark.Name;
            if (_urlTextBox != null) _urlTextBox.Text = bookmark.Url;

            // Delete from DB
            _storageService.DeleteBookmark(bookmark.Id);

            // Refresh the VM's ObservableCollection so the binding updates
            ReloadVmBookmarks();
        }
    }
    
    private void InputFields_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var hasText = !string.IsNullOrWhiteSpace(_nameTextBox?.Text) || !string.IsNullOrWhiteSpace(_urlTextBox?.Text);
        if (_addButton != null)
        {
            _addButton.IsEnabled = hasText && IsValidUrl(_urlTextBox?.Text ?? string.Empty);
        }

        // This is a bit tricky in Avalonia without direct access to the generated containers.
        // A common approach is to use a ViewModel and bind the IsEnabled property.
        // For this code-behind approach, we'll just disable adding if text exists.
        // Disabling edit buttons dynamically from here is complex.
        // A simpler UX is to just not let them edit if the fields are full. The check at the start of EditBookmark_Click handles this.
    }

    // Reloads the VM-backed Bookmarks collection without rebinding the ListBox
    private void ReloadVmBookmarks()
    {
        if (this.DataContext is ViewModels.MainWindowViewModel vm)
        {
            var bookmarks = _storageService.LoadBookmarks();
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                vm.Bookmarks.Clear();
                foreach (var b in bookmarks)
                {
                    vm.Bookmarks.Add(b);
                }
            });
        }
    }

    private void AddButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var name = _nameTextBox?.Text ?? string.Empty;
        var url = _urlTextBox?.Text ?? string.Empty;

        // Validate URL
        if (!IsValidUrl(url))
        {
            // Optionally, show a message to the user (for now, just return)
            return;
        }

        _storageService.SaveBookmark(name, url);

        // Clear the text fields after adding
        if (_nameTextBox != null) _nameTextBox.Text = string.Empty;
        if (_urlTextBox != null) _urlTextBox.Text = string.Empty;

        ReloadVmBookmarks();

    }

    private bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (Uri.TryCreate(url, UriKind.Absolute, out var uriResult))
        {
            return uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps;
        }
        return false;
    }

    private void Bookmark_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is TextBlock { DataContext: Bookmark bookmark })
        {
            // Find the MainWindowViewModel from the DataContext of the parent window
            var mainWindow = (this.Parent?.Parent?.Parent?.Parent?.Parent as Window);
            if (mainWindow?.DataContext is ViewModels.MainWindowViewModel vm)
            {
                vm.Address = bookmark.Url;
                if (vm.FetchHtmlCommand.CanExecute(null))
                {
                    vm.FetchHtmlCommand.Execute(null);
                }
            }
        }
    }
}
