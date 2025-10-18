using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Squash_Web_Browser.Services;
using Squash_Web_Browser.Models;

namespace Squash_Web_Browser.ViewModels;

/*
Summary: This code defines the MainWindowViewModel class, which serves as the ViewModel for the main window of a web browser application. It manages the state and behavior of the UI, including fetching HTML content, parsing it, and handling navigation. Key features include:

- Properties for the current URL, home page URL, HTML source, status messages, and parsed links.
- Commands for fetching HTML, toggling HTML visibility, navigating back and forward, going to the home page, saving the home page URL, and handling history item clicks.
- Integration with services for web requests, storage, HTML parsing, navigation, and history management.

Methods include:
- FetchHtmlAsync(bool isNewNavigation): Asynchronously fetches HTML content from the specified URL and updates the UI accordingly.
- ParseHtml(string html, string baseUrl): Parses the fetched HTML to extract the page title and links.
- ClearParsed(): Clears the parsed state when an error occurs or no HTML is available.
- GoHomeAsync(), GoBackAsync(), GoForwardAsync(): Navigation methods to handle home, back, and forward actions.

*/
public class MainWindowViewModel : ViewModelBase
{
    private readonly IWebService _webService;
    private readonly IStorageService _settingsService;
    private readonly IHtmlParser _htmlParser;
    private readonly INavService _navService;
    private readonly IHistoryService _historyService;

    // public static string Title => "Squash Browser"; // Application title
    private string _address;                        // current URL
    private const string DbFile = "browserdata.db"; // still referenced by default settings service
    private string _htmlSrc = string.Empty;         // holds fetched HTML
    private string _status = "Idle";                // status messages (success/error/loading)
    private bool _isBusy;                           // indicates fetch in progress       
    private string _homePageUrl = string.Empty;     // home page URL
    private string _pageTitle = string.Empty;       // parsed <title>          
    private bool _showHtml = true;                  // controls visibility of raw HTML panel

    // When to add an item to or remove an item from an ObservableCollection, it automatically notifies the user interface, which then updates itself to reflect the change.
    public ObservableCollection<ParsedLink> Links { get; } = new();
    public ObservableCollection<History> History { get; } = new();
    public ObservableCollection<Bookmark> Bookmarks { get; } = new();
    public ICommand FetchHtmlCommand { get; }
    public ICommand ToggleHtmlCommand { get; }
    public ICommand LinkClickCommand { get; }
    public ICommand BackButtonCommand { get; }
    public ICommand ForwardButtonCommand { get; }
    public ICommand HomeButtonCommand { get; }
    public ICommand SaveHomePageCommand { get; }
    public ICommand HistoryClickCommand { get; }
    public ICommand ShowBookmarkFlyoutCommand { get; }
    public ICommand AddBookmarkCommand { get; }
    public ICommand CancelBookmarkCommand { get; }

    public string Address
    {
        get => _address;
        set
        {
            // Normalize and save last URL to DB
            if (value != _address)
            {
                _address = value;
                RaisePropertyChanged();
                SaveLastUrl(_address);
            }
        }
    }

    // The URL for the home page
    public string HomePageUrl
    {
        get => _homePageUrl;
        set
        {
            if (value != _homePageUrl)
            {
                _homePageUrl = value;
                RaisePropertyChanged();
            }
        }
    }

    // The fetched HTML source code
    public string HtmlSource
    {
        get => _htmlSrc;
        private set
        {
            // Update only if changed to avoid unnecessary UI updates
            if (value != _htmlSrc)
            {
                _htmlSrc = value;
                RaisePropertyChanged();
            }
        }
    }

    // Status message for the UI
    public string Status
    {
        get => _status;
        private set
        {
            // Update only if changed to avoid unnecessary UI updates
            if (value != _status)
            {
                _status = value;
                RaisePropertyChanged();
            }
        }
    }

