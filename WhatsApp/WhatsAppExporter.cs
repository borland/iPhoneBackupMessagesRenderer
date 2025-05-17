using System.Text;

namespace iPhoneBackupMessagesRenderer.WhatsApp;

public static class WhatsAppExporter
{
    public static void Export(ManifestDatabase manifestDb, string myName, string backupBasePath,
        string outputDirectory)
    {
        // Interestingly, ChatStorage.sqlite appears at the "root" because it is in a different domain.
        // Our file search only cares about relative path though, so we're good
        var chatStorageDbFileInfo =
            manifestDb.GetFileInfo("AppDomainGroup-group.net.whatsapp.WhatsApp.shared", "ChatStorage.sqlite") ??
            throw new Exception("Can't find ChatStorage.sqlite in manifest");

        // 7c7fba66680ef796b916b067077cc246adacf01d
        using var chatStorageDb = new WhatsAppChatStorageDatabase(chatStorageDbFileInfo.GetContentPath(backupBasePath));

        Directory.CreateDirectory(outputDirectory);
        foreach (var chat in chatStorageDb.GetChatSessions())
        {
            var chatRecipient = chat.PartnerName;
            if (chatRecipient.StartsWith("+")) chatRecipient = chatRecipient.Replace("+", "").Replace(" ", "");

            var messages = chat.GetMessages();
            if (messages.Count == 0) continue; // empty chat

            WriteOutput(chat, chatRecipient, messages);
        }

        Console.WriteLine($"Export complete. Files written to: {Path.GetFullPath(outputDirectory)}");

        void WriteOutput(ChatSession chat, string chatRecipient, List<Message> messages)
        {
            var safeBaseName = $"chat_{chat.Id}_{chatRecipient}";

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset=\"utf-8\" />");
            HtmlHelper.WriteWhatsAppCss(sb);
            sb.AppendLine("</head><body>");

            foreach (var message in messages.OfType<Message.Text>())
            {
                var sender = message.IsFromMe ? myName : chatRecipient;
                var timestamp = message.MessageDate; // TODO figure this bit out

                // Lang=HTML
                sb.AppendLine(
                    $"""
                      <div class="message {(message.IsFromMe ? "from-me" : "from-them")}">
                         <div>{System.Net.WebUtility.HtmlEncode(message.MessageText)}</div>
                         <div class="subtitle">{System.Net.WebUtility.HtmlEncode(sender)} - {timestamp:G}</div>
                     """);

                // TODO attachments and media if there are any

                sb.AppendLine("</div>");
            }

            sb.AppendLine("</body></html>");

            File.WriteAllText(Path.Combine(outputDirectory, $"{safeBaseName}.html"), sb.ToString());
        }
    }
}