using Microsoft.Data.Sqlite;

namespace iPhoneBackupMessagesRenderer;

public static class WhatsAppExporter
{
    public static void Export(ManifestDatabase manifestDb, AddressBookDatabase addressBookDb, string backupBasePath,
        string outputDirectory)
    {
        // Interestingly, ChatStorage.sqlite appears at the "root" because it is in a different domain.
        // Our file search only cares about relative path though, so we're good
        var chatStorageDbFileInfo = manifestDb.GetFileInfo("AppDomainGroup-group.net.whatsapp.WhatsApp.shared", "ChatStorage.sqlite") ??
                            throw new Exception("Can't find ChatStorage.sqlite in manifest");

        using var chatStorageDb = new WhatsAppChatStorageDatabase(chatStorageDbFileInfo.GetContentPath(backupBasePath));
    }
}

public class WhatsAppChatStorageDatabase(string smsDbFilePath) : IDisposable
{
    public readonly SqliteConnection Connection = SqliteHelper.OpenDatabaseReadOnly(smsDbFilePath);

    public void Dispose()
    {
        Connection.Dispose();
    }
}
