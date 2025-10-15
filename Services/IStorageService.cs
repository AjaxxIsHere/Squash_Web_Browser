using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
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

public sealed class StorageService : IStorageService
{

	private readonly string _dbFile;
	public StorageService(string dbFile = "browserdata.db")
	{
		_dbFile = dbFile;
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
		catch
		{ 
			// swallow, caller can show generic error if needed
		}
		return null;
	}

	public void SaveBookmark(string name, string url)
	{
		if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url)) return;

		try
		{
			// Ensure the Bookmarks table exists
			using var conn = new SqliteConnection($"Data Source={_dbFile}");
			conn.Open();
			using var cmd = conn.CreateCommand();
			cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Bookmarks (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Url TEXT);";
			cmd.ExecuteNonQuery();

			// Check if a bookmark with the same name and url already exists using LINQ
			var existing = LoadBookmarks()
				.Any(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase) &&
						  string.Equals(b.Url, url, StringComparison.OrdinalIgnoreCase));
			if (existing)
				return;

			// Insert new bookmark
			cmd.CommandText = @"INSERT INTO Bookmarks (Name, Url) VALUES ($name, $url);";
			cmd.Parameters.AddWithValue("$name", name);
			cmd.Parameters.AddWithValue("$url", url);
			cmd.ExecuteNonQuery();
		}
		catch
		{
			// swallow, caller can show generic error if needed
		}
	}

	public void DeleteBookmark(int id)
	{
		try
		{
			var bookmarks = LoadBookmarks();
			var bookmarkToDelete = bookmarks.FirstOrDefault(b => b.Id == id);
			if (bookmarkToDelete != null)
			{
				using var conn = new SqliteConnection($"Data Source={_dbFile}");
				conn.Open();
				using var cmd = conn.CreateCommand();
				cmd.CommandText = @"DELETE FROM Bookmarks WHERE Id = $id;";
				cmd.Parameters.AddWithValue("$id", id);
				cmd.ExecuteNonQuery();
			}
		}
		catch
		{
			// swallow, caller can show generic error if needed
		}
	}

	public List<Bookmark> LoadBookmarks()
	{
		var bookmarks = new List<Bookmark>();
		try
		{
			using var conn = new SqliteConnection($"Data Source={_dbFile}");
			conn.Open();
			using var cmd = conn.CreateCommand();
			cmd.CommandText = @"SELECT Id, Name, Url FROM Bookmarks;";
			using var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				bookmarks.Add(new Bookmark
				{
					Id = reader.GetInt32(0),
					Name = reader.GetString(1),
					Url = reader.GetString(2)
				});
			}
		}
		catch
		{
			// swallow, caller can show generic error if needed
		}
		return bookmarks;
	}

	public void SaveHistory(string url)
	{
		if (string.IsNullOrWhiteSpace(url)) return;

		try
		{
			using var conn = new SqliteConnection($"Data Source={_dbFile}");
			conn.Open();
			using var cmd = conn.CreateCommand();
			cmd.CommandText = @"CREATE TABLE IF NOT EXISTS History (Id INTEGER PRIMARY KEY AUTOINCREMENT, Url TEXT, Timestamp DATETIME);";
			cmd.ExecuteNonQuery();

			cmd.CommandText = @"INSERT INTO History (Url, Timestamp) VALUES ($url, $timestamp);";
			cmd.Parameters.AddWithValue("$url", url);
			cmd.Parameters.AddWithValue("$timestamp", DateTime.Now);
			cmd.ExecuteNonQuery();
		}
		catch
		{
			// swallow
		}
	}

	public List<History> LoadHistory()
	{
		var history = new List<History>();
		try
		{
			using var conn = new SqliteConnection($"Data Source={_dbFile}");
			conn.Open();
			using var cmd = conn.CreateCommand();
			cmd.CommandText = @"SELECT Id, Url, Timestamp FROM History ORDER BY Timestamp DESC;";
			using var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				history.Add(new History
				{
					Id = reader.GetInt32(0),
					Url = reader.GetString(1),
					Timestamp = reader.GetDateTime(2)
				});
			}
		}
		catch
		{
			// swallow
		}
		return history;
	}

	public void SaveHomePage(string url)
	{
		if (string.IsNullOrWhiteSpace(url)) return;
		try
		{
			using var conn = new SqliteConnection($"Data Source={_dbFile}");
			conn.Open();
			using var cmd = conn.CreateCommand();
			cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Settings (Key TEXT PRIMARY KEY, Value TEXT);";
			cmd.ExecuteNonQuery();
			cmd.CommandText = @"INSERT INTO Settings (Key, Value) VALUES ('HomePage', $url) ON CONFLICT(Key) DO UPDATE SET Value=$url;";
			cmd.Parameters.AddWithValue("$url", url);
			cmd.ExecuteNonQuery();
		}
		catch
		{
			// swallow, caller can show generic error if needed
		}
	}

	public string? LoadHomePage()
	{
		try
		{
			using var conn = new SqliteConnection($"Data Source={_dbFile}");
			conn.Open();
			using var cmd = conn.CreateCommand();
			cmd.CommandText = @"SELECT Value FROM Settings WHERE Key='HomePage' LIMIT 1;";
			using var reader = cmd.ExecuteReader();
			if (reader.Read())
				return reader.GetString(0);
		}
		catch
		{
			// swallow, caller can show generic error if needed
		}
		return null;
	}
}
