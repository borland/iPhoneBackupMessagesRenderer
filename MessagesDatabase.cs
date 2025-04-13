using Microsoft.Data.Sqlite;

namespace iPhoneBackupMessagesRenderer;

public class Chat(MessagesDatabase database, int chatId, string? displayName, string identifier)
{
    public int ChatId { get; } = chatId;
    public string? DisplayName { get; } = displayName;
    public string Identifier { get; } = identifier;

    public string GetMainHandle()
    {
        return MessagesDatabaseHelper.GetMainHandleForChat(database.Connection, ChatId);
    }
    
    public List<Message> GetMessages()
    {
        return MessagesDatabaseHelper.GetMessagesForChat(database.Connection, ChatId)
            .Select(m => new Message(m.Sender, m.Text, m.Date, m.IsFromMe))
            .ToList();
    }
}

public class Message(string sender, string text, DateTime date, bool isFromMe)
{
    public string Sender { get; } = sender;
    public string Text { get; } = text;
    public DateTime Date { get; } = date;
    public bool IsFromMe { get; } = isFromMe;
}

public class MessagesDatabase : IDisposable
{
    public readonly SqliteConnection Connection;

    public MessagesDatabase(string smsDbFilePath)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = smsDbFilePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString();

        Connection = new SqliteConnection(connectionString);
        Connection.Open();
    }

    public void Dispose()
    {
        Connection.Dispose();
    }

    public List<Chat> GetChats() => MessagesDatabaseHelper.GetChats(Connection)
        .Select(c => new Chat(this, c.ChatId, c.DisplayName, c.Identifier))
        .ToList();
}