    // Indicates if a fetch operation is in progress
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (value != _isBusy)
            {
                _isBusy = value;
                RaisePropertyChanged();
                // also update command can-execute state
                (FetchHtmlCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (BackButtonCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (ForwardButtonCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    private string _newBookmarkName = string.Empty;
    public string NewBookmarkName
    {
        get => _newBookmarkName;
        set
        {
            if (value != _newBookmarkName)
            {
                _newBookmarkName = value;
                RaisePropertyChanged();
            }
        }
    }
    private bool _isBookmarkFlyoutOpen;
    public bool IsBookmarkFlyoutOpen
    {
        get => _isBookmarkFlyoutOpen;
        set
        {
            if (value != _isBookmarkFlyoutOpen)
            {
                _isBookmarkFlyoutOpen = value;
                RaisePropertyChanged();
            }
        }
    }


    // Default constructor initializing with default services
    public MainWindowViewModel()
        : this(new WebService(), new StorageService(DbFile), new HtmlParser(), new NavService(), null) { }


    // Constructor with dependency injection for services
    public MainWindowViewModel(IWebService webService, IStorageService settingsService, IHtmlParser htmlParser, INavService navService, IHistoryService? historyService = null)
    {
        _webService = webService;
        _settingsService = settingsService;
        _htmlParser = htmlParser;
        _navService = navService;
        _historyService = historyService ?? new HistoryService(_settingsService);

        ShowBookmarkFlyoutCommand = new RelayCommand(ShowBookmarkFlyout);
        AddBookmarkCommand = new RelayCommand(AddBookmark);
        CancelBookmarkCommand = new RelayCommand(CancelBookmark);

        FetchHtmlCommand = new AsyncRelayCommand(() => FetchHtmlAsync(true), () => !IsBusy);
        ToggleHtmlCommand = new RelayCommand(() => ShowHtml = !ShowHtml);
        LinkClickCommand = new RelayCommand((object? param) =>
        {
            if (param is ParsedLink link && !string.IsNullOrWhiteSpace(link.Href))
            {
                Address = link.Href;
                (FetchHtmlCommand as AsyncRelayCommand)?.Execute(null);
            }
        });

        BackButtonCommand = new AsyncRelayCommand(GoBackAsync, () => _navService.CanGoBack());
        ForwardButtonCommand = new AsyncRelayCommand(GoForwardAsync, () => _navService.CanGoForward());
        HomeButtonCommand = new AsyncRelayCommand(GoHomeAsync);

        SaveHomePageCommand = new RelayCommand(SaveHomePage);
        HistoryClickCommand = new RelayCommand((object? param) =>
        {
            if (param is History historyItem && !string.IsNullOrWhiteSpace(historyItem.Url))
            {
                Address = historyItem.Url;
                (FetchHtmlCommand as AsyncRelayCommand)?.Execute(null);
            }
        });

        var homePageUrl = _settingsService.LoadHomePage();
        if (string.IsNullOrWhiteSpace(homePageUrl))
        {
            homePageUrl = "https://hw.ac.uk";
            _settingsService.SaveHomePage(homePageUrl);
        }
        _homePageUrl = homePageUrl;
        RaisePropertyChanged(nameof(HomePageUrl));
        _address = homePageUrl;
        RaisePropertyChanged(nameof(Address));

        var lastUrl = _settingsService.LoadLastUrl();
        if (!string.IsNullOrWhiteSpace(lastUrl))
        {
            _address = lastUrl;
            RaisePropertyChanged(nameof(Address));
        }

        LoadHistory();
        LoadBookmarks();
    }

    private void CancelBookmark()
    {
        IsBookmarkFlyoutOpen = false;
    }

    private void AddBookmark()
    {
        if (string.IsNullOrWhiteSpace(NewBookmarkName) || string.IsNullOrWhiteSpace(Address))
        {
            Status = "Bookmark name and URL cannot be empty.";
            return;
        }
        _settingsService.SaveBookmark(NewBookmarkName, Address);
        Status = $"Bookmark '{NewBookmarkName}' added.";
        NewBookmarkName = string.Empty;
        IsBookmarkFlyoutOpen = false;
        LoadBookmarks();
    }

    private void ShowBookmarkFlyout(object? obj)
    {
        if (string.IsNullOrWhiteSpace(Address))
        {
            Status = "Cannot bookmark an empty URL.";
            return;
        }
        NewBookmarkName = PageTitle;
        IsBookmarkFlyoutOpen = true;
    }

    private void LoadBookmarks()
    {
        var bookmarks = _settingsService.LoadBookmarks();
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Bookmarks.Clear();
            foreach (var item in bookmarks)
            {
                Bookmarks.Add(item);
            }
        });
    }

    private async Task GoHomeAsync()
    {
        var homePageUrl = _settingsService.LoadHomePage();
        if (!string.IsNullOrWhiteSpace(homePageUrl))
        {
            Address = homePageUrl;
            await FetchHtmlAsync(true);
        }
    }

    private void SaveHomePage()
    {
        _settingsService.SaveHomePage(HomePageUrl);
        Address = HomePageUrl;
        RaisePropertyChanged(nameof(Address));
    }

    private void SaveLastUrl(string url) => _settingsService.SaveLastUrl(url);

    // Controls visibility of raw HTML panel
    public bool ShowHtml
    {
        get => _showHtml;
        set
        {
            if (value != _showHtml)
            {
                _showHtml = value;
                RaisePropertyChanged();
            }
        }
    }

    // The parsed <title> of the fetched page
    public string PageTitle
    {
        get => _pageTitle;
        private set
        {
            if (value != _pageTitle)
            {
                _pageTitle = value;
                RaisePropertyChanged();
            }
        }
    }

    // Number of parsed links
    public int LinkCount => Links.Count;
    private async Task GoBackAsync()
    {
        var prev = _navService.GoBack();
        if (prev != null)
        {
            Address = prev;
            await FetchHtmlAsync(false);
        }
    }

    private async Task GoForwardAsync()
    {
        var next = _navService.GoForward();
        if (next != null)
        {
            Address = next;
            await FetchHtmlAsync(false);
        }
    }

    // Fetches the HTML from the specified URL asynchronously
    private async Task FetchHtmlAsync(bool isNewNavigation)
    {
        if (string.IsNullOrWhiteSpace(Address))
        {
            Status = "Please enter a URL.";
            return;
        }

        IsBusy = true;
        HtmlSource = string.Empty;
        try
        {
            if (isNewNavigation)
            {
                _navService.NavigateTo(Address);
                _historyService.SaveHistory(Address);
                LoadHistory();
            }

            var result = await _webService.FetchHtmlAsync(Address);
            HtmlSource = result.Html;
            Status = result.StatusMessage;
            if (!string.IsNullOrWhiteSpace(result.Html))
            {
                ParseHtml(result.Html, Address);
            }
            else
            {
                ClearParsed();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadHistory()
    {
        var history = _historyService.LoadHistory();
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            History.Clear();
            foreach (var item in history)
            {
                History.Add(item);
            }
        });
    }

    private void ClearParsed()
    {
        PageTitle = string.Empty;
        Links.Clear();
        RaisePropertyChanged(nameof(LinkCount));
    }

    private void ParseHtml(string html, string baseUrl)
    {
        var parseResult = _htmlParser.Parse(html, baseUrl);
        PageTitle = parseResult.Title;
        Links.Clear();
        foreach (var l in parseResult.Links)
            Links.Add(l);
        RaisePropertyChanged(nameof(LinkCount));
    }
}