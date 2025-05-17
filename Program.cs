using iPhoneBackupMessagesRenderer.WhatsApp;

namespace iPhoneBackupMessagesRenderer;

public static class Program
{
    public static void Main()
    {
        // ***** Edit these ***** 
        
        ImageConverter.AvifEncPath = "<insert path here>";
        var myName = "Julie";
        string backupBasePath = "<insert path here>";
        string outputDirectory = $"<insert path here>/{myName}";

        // ***** Regular sourcecode hereon *****

        var manifestDbPath = Path.Combine(backupBasePath, "Manifest.db");
        using var manifestDb = new ManifestDatabase(manifestDbPath);

        var addressBookDbFileInfo = manifestDb.GetFileInfo("HomeDomain", "Library/AddressBook/AddressBook.sqlitedb") ??
                                    throw new Exception("Can't find AddressBook.sqlitedb in manifest");
        using var addressBookDb =
            new AddressBookDatabase(addressBookDbFileInfo.GetContentPath(backupBasePath), myName);

        // AppleMessagesExporter.Export(manifestDb, addressBookDb, backupBasePath, Path.Combine(outputDirectory, "Messages"));
        WhatsAppExporter.Export(manifestDb, myName, backupBasePath, Path.Combine(outputDirectory, "WhatsApp"));
    }
}