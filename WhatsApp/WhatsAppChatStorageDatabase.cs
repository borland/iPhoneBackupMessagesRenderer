using Microsoft.Data.Sqlite;

namespace iPhoneBackupMessagesRenderer.WhatsApp;

public class ChatSession(
    WhatsAppChatStorageDatabase database,
    long id,
    string partnerName)
{
    public long Id { get; } = id;
    public string PartnerName { get; } = partnerName;

    public List<Message> GetMessages()
    {
        var messages = new List<Message>();

        var cmd = database.Connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT
              Z_PK,
              ZISFROMME,
              ZMESSAGEDATE,
              ZGROUPMEMBER,
              ZMEDIAITEM,
              ZTEXT,
              ZMEDIASECTIONID
            FROM ZWAMESSAGE
            WHERE ZCHATSESSION = $sessionId
            """;
        cmd.Parameters.AddWithValue("$sessionId", Id);
        
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2)) continue; // skip invalid rows; other columns are allowed to be null.

            var iter = new DbDataReaderIterator(reader);

            var id = iter.NextInt64();
            var isFromMe = iter.NextBool();
            var messageDate = Util.ConvertAppleTimestamp(iter.NextInt64()); // this is actually a floating point number, what are we supposed to do with that?
            var groupMember = iter.NextNullableInt64();
            var isMediaItem = iter.NextNullableBool() == true;
            string? text = iter.NextNullableString();
            string? mediaSectionId = iter.NextNullableString();

            if (isMediaItem && mediaSectionId != null)
            {
                messages.Add(new Message.Media(id, isFromMe, Id, groupMember, messageDate, mediaSectionId));
            }
            else if (text != null)
            {
                messages.Add(new Message.Text(id, isFromMe, Id, groupMember, messageDate, text));
            }
            // else it's not a media item and the text is null; skip it
        }
        return messages;
    }
}

public class WhatsAppChatStorageDatabase(string smsDbFilePath) : IDisposable
{
    public readonly SqliteConnection Connection = SqliteHelper.OpenDatabaseReadOnly(smsDbFilePath);

    public void Dispose()
    {
        Connection.Dispose();
    }
    
    public List<ChatSession> GetChatSessions()
    {
        var chats = new List<ChatSession>();
        var cmd = Connection.CreateCommand();
        cmd.CommandText = "SELECT Z_PK, ZPARTNERNAME FROM ZWACHATSESSION";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(1)) continue; // skip any invalid rows
            
            var iter = new DbDataReaderIterator(reader);
            chats.Add(new ChatSession(
                this,
                iter.NextInt64(),
                iter.NextString()
            ));
        }

        return chats;
    }
    
    public List<GroupMember> GetGroupMembers()
    {
        var results = new List<GroupMember>();

        var cmd = Connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT Z_PK, ZCONTACTNAME, ZMEMBERJID
            FROM ZWAGROUPMEMBER
            WHERE ZCONTACTNAME IS NOT NULL
            AND ZCONTACTNAME <> ''
            """;
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2)) continue; // skip invalid rows; other columns are allowed to be null.

            var iter = new DbDataReaderIterator(reader);
            results.Add(new GroupMember(
                iter.NextInt64(),
                iter.NextString(),
                iter.NextString()));
        }
        return results;
    }
}

//ZWAGROUPMEMBER table
public record GroupMember(
    long Id, // Z_PK
    string ContactName, // ZCONTACTNAME
    string MemberJid); // ZMEMBERJID

// ZWAMESSSAGE table
public abstract record Message(
    long Id, // PK
    bool IsFromMe, // ZISFROMME
    long SessionId, // ZCHATSESSION
    long? GroupMember, // ZGROUPMEMBER
    DateTime MessageDate // ZMESSAGEDATE
    )
{
    // If the ZMEDIAITEM column is null then it's a text message
    public record Text(
        long Id,
        bool IsFromMe,
        long SessionId,
        long? GroupMember,
        DateTime MessageDate,
        string MessageText // ZTEXT
    ) : Message(Id, IsFromMe, SessionId, GroupMember, MessageDate);
    
    // If the ZMEDIAITEM column not null then it's a media message
    public record Media(
        long Id,
        bool IsFromMe,
        long SessionId,
        long? GroupMember,
        DateTime MessageDate,
        string MediaSectionId // ZMEDIASECTIONID
    ) : Message(Id, IsFromMe, SessionId, GroupMember, MessageDate);
}