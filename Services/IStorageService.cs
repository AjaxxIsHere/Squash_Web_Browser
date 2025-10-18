using System;
using System.Collections.Generic;
using System.Linq;
using Squash_Web_Browser.Models;

namespace Squash_Web_Browser.Services;

public interface IStorageService
{
	void SaveHomePage(string url);
	string? LoadHomePage();
	void SaveLastUrl(string url);
	string? LoadLastUrl();
	void SaveBookmark(string name, string url);
	void DeleteBookmark(int id);
	List<Bookmark> LoadBookmarks();
	void SaveHistory(string url);
	List<History> LoadHistory();
}


/*
Summary: This code defines a storage service interface (IStorageService) and its implementation (StorageService) for managing persistent data storage in a web browser context.
The StorageService class uses SQLite to store and retrieve data such as home page URL, last visited URL, bookmarks, and browsing history. Its methods include:

- SaveHomePage(string url) and LoadHomePage(): Save and load the home page URL.
- SaveLastUrl(string url) and LoadLastUrl(): Save and load the last visited URL
- SaveBookmark(string name, string url), DeleteBookmark(int id), and LoadBookmarks(): Manage bookmarks by saving, deleting, and loading them.
- SaveHistory(string url) and LoadHistory(): Save and load browsing history.
- LoadBookmarks() : Loads all bookmarks from the database and returns them as a list of Bookmark objects.

*/
public sealed class StorageService : IStorageService
{

	// Database file path
	private readonly string _dbFile;

	// Constructor to initialize the storage service with the database file path
	public StorageService(string dbFile = "browserdata.db")
	{
		_dbFile = dbFile;
        using var db = new AppDbContext(_dbFile);
        db.Database.EnsureCreated();
	}

	public void SaveLastUrl(string url)
	{
		if (string.IsNullOrWhiteSpace(url)) return;
		try
		{
			using var db = new AppDbContext(_dbFile);
			var lastUrlSetting = db.Settings.FirstOrDefault(s => s.Key == "LastUrl");
			if (lastUrlSetting == null)
			{
				db.Settings.Add(new Setting { Key = "LastUrl", Value = url });
			}
			else
			{
				lastUrlSetting.Value = url;
			}
			db.SaveChanges();
		}
		catch
		{
			Console.WriteLine("Error saving last URL.");
		}
	}

	public string? LoadLastUrl()
	{
		try
		{
			using var db = new AppDbContext(_dbFile);
			return db.Settings.FirstOrDefault(s => s.Key == "LastUrl")?.Value;
		}
		catch
		{
			Console.WriteLine("Error loading last URL.");
		}
		return null;
	}

	public void SaveBookmark(string name, string url)
	{
		if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url)) return;

		try
		{
			using var db = new AppDbContext(_dbFile);
			var existing = db.Bookmarks
				.Any(b => b.Name.ToLower() == name.ToLower() &&
						  b.Url.ToLower() == url.ToLower());
			if (existing)
				return;

			db.Bookmarks.Add(new Bookmark { Name = name, Url = url });
			db.SaveChanges();
		}
		catch (Exception ex)
		{
			Console.WriteLine("Error saving bookmark: " + ex.Message);
		}
	}

	public void DeleteBookmark(int id)
	{
		try
		{
			using var db = new AppDbContext(_dbFile);
			var bookmarkToDelete = db.Bookmarks.FirstOrDefault(b => b.Id == id);
			if (bookmarkToDelete != null)
			{
				db.Bookmarks.Remove(bookmarkToDelete);
				db.SaveChanges();
			}
		}
		catch
		{
			Console.WriteLine("Error deleting bookmark.");
		}
	}

	public List<Bookmark> LoadBookmarks()
	{
		try
		{
			using var db = new AppDbContext(_dbFile);
			return db.Bookmarks.ToList();
		}
		catch
		{
			Console.WriteLine("Error loading bookmarks.");
		}
		return new List<Bookmark>();
	}

	public void SaveHistory(string url)
	{
		if (string.IsNullOrWhiteSpace(url)) return;

		try
		{
			using var db = new AppDbContext(_dbFile);
			db.History.Add(new History { Url = url, Timestamp = DateTime.Now });
			db.SaveChanges();
		}
		catch
		{
			Console.WriteLine("Error saving history.");
		}
	}

	public List<History> LoadHistory()
	{
		try
		{
			using var db = new AppDbContext(_dbFile);
			return db.History.OrderByDescending(h => h.Timestamp).ToList();
		}
		catch
		{
			Console.WriteLine("Error loading history.");
		}
		return new List<History>();
	}

	public void SaveHomePage(string url)
	{
		if (string.IsNullOrWhiteSpace(url)) return;
		try
		{
			using var db = new AppDbContext(_dbFile);
			var homePageSetting = db.Settings.FirstOrDefault(s => s.Key == "HomePage");
			if (homePageSetting == null)
			{
				db.Settings.Add(new Setting { Key = "HomePage", Value = url });
			}
			else
			{
				homePageSetting.Value = url;
			}
			db.SaveChanges();
		}
		catch
		{
			Console.WriteLine("Error saving home page.");
		}
	}

	public string? LoadHomePage()
	{
		try
		{
			using var db = new AppDbContext(_dbFile);
			return db.Settings.FirstOrDefault(s => s.Key == "HomePage")?.Value;
		}
		catch
		{
			Console.WriteLine("Error loading home page.");
		}
		return null;
	}
}

