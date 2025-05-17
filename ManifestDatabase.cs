using Microsoft.Data.Sqlite;

namespace iPhoneBackupMessagesRenderer;

public class ManifestDatabase(string manifestDbFilePath) : IDisposable
{
    private readonly SqliteConnection _connection = SqliteHelper.OpenDatabaseReadOnly(manifestDbFilePath);

    // exact match on domain and relativePath, no guessing
    public ManifestFileInfo? GetFileInfo(string domain, string path)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = """
                          SELECT fileID, domain, relativePath, flags
                          FROM Files
                          WHERE domain = $domain AND relativePath = $relativePath
                          """;

        cmd.Parameters.AddWithValue("$domain", domain);
        
        // Sms.db references things with ~/Library/ but Manifest db references with Library/
        cmd.Parameters.AddWithValue("$relativePath", path.StartsWith("~/") ? path[2..] : path);

        using var reader = cmd.ExecuteReader();

        while (reader.Read()) // just read the first valid row. If there are multiple rows we have no way to rank them
        {
            // no columns should be null; if there are, skip the row
            if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2) || reader.IsDBNull(3)) continue;
            
            return new ManifestFileInfo(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3));
        }

        // could not find a matching file
        return null;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}

public record ManifestFileInfo(string FileId, string Domain, string RelativePath, int Flags)
{
    // Within a backup, files are named by GUIDs under subfolders of the first 2 characters, e.g.
    // ac/ac1d5a84d5b7b10cff9c6f743bde79898cf8abc2
    //
    // This function returns the path to the file content so you can open or do something with it
    public string GetContentPath(string basePath) => Path.Combine(basePath, FileId[..2], FileId);
}