using System;

namespace Squash_Web_Browser.Services;


/*
Summary: This code defines a UrlHelper class with a static method NormalizeUrl that standardizes URLs by ensuring they have the correct scheme (http/https) and formatting. The method checks if the URL is null or whitespace, adds "https://" if no scheme is present, and ensures that the path ends with a slash if it's just a domain. This helps maintain consistency in URL handling across the application. 

Its used in:
IWebService.FetchHtmlAsync
INavService.NavigateTo
IHistoryService.SaveHistory

*/
public static class UrlHelper
{
    public static string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        string normalizedUrl = url.Trim();
        if (!normalizedUrl.StartsWith("http://") && !normalizedUrl.StartsWith("https://"))
            normalizedUrl = "https://" + normalizedUrl; // concatenating also normalizes uppercases to lowercases

        if (Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri))
        {
            // Ensure the path ends with a slash if it's just a domain
            if (string.IsNullOrEmpty(uri.AbsolutePath) || uri.AbsolutePath == "/")
            {
                var builder = new UriBuilder(uri) { Path = "/" };
                uri = builder.Uri;
            }
            return uri.ToString();
        }

        return url;
    }
}