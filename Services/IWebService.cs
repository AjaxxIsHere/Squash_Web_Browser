using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Squash_Web_Browser.Services;

public interface IWebService
{
	Task<WebResult> FetchHtmlAsync(string url, CancellationToken cancellationToken = default); // Asynchronously fetches HTML content from the specified URL
}

// WebResult class which encapsulates the result of a web fetch operation, including success status, HTML content, status message, bytes loaded, and HTTP status code.
public sealed class WebResult
{
	public bool IsSuccess { get; init; }
	public string FinalUrl { get; init; } = string.Empty;
	public string Html { get; init; } = string.Empty;
	public string StatusMessage { get; init; } = string.Empty;
	public int BytesLoaded { get; init; }
	public int? StatusCode { get; init; }
}


/*
Summary: This code defines a web service interface (IWebService) and its implementation (WebService) for fetching HTML content from a given URL. The WebService class uses HttpClient to perform HTTP GET requests and handle various scenarios such as time outs, network errors, and invalid URLs. Its methods include:

- FetchHtmlAsync(string url, CancellationToken cancellationToken = default): Asynchronously fetches HTML content from the specified URL. It returns a WebResult object containing the success status, HTML content, status message, number of bytes loaded, and HTTP status code.
- WebResult: A class representing the result of the web fetch operation, including properties for success status, HTML content, status message, bytes loaded, and HTTP status code.

*/
public sealed class WebService : IWebService
{
	// HttpClient instance for making HTTP requests
	private readonly HttpClient _httpClient;

	// Constructor to initialize the web service with an optional HttpClient
	public WebService(HttpClient? client = null)
	{
		_httpClient = client ?? new HttpClient();
	}

	public async Task<WebResult> FetchHtmlAsync(string url, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(url))
			return new WebResult { IsSuccess = false, StatusMessage = "Empty URL." };

		string normalizedUrl = UrlHelper.NormalizeUrl(url);
		if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri))
		{
			return new WebResult { IsSuccess = false, StatusMessage = "Invalid URL." };
		}

		try
		{
			using var ctsLinked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			ctsLinked.CancelAfter(TimeSpan.FromSeconds(30));
			var response = await _httpClient.GetAsync(uri, ctsLinked.Token);
			var bytes = await response.Content.ReadAsByteArrayAsync(ctsLinked.Token);

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
			if (response.IsSuccessStatusCode)
			{
				return new WebResult
				{
					IsSuccess = true,
					FinalUrl = uri.ToString(),
					Html = html,
					BytesLoaded = bytes.Length,
					StatusCode = (int)response.StatusCode,
					StatusMessage = $"Loaded {bytes.Length:N0} bytes (HTTP {(int)response.StatusCode} {response.ReasonPhrase})"
				};
			}
			else
			{
				string errorMsg = response.StatusCode switch
				{
					System.Net.HttpStatusCode.BadRequest => "400 Bad Request: The server could not understand the request.",
					System.Net.HttpStatusCode.Forbidden => "403 Forbidden: You do not have permission to access this resource.",
					System.Net.HttpStatusCode.NotFound => "404 Not Found: The requested resource could not be found.",
					_ => $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}"
				};
				return new WebResult
				{
					IsSuccess = false,
					FinalUrl = uri.ToString(),
					Html = html, // still return body for parsing attempt
					BytesLoaded = bytes.Length,
					StatusCode = (int)response.StatusCode,
					StatusMessage = errorMsg
				};
			}
		}
		catch (TaskCanceledException)
		{
			return new WebResult { IsSuccess = false, StatusMessage = "Request timed out." };
		}
		catch (HttpRequestException ex)
		{
			return new WebResult { IsSuccess = false, StatusMessage = "Network error: " + ex.Message };
		}
		catch (Exception ex)
		{
			return new WebResult { IsSuccess = false, StatusMessage = "Unexpected error: " + ex.Message };
		}
	}
}

