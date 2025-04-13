using System.Globalization;
using Microsoft.Data.Sqlite;
using System.Text;
using iPhoneBackupMessagesRenderer;

string smsDbPath = "..."; // path to your sms.db
string addressBookDbPath = "..."; // path to your AddressBook.sqlitedb
string outputDirectory = "output_html";

Directory.CreateDirectory(outputDirectory);

using var addressBookDb = new AddressBookDatabase(addressBookDbPath);

using var messagesDb = new MessagesDatabase(smsDbPath);

foreach (var chat in messagesDb.GetChats())
{
    string chatRecipient;
    var mainHandle = chat.GetMainHandle();
    if (addressBookDb.GuessName(mainHandle) is { } guessedName)
    {
        chatRecipient = guessedName;
    }
    else
    {
        chatRecipient = mainHandle.Replace("+", "").Replace(" ", "");
    }
    
    var messages = chat.GetMessages();
    var html = RenderHtml(chat, messages);
    var safeName = $"chat_{chat.ChatId}_{chatRecipient}.html";
    File.WriteAllText(Path.Combine(outputDirectory, safeName), html);
}

Console.WriteLine($"Export complete. Files written to: {Path.GetFullPath(outputDirectory)}");

string RenderHtml(Chat chat, List<Message> messages)
{
    var sb = new StringBuilder();
    sb.AppendLine("<!DOCTYPE html>");
    sb.AppendLine("<html><head><meta charset=\"utf-8\" />");
    sb.AppendLine("<style>");
    // Lang=css
    sb.AppendLine(
        """
        body {
            font-family: -apple-system, BlinkMacSystemFont, sans-serif;
            background-color: #f2f2f7;
            padding: 20px;
        }

        .message {
            max-width: 80%;
            padding: 10px 15px;
            border-radius: 20px;
            margin: 10px;
            display: flex;
            flex-direction: column;
            clear: both;
        }

        .from-me {
            background-color: #0b93f6;
            color: white;
            align-self: flex-end;
            margin-left: auto;
        }

        .from-them {
            background-color: #e5e5ea;
            color: black;
            align-self: flex-start;
            margin-right: auto;
        }

        .subtitle {
            font-size: 0.8em;
            opacity: 0.7;
            margin-top: 5px;
        }

        .from-me .subtitle {
            text-align: right;
        }
        """);
    sb.AppendLine("</style>");
    sb.AppendLine("</head><body>");

    sb.AppendLine($"<h2>{chat.DisplayName ?? chat.Identifier}</h2>");

    foreach (var message in messages)
    {
        var sender = addressBookDb.GuessName(message.Sender) ?? message.Sender;
        // Lang=HTML
        sb.AppendLine(
            $"""
             <div class="message {(message.IsFromMe ? "from-me" : "from-them")}">
                <div>{System.Net.WebUtility.HtmlEncode(message.Text)}</div>
                <div class="subtitle">{System.Net.WebUtility.HtmlEncode(sender)} - {message.Date:G}</div>
            </div>
            """);
    }

    sb.AppendLine("</body></html>");
    return sb.ToString();
}