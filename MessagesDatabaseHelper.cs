using Microsoft.Data.Sqlite;

namespace iPhoneBackupMessagesRenderer;

public static class MessagesDatabaseHelper
{
    public record Chat(int ChatId, string? DisplayName, string Identifier);
    public record Message(string Sender, string Text, DateTime Date, bool IsFromMe);
    
    public static List<Chat> GetChats(SqliteConnection conn)
    {
        var chats = new List<Chat>();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
                              SELECT c.ROWID, c.display_name, c.chat_identifier
                              FROM chat c
                          """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            chats.Add(new Chat(
                reader.GetInt32(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.GetString(2)
            ));
        }
        return chats;
    }

    public static List<Message> GetMessagesForChat(SqliteConnection conn, int chatId)
    {
        var messages = new List<Message>();

        var cmd = conn.CreateCommand();
        cmd.CommandText = """
                              SELECT
                                  m.text,
                                  m.is_from_me,
                                  m.date,
                                  h.id
                              FROM chat_message_join cmj
                              JOIN message m ON m.ROWID = cmj.message_id
                              LEFT JOIN handle h ON m.handle_id = h.ROWID
                              WHERE cmj.chat_id = $chatId
                              ORDER BY m.date
                          """;
        cmd.Parameters.AddWithValue("$chatId", chatId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string text = reader.IsDBNull(0) ? "(no text)" : reader.GetString(0);
            bool isFromMe = reader.GetInt32(1) == 1;
            long rawDate = reader.GetInt64(2);
            DateTime date = ConvertAppleTimestamp(rawDate);
            string sender = isFromMe ? "Me" : (reader.IsDBNull(3) ? "Unknown" : reader.GetString(3));

            messages.Add(new Message(sender, text, date, isFromMe));
        }
        return messages;
    }
    
    public static string GetMainHandleForChat(SqliteConnection conn, int chatId)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
                              SELECT h.id
                              FROM chat_handle_join chj
                              JOIN handle h ON chj.handle_id = h.ROWID
                              WHERE chj.chat_id = $chatId
                              ORDER BY h.id ASC
                              LIMIT 1
                          """;
        cmd.Parameters.AddWithValue("$chatId", chatId);

        var result = cmd.ExecuteScalar();
        return result?.ToString() ?? $"chat_{chatId}";
    }


    private static readonly DateTime BaseDate = new(2001, 1, 1);
    
    static DateTime ConvertAppleTimestamp(long timestamp)
    {
        if (timestamp > 100_000_000_000) // Definitely not seconds
        {
            // Probably nanoseconds — convert to seconds
            long seconds = timestamp / 1_000_000_000; // we lost resolution by rounding, don't care for this app
            return BaseDate.AddSeconds(seconds);
        }
        else
        {
            // Already in seconds
            return BaseDate.AddSeconds(timestamp);
        }
    }
}