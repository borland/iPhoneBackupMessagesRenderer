using System.Text;

namespace iPhoneBackupMessagesRenderer.WhatsApp;

public static class WhatsAppExporter
{
    private const string WhatsAppDomain = "AppDomainGroup-group.net.whatsapp.WhatsApp.shared";
    
    public static void Export(ManifestDatabase manifestDb, string myName, string backupBasePath,
        string outputDirectory)
    {
        // Interestingly, ChatStorage.sqlite appears at the "root" because it is in a different domain.
        // Our file search only cares about relative path though, so we're good
        var chatStorageDbFileInfo =
            manifestDb.GetFileInfo(WhatsAppDomain, "ChatStorage.sqlite") ??
            throw new Exception("Can't find ChatStorage.sqlite in manifest");

        // 7c7fba66680ef796b916b067077cc246adacf01d
        using var chatStorageDb = new WhatsAppChatStorageDatabase(chatStorageDbFileInfo.GetContentPath(backupBasePath));

        var groupMemberLookup = chatStorageDb.GetGroupMembers().ToDictionary(m => m.Id);

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

            bool createdAttachmentsDirectory = false;
            
            foreach (var message in messages)
            {
                var sender = message.IsFromMe
                    ? myName
                    : message.GroupMember is { } groupMemberId
                        ? groupMemberLookup.TryGetValue(groupMemberId, out var gm) ? gm.ContactName : "Unknown Group Member"
                        : chatRecipient; // else not a group member, must be the whole-chat recipient

                // Lang=HTML
                sb.AppendLine($"<div class=\"message {(message.IsFromMe ? "from-me" : "from-them")}\">");

                switch (message)
                {
                    case Message.Text textMessage:
                        sb.AppendLine($"<div>{System.Net.WebUtility.HtmlEncode(textMessage.MessageText)}</div>");
                        break;
                    case Message.Media mediaMessage:
                        if (!createdAttachmentsDirectory)
                        {
                            Directory.CreateDirectory(Path.Combine(outputDirectory, "attachments", safeBaseName));
                            createdAttachmentsDirectory = true;
                        }

                        if (!string.IsNullOrEmpty(mediaMessage.Title)) // some media has a title, some doesn't
                        {
                            sb.AppendLine($"<div>{System.Net.WebUtility.HtmlEncode(mediaMessage.Title)}</div>");
                        }

                        // In the WhatsApp DB the entries are like Media/447792428198@s.whatsapp.net/2/8/28c77ad8010b2956d37927954b18530b.jpg
                        // but on the iPhone filesystem they are in a subfolder e.g. Message/Media/447792428198@s.whatsapp.net/2/8/28c77ad8010b2956d37927954b18530b.jpg
                        // Sometimes they randomly have a leading / so we can't just do "Message/" + mediaMessage.LocalPath
                        var fileInfo = manifestDb.GetFileInfo(WhatsAppDomain, JoinPath("Message", mediaMessage.LocalPath)); 
                        if (fileInfo == null)
                        {
                            continue; // can't find the file
                        }
                        
                        var contentPath = fileInfo.GetContentPath(backupBasePath);
                    
                        var outputMediaRelativePath = Path.Combine("attachments", safeBaseName,
                            $"{mediaMessage.Id}_{Path.GetFileName(mediaMessage.LocalPath)}");
                        var outputMediaAbsolutePath = Path.Combine(outputDirectory, outputMediaRelativePath);
                    
                        MediaExportHelper.CopyMediaAndEmitHtml(
                            contentPath,
                            mediaMessage.MimeType,
                            outputMediaRelativePath,
                            outputMediaAbsolutePath,
                            sb);
                        break;
                }
                
                sb.AppendLine($"<div class=\"subtitle\">{System.Net.WebUtility.HtmlEncode(sender)} - {message.MessageDate:G}</div>");

                sb.AppendLine("</div>");
            }

            sb.AppendLine("</body></html>");

            File.WriteAllText(Path.Combine(outputDirectory, $"{safeBaseName}.html"), sb.ToString());
        }

        // This is approximately the same as Path.Join except it always uses / rather than the OS-specific path separator.
        // Paths on iPhones always use / even if this app were to be running on windows
        static string JoinPath(string a, string b)
        {
            var aEndsWithSlash = a.EndsWith("/");
            var bStartsWithSlash = b.StartsWith("/");

            return (aEndsWithSlash, bStartsWithSlash) switch
            {
                (false, false) => $"{a}/{b}",
                (true, false) or (false, true) => a + b,
                (true, true) => $"{a[..^1]}{b}"
            };
        }
    }
}