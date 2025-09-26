using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Squash_Web_Browser.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public string Title => "Squash Browser";

    private string _address = "https://www.example.com"; // default example URL
    private string _htmlSource = string.Empty;             // holds fetched HTML
    private string _status = "Idle";                      // status messages (success/error/loading)
    private bool _isBusy;                                  // indicates fetch in progress

    private readonly HttpClient _httpClient = new();       // reuse a single HttpClient instance

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

    public MainWindowViewModel()
    {
        FetchHtmlCommand = new AsyncRelayCommand(FetchHtmlAsync, () => !IsBusy);
    }

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
        }
        catch (TaskCanceledException)
        {
            Status = "Request timed out.";
        }
        catch (HttpRequestException ex)
        {
            Status = "Network error: " + ex.Message;
        }
        catch (Exception ex)
        {
            Status = "Unexpected error: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
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
