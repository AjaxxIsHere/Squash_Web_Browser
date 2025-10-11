using System.Collections.Generic;
using HtmlAgilityPack;

namespace Squash_Web_Browser.Services;

public interface IHtmlParser
{
	ParseResult Parse(string html, int linkLimit = 5);
}

public sealed class ParseResult
{
	public string Title { get; set; } = string.Empty;
	public List<ParsedLink> Links { get; set; } = new();
}

public sealed class HtmlParser : IHtmlParser
{
	public ParseResult Parse(string html, int linkLimit = 5)
	{
		var result = new ParseResult();
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
				int count = 0;
				foreach (var a in linkNodes)
				{
					if (count++ >= linkLimit) break;
					var href = a.GetAttributeValue("href", string.Empty).Trim();
					if (string.IsNullOrEmpty(href)) continue;
					var text = a.InnerText?.Trim();
					if (string.IsNullOrEmpty(text)) text = href;
					text = HtmlEntity.DeEntitize(text);
					links.Add(new ParsedLink { Href = href, Text = text });
				}
			}
			result.Links = links; // mutate after creation for simplicity
			return result;
		}
		catch
		{
			return new ParseResult();
		}
	}
}

// Keep ParsedLink here so services & VM can share
public class ParsedLink
{
	public string Href { get; set; } = string.Empty;
	public string Text { get; set; } = string.Empty;
}
