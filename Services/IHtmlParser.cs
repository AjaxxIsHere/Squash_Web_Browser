using System;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace Squash_Web_Browser.Services;

public interface IHtmlParser
{
	NewParsedResult Parse(string html, string baseUrl, int linkLimit = 5);
}

// ParseResult class which is needed because it acts as a data container for the results of HTML parsing, allowing the parser to return both the page title and a list of parsed links in a single object.
public sealed class NewParsedResult
{
	public string Title { get; set; } = string.Empty;
	public List<ParsedLink> Links { get; set; } = new();
}

/*
Summary: This code defines an HTML parser interface (IHtmlParser) and its implementation (HtmlParser) for extracting the title and links from HTML content.
The HtmlParser class uses the HtmlAgilityPack library to parse the HTML and extract relevant information. Its methods include:

- Parse(string html, string baseUrl, int linkLimit = 5): Parses the provided HTML string and extracts the title and a list of links (up to the specified limit). It returns a ParseResult object containing the extracted title and links.
- ParsedLink: A class representing a parsed link with its href and text.

*/
public sealed class HtmlParser : IHtmlParser
{
	public NewParsedResult Parse(string html, string baseUrl, int linkLimit = 5)
	{
		var result = new NewParsedResult();
		
		if (string.IsNullOrWhiteSpace(html)) return result;
		try
		{
			var doc = new HtmlDocument();
			doc.LoadHtml(html);
			var titleNode = doc.DocumentNode.SelectSingleNode("//title");
			var title = titleNode?.InnerText?.Trim() ?? string.Empty;
			if (!string.IsNullOrEmpty(title)) title = HtmlEntity.DeEntitize(title);
			result.Title = title;

			var links = new List<ParsedLink>();
			var linkNodes = doc.DocumentNode.SelectNodes("//a[@href]");
			if (linkNodes != null)
			{
				var baseUri = new Uri(baseUrl);
				int count = 0;
				foreach (var a in linkNodes)
				{
					if (count++ >= linkLimit) break;
					var href = a.GetAttributeValue("href", string.Empty).Trim();
					if (string.IsNullOrEmpty(href)) continue;

					var absoluteUri = new Uri(baseUri, href);
					var text = a.InnerText?.Trim();
					if (string.IsNullOrEmpty(text)) text = href;
					text = HtmlEntity.DeEntitize(text);
					links.Add(new ParsedLink { Href = absoluteUri.ToString(), Text = text });
				}
			}
			result.Links = links; // mutate after creation for simplicity
			return result;
		}
		catch
		{
			return new NewParsedResult();
		}
	}
}

// Keep ParsedLink here so services & VM can share
public class ParsedLink
{
	public string Href { get; set; } = string.Empty;
	public string Text { get; set; } = string.Empty;
}
