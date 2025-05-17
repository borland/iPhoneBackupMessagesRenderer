using Microsoft.Data.Sqlite;

namespace iPhoneBackupMessagesRenderer.AppleMessages;

public class Chat(MessagesDatabase database, int chatId, string? displayName, string identifier)
{
    public int ChatId { get; } = chatId;
    public string? DisplayName { get; } = displayName;
    public string Identifier { get; } = identifier;

    public string GetMainHandle()
    {
        var cmd = database.Connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT h.id
            FROM chat_handle_join chj
            JOIN handle h ON chj.handle_id = h.ROWID
            WHERE chj.chat_id = $chatId
            ORDER BY h.id ASC
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("$chatId", ChatId);

        var result = cmd.ExecuteScalar();
        return result?.ToString() ?? $"chat_{ChatId}";
    }

    public List<Message> GetMessages()
    {
        var messages = new List<Message>();

        var cmd = database.Connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT
              m.ROWID,
              m.text,
              m.is_from_me,
              m.date,
              h.id,
              att.ROWID attachmentROWID, att.filename, att.mime_type, att.transfer_name
            FROM chat_message_join cmj
            JOIN message m ON m.ROWID = cmj.message_id
            LEFT JOIN handle h ON m.handle_id = h.ROWID
            LEFT JOIN message_attachment_join maj on maj.message_id = cmj.message_id
            LEFT JOIN attachment att on att.ROWID = maj.attachment_id
            WHERE cmj.chat_id = $chatId
            ORDER BY m.date
            """;
        cmd.Parameters.AddWithValue("$chatId", ChatId);

        using var reader = cmd.ExecuteReader();
        Message? lastMessageWithAttachments = null; // buffer to accumulate attachments if a message has more than one.

        while (reader.Read())
        {
            long rowId = reader.GetInt64(0);
            string text = reader.IsDBNull(1) ? "(no text)" : reader.GetString(1);
            bool isFromMe = reader.GetInt32(2) == 1;
            long rawDate = reader.GetInt64(3);
            DateTime date = Util.ConvertAppleTimestamp(rawDate);
            string sender = isFromMe ? "Me" : (reader.IsDBNull(4) ? "Unknown" : reader.GetString(4));

            long attachmentRowId = reader.IsDBNull(5) ? 0 : reader.GetInt64(5);
            string? attachmentFilename = reader.IsDBNull(6) ? null : reader.GetString(6);
            string? attachmentMimeType = reader.IsDBNull(7) ? null : reader.GetString(7);
            string? attachmentTransferName = reader.IsDBNull(8) ? null : reader.GetString(8);

            if (attachmentFilename is not null && attachmentMimeType is not null && attachmentTransferName is not null)
            {
                // could it be another attachment for the last message?
                if (lastMessageWithAttachments is not null && lastMessageWithAttachments.RowId == rowId)
                {
                    lastMessageWithAttachments.AddAttachment(attachmentRowId, attachmentFilename, attachmentMimeType,
                        attachmentTransferName);
                }
                else // a new message with an attachment
                {
                    lastMessageWithAttachments = new Message(rowId, sender, text, date, isFromMe);
                    lastMessageWithAttachments.AddAttachment(attachmentRowId, attachmentFilename, attachmentMimeType,
                        attachmentTransferName);

                    messages.Add(lastMessageWithAttachments);
                }
            }
            else // an ordinary message with no attachments
            {
                messages.Add(new Message(rowId, sender, text, date, isFromMe));
            }
        }

        return messages;
    }
}

public record Message(long RowId, string Sender, string Text, DateTime Date, bool IsFromMe)
{
    public Attachment[] Attachments { get; private set; } = [];

    public void AddAttachment(long rowId, string attachmentFilename, string attachmentMimeType,
        string attachmentTransferName)
    {
        Attachments =
            [..Attachments, new Attachment(rowId, attachmentFilename, attachmentMimeType, attachmentTransferName)];
    }
}

public record Attachment(long RowId, string FileName, string MimeType, string TransferName);

public class MessagesDatabase(string smsDbFilePath) : IDisposable
{
    public readonly SqliteConnection Connection = SqliteHelper.OpenDatabaseReadOnly(smsDbFilePath);

    public void Dispose()
    {
        Connection.Dispose();
    }

    public List<Chat> GetChats()
    {
        var chats = new List<Chat>();
        var cmd = Connection.CreateCommand();
        cmd.CommandText = "SELECT ROWID, display_name, chat_identifier FROM chat";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            chats.Add(new Chat(
                this,
                reader.GetInt32(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.GetString(2)
            ));
        }

        return chats;
    }
}