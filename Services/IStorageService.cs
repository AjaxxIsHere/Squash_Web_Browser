
using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace Squash_Web_Browser.Services;

public interface IStorageService
{
	void SaveLastUrl(string url);
	string? LoadLastUrl();
	void SaveBookmark(string name, string url); // placeholder for saving a bookmark
}

public sealed class StorageService : IStorageService
{
	private readonly string _dbFile;
	public StorageService(string dbFile = "browserdata.db")
	{
		_dbFile = dbFile;
	}

	public void SaveBookmark(string name, string url)
	{
		// Placeholder: just print to console
		Console.WriteLine($"Bookmark saved: Name={name}, URL={url}");

		// Save to database
		if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url)) return;
		try
		{
			using var conn = new SqliteConnection($"Data Source={_dbFile}");
			conn.Open();
			using var cmd = conn.CreateCommand();
			cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Bookmarks (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Url TEXT);";
			cmd.ExecuteNonQuery();
			cmd.CommandText = @"INSERT INTO Bookmarks (Name, Url) VALUES ($name, $url);";
			cmd.Parameters.AddWithValue("$name", name);
			cmd.Parameters.AddWithValue("$url", url);
			cmd.ExecuteNonQuery();
			LoadAllBookmarks(); // Refresh bookmarks after adding a new one
		}
		catch
		{
			// swallow, caller can show generic error if needed
		}
	}

	public List<(string Name, string Url)> LoadAllBookmarks()
	{
		var bookmarks = new List<(string Name, string Url)>();
		try
		{
			using var conn = new SqliteConnection($"Data Source={_dbFile}");
			conn.Open();
			using var cmd = conn.CreateCommand();
			cmd.CommandText = @"SELECT Name, Url FROM Bookmarks;";
			using var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				var name = reader.GetString(0);
				var url = reader.GetString(1);
				bookmarks.Add((name, url));
			}
		}
		catch
		{
			// swallow, caller can show generic error if needed
		}
		return bookmarks;
	}

	public void SaveLastUrl(string url)
	{
		if (string.IsNullOrWhiteSpace(url)) return;
		try
		{
			using var conn = new SqliteConnection($"Data Source={_dbFile}");
			conn.Open();
			using var cmd = conn.CreateCommand();
			cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Settings (Key TEXT PRIMARY KEY, Value TEXT);";
			cmd.ExecuteNonQuery();
			cmd.CommandText = @"INSERT INTO Settings (Key, Value) VALUES ('LastUrl', $url) ON CONFLICT(Key) DO UPDATE SET Value=$url;";
			cmd.Parameters.AddWithValue("$url", url);
			cmd.ExecuteNonQuery();
		}
		catch
		{
			// swallow, caller can show generic error if needed
		}
	}

	public string? LoadLastUrl()
	{
		try
		{
			using var conn = new SqliteConnection($"Data Source={_dbFile}");
			conn.Open();
			using var cmd = conn.CreateCommand();
			cmd.CommandText = @"SELECT Value FROM Settings WHERE Key='LastUrl' LIMIT 1;";
			using var reader = cmd.ExecuteReader();
			if (reader.Read())
				return reader.GetString(0);
		}
		catch { }
		return null;
	}
}
