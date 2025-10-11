using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Squash_Web_Browser.Models;

namespace Squash_Web_Browser.Controls;

public partial class BookmarksPanel : UserControl
{
    private TextBox? _nameTextBox;
    private TextBox? _urlTextBox;
    private Button? _addButton;

    private ListBox? _bookmarksListBox;
    private readonly Services.IStorageService _storageService = new Services.StorageService();

    public BookmarksPanel()
    {
        InitializeComponent();

        _nameTextBox = this.FindControl<TextBox>("NameTextBox");
        _urlTextBox = this.FindControl<TextBox>("UrlTextBox");
        _addButton = this.FindControl<Button>("AddBookmarkButton");
        if (_addButton != null)
            _addButton.Click += AddButton_Click;

        _bookmarksListBox = this.FindControl<ListBox>("BookmarksList");
        LoadBookmarks();
    }

    private void DeleteBookmark_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Bookmark bookmark)
        {
            _storageService.DeleteBookmark(bookmark.Id);
            LoadBookmarks();
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

            // Refresh the list
            LoadBookmarks();
        }
    }
    
    private void InputFields_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var hasText = !string.IsNullOrWhiteSpace(_nameTextBox?.Text) || !string.IsNullOrWhiteSpace(_urlTextBox?.Text);
        if (_addButton != null)
        {
            _addButton.IsEnabled = hasText && IsValidUrl(_urlTextBox?.Text ?? string.Empty);
        }

        if (_bookmarksListBox?.Items == null) return;
        
        // This is a bit tricky in Avalonia without direct access to the generated containers.
        // A common approach is to use a ViewModel and bind the IsEnabled property.
        // For this code-behind approach, we'll just disable adding if text exists.
        // Disabling edit buttons dynamically from here is complex.
        // A simpler UX is to just not let them edit if the fields are full. The check at the start of EditBookmark_Click handles this.
    }


    private void LoadBookmarks()
    {
	    if (_bookmarksListBox == null) return;
	    var bookmarks = _storageService.LoadBookmarks();
	    _bookmarksListBox.ItemsSource = bookmarks;
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

        LoadBookmarks();

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
