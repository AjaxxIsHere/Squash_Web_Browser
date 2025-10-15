using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Linq;
using Squash_Web_Browser.Services;
using Squash_Web_Browser.Models;

namespace Squash_Web_Browser.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public static string Title => "Squash Browser";

    private string _address;
    private const string DbFile = "browserdata.db"; // still referenced by default settings service

    // holds fetched HTML
    private string _htmlSrc = string.Empty;

    // status messages (success/error/loading)
    private string _status = "Idle";

    // indicates fetch in progress            
    private bool _isBusy;

    private string _homePageUrl = string.Empty;

    // parsed <title>               
    private string _pageTitle = string.Empty;

    // controls visibility of raw HTML panel
    private bool _showHtml = true;

    private readonly List<string> _navigationStack = new();
    private int _navigationIndex = -1;

    private readonly IWebService _webService;
    private readonly IStorageService _settingsService;
    private readonly IHtmlParser _htmlParser;

    // parsed links

    public ObservableCollection<ParsedLink> Links
    {
        get;
    } = [];

    // history
    public ObservableCollection<History> History { get; } = [];

    // The URL entered by the user
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

    // Command bound to the Go button / Enter key to fetch HTML
    public ICommand FetchHtmlCommand { get; }
    public ICommand ToggleHtmlCommand { get; }
    public ICommand LinkClickCommand { get; }
    public ICommand BackButtonCommand { get; }
    public ICommand ForwardButtonCommand { get; }
    public ICommand HomeButtonCommand { get; }

    // Command to save the home page URL
    public ICommand SaveHomePageCommand { get; }

    // Command to handle clicks on history items
    public ICommand HistoryClickCommand { get; }

    public MainWindowViewModel()
        : this(new WebService(), new StorageService(DbFile), new HtmlParser()) { }

    public MainWindowViewModel(IWebService webService, IStorageService settingsService, IHtmlParser htmlParser)
    {
        _webService = webService;
        _settingsService = settingsService;
        _htmlParser = htmlParser;

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

        BackButtonCommand = new AsyncRelayCommand(GoBackAsync, CanGoBack);
        ForwardButtonCommand = new AsyncRelayCommand(GoForwardAsync, CanGoForward);
        HomeButtonCommand = new AsyncRelayCommand(GoHomeAsync);

        SaveHomePageCommand = new RelayCommand(SaveHomePage);
        HistoryClickCommand = new RelayCommand((object? param) =>
        {
            if (param is History historyItem && !string.IsNullOrWhiteSpace(historyItem.Url))
            {
                // If the clicked URL is the same as the current one, do nothing.
                if (historyItem.Url == Address)
                {
                    return;
                }
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


    private bool CanGoBack() => _navigationIndex > 0;
    private bool CanGoForward() => _navigationIndex < _navigationStack.Count - 1;

    private async Task GoBackAsync()
    {
        if (CanGoBack())
        {
            _navigationIndex--;
            Address = _navigationStack[_navigationIndex];
            await FetchHtmlAsync(false);
        }
    }

    private async Task GoForwardAsync()
    {
        if (CanGoForward())
        {
            _navigationIndex++;
            Address = _navigationStack[_navigationIndex];
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
                if (_navigationIndex < _navigationStack.Count - 1)
                {
                    _navigationStack.RemoveRange(_navigationIndex + 1, _navigationStack.Count - (_navigationIndex + 1));
                }
                _navigationStack.Add(Address);
                _navigationIndex = _navigationStack.Count - 1;
                
                // Prevent adding consecutive duplicates to history
                var lastHistoryItem = History.FirstOrDefault();
                if (lastHistoryItem == null || lastHistoryItem.Url != Address)
                {
                    _settingsService.SaveHistory(Address);
                    LoadHistory();
                }
            }
            
            var result = await _webService.FetchHtmlAsync(Address);
            HtmlSource = result.Html;
            Status = result.StatusMessage;
            if (!string.IsNullOrWhiteSpace(result.Html))
            {
                ParseHtml(result.Html);
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
        var history = _settingsService.LoadHistory();
        
        // To prevent UI exceptions, update the collection on the UI thread
        // and do it in a way that avoids race conditions with selection.
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            History.Clear();
            foreach (var item in history)
            {
                History.Add(item);
            }
        });
    }

    // Clears parsed state
    // For dummies: This is like resetting the "important stuff" when something goes wrong.
    private void ClearParsed()
    {
        PageTitle = string.Empty;
        Links.Clear();
        RaisePropertyChanged(nameof(LinkCount));
    }

    // Parses the HTML to extract <title> and <a href> links
    // For dummies: This is like reading a webpage and picking out the title and all the links on it.
    private void ParseHtml(string html)
    {
        var parseResult = _htmlParser.Parse(html);
        PageTitle = parseResult.Title;
        Links.Clear();
        foreach (var l in parseResult.Links)
            Links.Add(l);
        RaisePropertyChanged(nameof(LinkCount));
    }
}
// ParsedLink, RelayCommand, AsyncRelayCommand moved to Services namespace file(s)
