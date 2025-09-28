using Microsoft.Data.Sqlite;

namespace Squash_Web_Browser.Services;

public interface IStorageService
{
	void SaveLastUrl(string url);
	string? LoadLastUrl();
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
		catch { }
		return null;
	}
}
