using System;
using System.Collections.ObjectModel;
using Squash_Web_Browser.Models;

namespace Squash_Web_Browser.Services;

public interface IHistoryService
{
    ObservableCollection<History> LoadHistory();
    void SaveHistory(string url);
}

/*
Summary: This section defines a history service interface (IHistoryService) and its implementation (HistoryService) for managing browsing history in a web browser context.
The HistoryService class interacts with a storage service (IStorageService) to load and save browsing history. Its methods include:

- HistoryService(IStorageService storageService): Constructor that accepts a storage service dependency for data persistence.
- LoadHistory(): Loads the browsing history from storage and returns it as an observable collection of History objects.
- SaveHistory(string url): Saves a new URL to the browsing history in storage.
- NormalizeUrl(string url): A private helper method that normalizes a given URL by ensuring it has the correct scheme (http/https) and formatting.

*/
public class HistoryService : IHistoryService
{
    // Dependency on a storage service to handle the actual data persistence
    private readonly IStorageService _storageService;

    // Constructor to inject the storage service dependency
    public HistoryService(IStorageService storageService)
    {
        _storageService = storageService;
    }


    public ObservableCollection<History> LoadHistory()
    {
        var history = _storageService.LoadHistory();
        return new ObservableCollection<History>(history);
    }

    public void SaveHistory(string url)
    {
        _storageService.SaveHistory(UrlHelper.NormalizeUrl(url));
    }
}
