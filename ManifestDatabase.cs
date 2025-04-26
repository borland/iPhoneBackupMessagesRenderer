using Microsoft.Data.Sqlite;

namespace iPhoneBackupMessagesRenderer;

public class ManifestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    
    public ManifestDatabase(string manifestDbFilePath)
    {
        _connection = SqliteHelper.OpenDatabaseReadOnly(manifestDbFilePath);
    }

    // exact match on relativePath, no guessing
    public ManifestFileInfo? GetFileInfoByRelativePath(string path)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = """
                          SELECT fileID, domain, relativePath, flags
                          FROM Files
                          WHERE relativePath = $relativePath
                          """;
        
        // Sms.db references things with ~/Library/ but Manifest db references with Library/
        cmd.Parameters.AddWithValue("$relativePath", path.StartsWith("~/") ? path[2..] : path);

        using var reader = cmd.ExecuteReader();
        string? fileId = null, domain = null, relativePath = null;
        int flags;

        if (reader.Read()) // just read the first row, if there are multiple we have no way to rank them
        {
            fileId = !reader.IsDBNull(0) ? reader.GetString(0) : null;
            domain = !reader.IsDBNull(1) ? reader.GetString(1) : null;
            relativePath = !reader.IsDBNull(2) ? reader.GetString(2) : null;
            flags = !reader.IsDBNull(3) ? reader.GetInt32(3) : 0;

            return new ManifestFileInfo(fileId, domain, relativePath, flags);
        }

        // could not find a matching file
        return null;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}

public record ManifestFileInfo(string fileId, string domain, string relativePath, int flags)
{
    // Within a backup, files are named by GUIDs under subfolders of the first 2 characters, e.g.
    // ac/ac1d5a84d5b7b10cff9c6f743bde79898cf8abc2
    //
    // This function returns the path to the file content so you can open or do something with it
    public string GetContentPath(string basePath) => Path.Combine(basePath, fileId[..2], fileId);
}