using Microsoft.Data.Sqlite;

namespace iPhoneBackupMessagesRenderer;

public static class SqliteHelper
{
    public static SqliteConnection OpenDatabaseReadOnly(string dbPath)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString();

        var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        return connection;
    }
}