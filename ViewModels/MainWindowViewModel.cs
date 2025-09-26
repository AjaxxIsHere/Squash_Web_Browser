using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;
using HtmlAgilityPack;

namespace Squash_Web_Browser.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public string Title => "Squash Browser";

    private string _address = "https://www.example.com"; // default example URL
    private string _htmlSource = string.Empty;             // holds fetched HTML
    private string _status = "Idle";                      // status messages (success/error/loading)
    private bool _isBusy;                                  // indicates fetch in progress
    private string _pageTitle = string.Empty;              // parsed <title>
    private bool _showHtml = true;                         // controls visibility of raw HTML panel

    private readonly HttpClient _httpClient = new();       // reuse a single HttpClient instance

    public ObservableCollection<ParsedLink> Links { get; } = new(); // parsed links

    public string Address
    {
        get => _address;
        set
        {
            if (value != _address)
            {
                _address = value;
                RaisePropertyChanged();
            }
        }
    }

    public string HtmlSource
    {
        get => _htmlSource;
        private set
        {
            if (value != _htmlSource)
            {
                _htmlSource = value;
                RaisePropertyChanged();
            }
        }
    }

    public string Status
    {
        get => _status;
        private set
        {
            if (value != _status)
            {
                _status = value;
                RaisePropertyChanged();
            }
        }
    }

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
            }
        }
    }

    // Command bound to the Go button / Enter key to fetch HTML
    public ICommand FetchHtmlCommand { get; }
    public ICommand ToggleHtmlCommand { get; }

    public MainWindowViewModel()
    {
        FetchHtmlCommand = new AsyncRelayCommand(FetchHtmlAsync, () => !IsBusy);
        ToggleHtmlCommand = new RelayCommand(() => ShowHtml = !ShowHtml);
    }

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

    public int LinkCount => Links.Count;

    private async Task FetchHtmlAsync()
    {
        if (string.IsNullOrWhiteSpace(Address))
        {
            Status = "Please enter a URL.";
            return;
        }

        // Try to build a valid Uri. Add scheme if missing.
        if (!Uri.TryCreate(Address, UriKind.Absolute, out var uri))
        {
            // Try adding https:// if user omitted scheme.
            if (Uri.TryCreate("https://" + Address, UriKind.Absolute, out var httpsUri))
            {
                uri = httpsUri;
                Address = uri.ToString(); // normalize displayed address
            }
            else
            {
                Status = "Invalid URL.";
                return;
            }
        }

        IsBusy = true;
        Status = "Loading...";
        HtmlSource = string.Empty; // clear previous

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20)); // simple timeout

        try
        {
            // Send request
            var response = await _httpClient.GetAsync(uri, cts.Token);
            var bytes = await response.Content.ReadAsByteArrayAsync(cts.Token);

            // Try to detect encoding (fallback UTF8)
            var charset = response.Content.Headers.ContentType?.CharSet;
            Encoding encoding;
            try
            {
                encoding = !string.IsNullOrWhiteSpace(charset) ? Encoding.GetEncoding(charset) : Encoding.UTF8;
            }
            catch
            {
                encoding = Encoding.UTF8;
            }

            var html = encoding.GetString(bytes);
            HtmlSource = html;

            Status = response.IsSuccessStatusCode
                ? $"Loaded {bytes.Length:N0} bytes"
                : $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";

            // Parse after successful fetch (even if non-success status we still attempt to parse body)
            ParseHtml(html);
        }
        catch (TaskCanceledException)
        {
            Status = "Request timed out.";
            ClearParsed();
        }
        catch (HttpRequestException ex)
        {
            Status = "Network error: " + ex.Message;
            ClearParsed();
        }
        catch (Exception ex)
        {
            Status = "Unexpected error: " + ex.Message;
            ClearParsed();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ClearParsed()
    {
        PageTitle = string.Empty;
        Links.Clear();
        RaisePropertyChanged(nameof(LinkCount));
    }

    private void ParseHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            ClearParsed();
            return;
        }

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Title
            var titleNode = doc.DocumentNode.SelectSingleNode("//title");
            var title = titleNode?.InnerText?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(title))
            {
                title = HtmlEntity.DeEntitize(title);
            }
            PageTitle = title;

            Links.Clear();
            var linkNodes = doc.DocumentNode.SelectNodes("//a[@href]");
            if (linkNodes != null)
            {
                int limit = 200; // cap to avoid overwhelming UI
                int count = 0;
                foreach (var a in linkNodes)
                {
                    if (count++ >= limit) break;
                    var href = a.GetAttributeValue("href", string.Empty).Trim();
                    if (string.IsNullOrEmpty(href)) continue;
                    var text = a.InnerText?.Trim();
                    if (string.IsNullOrEmpty(text)) text = href;
                    text = HtmlEntity.DeEntitize(text);
                    Links.Add(new ParsedLink { Href = href, Text = text });
                }
            }

            RaisePropertyChanged(nameof(LinkCount));
        }
        catch
        {
            // On parse failure just clear parsed state (silent)
            ClearParsed();
        }
    }
}

public class ParsedLink
{
    public string Href { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
    public event EventHandler? CanExecuteChanged;
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// Simple ICommand implementation for async operations with CanExecute refresh.
/// </summary>
public class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
        => !_isExecuting && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        _isExecuting = true;
        RaiseCanExecuteChanged();
        try
        {
            await _execute();
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public event EventHandler? CanExecuteChanged;
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
