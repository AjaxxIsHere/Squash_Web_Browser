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
	/*
	How does each line work in the Parse method?
	- The method starts by creating a new instance of NewParsedResult to hold the parsing results
	- It checks if the input HTML string is null or whitespace; if so, it returns an empty result
	- It loads the HTML string into an HtmlDocument object for parsing
	- It selects the title node from the document and extracts its inner text, trimming whitespace and decoding HTML entities
	- It initializes a list to hold the parsed links
	- It selects all anchor nodes with href attributes from the document
	- It creates a base URI from the provided baseUrl for resolving relative links
	- It iterates over the selected anchor nodes, up to the specified link limit
	- For each anchor, it retrieves the href attribute and resolves it to an absolute URI
	- It extracts the inner text of the anchor, trimming whitespace and decoding HTML entities
	- It adds a new ParsedLink object to the list with the resolved href and text
	- Finally, it assigns the list of parsed links to the result and returns it
	*/
	public NewParsedResult Parse(string html, string baseUrl, int linkLimit = 5)
	{
		var result = new NewParsedResult(); // Initialize new object to hold results

		if (string.IsNullOrWhiteSpace(html)) return result; // Return empty result if HTML is null or whitespace
		
		try
		{
			var doc = new HtmlDocument(); // Create new HTML document
			doc.LoadHtml(html);
			var titleNode = doc.DocumentNode.SelectSingleNode("//title"); // extract title
			var title = titleNode?.InnerText?.Trim() ?? string.Empty;
			if (!string.IsNullOrEmpty(title)) title = HtmlEntity.DeEntitize(title); 
			result.Title = title;

			var links = new List<ParsedLink>();
			var linkNodes = doc.DocumentNode.SelectNodes("//a[@href]"); // extract links
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
