using System;
using System.Collections.Generic;
using Squash_Web_Browser.Models;

namespace Squash_Web_Browser.Services;

public interface IBookmarkService
{
    IReadOnlyList<Bookmark> LoadBookmarks();
    BookmarkOperationResult AddBookmark(string name, string url, bool enforceUrlValidation = false);
    void DeleteBookmark(int id);
    bool IsValidUrl(string url);
}

// read only record struct to represent the result of bookmark operations
public readonly record struct BookmarkOperationResult(bool Success, string? ErrorMessage);

/* 
Summary: This section defines a bookmark service interface (IBookmarkService) and its implementation (BookmarkService) for managing bookmarks in a web browser context.
The BookmarkService class interacts with a storage service (IStorageService) to load, add, and delete bookmarks. Its methods include:

- BookmarkService(IStorageService storageService): Constructor that accepts a storage service dependency for data persistence.
- LoadBookmarks(): Loads the bookmarks from storage and returns them as a read-only list of Bookmark objects.
- AddBookmark(string name, string url, bool enforceUrlValidation = false): Adds a new bookmark after validating the name and URL, returning a result indicating success or failure.
- DeleteBookmark(int id): Deletes a bookmark by its ID.
- IsValidUrl(string url): Validates whether a given URL is well-formed and uses the HTTP or HTTPS scheme.

*/
public sealed class BookmarkService : IBookmarkService
{
    private readonly IStorageService _storageService;

    public BookmarkService(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public IReadOnlyList<Bookmark> LoadBookmarks() => _storageService.LoadBookmarks();

    public BookmarkOperationResult AddBookmark(string name, string url, bool enforceUrlValidation = false)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        var trimmedUrl = url?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedName) || string.IsNullOrWhiteSpace(trimmedUrl))
        {
            return new BookmarkOperationResult(false, "Bookmark name and URL cannot be empty.");
        }

        if (enforceUrlValidation && !IsValidUrl(trimmedUrl))
        {
            return new BookmarkOperationResult(false, "Invalid URL. Please enter a full HTTP/HTTPS address.");
        }

        _storageService.SaveBookmark(trimmedName, trimmedUrl);
        return new BookmarkOperationResult(true, null);
    }

    public void DeleteBookmark(int id) => _storageService.DeleteBookmark(id);

    public bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
