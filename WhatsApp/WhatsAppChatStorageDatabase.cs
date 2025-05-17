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
            
            var id = reader.GetInt64(0);
            var isFromMe = reader.GetInt32(1) == 1;
            var messageDate = Util.ConvertAppleTimestamp((long)reader.GetDouble(2)); // this is actually a floating point number, what are we supposed to do with that?
            var isMediaItem = !reader.IsDBNull(3) && reader.GetInt32(3) == 1;
            string? text = reader.IsDBNull(4) ? null : reader.GetString(4);
            string? mediaSectionId = reader.IsDBNull(5) ? null : reader.GetString(5);

            if (isMediaItem && mediaSectionId != null)
            {
                messages.Add(new Message.Media(id, isFromMe, Id, messageDate, mediaSectionId));
            }
            else if (text != null)
            {
                messages.Add(new Message.Text(id, isFromMe, Id, messageDate, text));
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
            
            chats.Add(new ChatSession(
                this,
                reader.GetInt64(0),
                reader.GetString(1)
            ));
        }

        return chats;
    }
}


// ZWAMESSSAGE table
public abstract record Message(
    long Id, // PK
    bool IsFromMe, // ZISFROMME
    long SessionId, // ZCHATSESSION
    DateTime MessageDate // ZMESSAGEDATE
    )
{
    // If the ZMEDIAITEM column is null then it's a text message
    public record Text(
        long Id,
        bool IsFromMe,
        long SessionId,
        DateTime MessageDate,
        string MessageText // ZTEXT
    ) : Message(Id, IsFromMe, SessionId, MessageDate);
    
    // If the ZMEDIAITEM column not null then it's a media message
    public record Media(
        long Id,
        bool IsFromMe,
        long SessionId,
        DateTime MessageDate,
        string MediaSectionId // ZMEDIASECTIONID
    ) : Message(Id, IsFromMe, SessionId, MessageDate);
}