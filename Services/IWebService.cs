using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Squash_Web_Browser.Services;

public interface IWebService
{
	Task<WebResult> FetchHtmlAsync(string url, CancellationToken cancellationToken = default);
}

public sealed class WebResult
{
	public bool IsSuccess { get; init; }
	public string Html { get; init; } = string.Empty;
	public string StatusMessage { get; init; } = string.Empty;
	public int BytesLoaded { get; init; }
	public int? StatusCode { get; init; }
}

public sealed class WebService : IWebService
{
	private readonly HttpClient _httpClient;

	public WebService(HttpClient? client = null)
	{
		_httpClient = client ?? new HttpClient();
	}

	public async Task<WebResult> FetchHtmlAsync(string url, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(url))
			return new WebResult { IsSuccess = false, StatusMessage = "Empty URL." };

		if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
		{
			if (Uri.TryCreate("https://" + url, UriKind.Absolute, out var httpsUri))
			{
				uri = httpsUri;
			}
			else
			{
				return new WebResult { IsSuccess = false, StatusMessage = "Invalid URL." };
			}
		}

		try
		{
			using var ctsLinked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			ctsLinked.CancelAfter(TimeSpan.FromSeconds(20));
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
