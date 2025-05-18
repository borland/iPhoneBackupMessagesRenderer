using System.Text;
using System.Text.RegularExpressions;

namespace iPhoneBackupMessagesRenderer.AppleMessages;

// Exports SMS and iMessage messages from the Apple Messages app
public static partial class AppleMessagesExporter
{
    // We don't want to bother generating output for short code numbers, these are typically just "Your item is ready for pickup" and not worthwhile
    [GeneratedRegex("\\b\\d{4,5}\\b")]
    private static partial Regex IsSmsShortCodeRegex();

    public static void Export(ManifestDatabase manifestDb, AddressBookDatabase addressBookDb, string backupBasePath,
        string outputDirectory)
    {
        var smsDbFileInfo = manifestDb.GetFileInfo("HomeDomain", "Library/SMS/sms.db") ??
                            throw new Exception("Can't find sms.db in manifest");

        using var messagesDb = new MessagesDatabase(smsDbFileInfo.GetContentPath(backupBasePath));

        Directory.CreateDirectory(outputDirectory);
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

            if (IsSmsShortCodeRegex().IsMatch(chatRecipient)) continue;

            var messages = chat.GetMessages();
            if (messages.Count == 0) continue; // empty chat

            WriteOutput(chat, chatRecipient, messages);
        }

        Console.WriteLine($"Export complete. Files written to: {Path.GetFullPath(outputDirectory)}");

        void WriteOutput(Chat chat, string chatRecipient, List<Message> messages)
        {
            var safeBaseName = $"chat_{chat.ChatId}_{chatRecipient}";

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset=\"utf-8\" />");
            HtmlHelper.WriteAppleMessagesCss(sb);
            sb.AppendLine("</head><body>");

            sb.AppendLine($"<h2>{chat.DisplayName ?? chat.Identifier}</h2>");

            bool createdAttachmentsDirectory = false;

            foreach (var message in messages)
            {
                var sender = addressBookDb.GuessName(message.Sender) ?? message.Sender;
                var timestamp = message.Date.ToLocalTime(); // database seems to store messages in UTC
                // Lang=HTML
                sb.AppendLine(
                    $"""
                      <div class="message {(message.IsFromMe ? "from-me" : "from-them")}">
                         <div>{System.Net.WebUtility.HtmlEncode(message.Text)}</div>
                         <div class="subtitle">{System.Net.WebUtility.HtmlEncode(sender)} - {timestamp:G}</div>
                     """);

                foreach (var attachment in message.Attachments)
                {
                    if (!createdAttachmentsDirectory)
                    {
                        Directory.CreateDirectory(Path.Combine(outputDirectory, "attachments", safeBaseName));
                        createdAttachmentsDirectory = true;
                    }

                    var fileInfo = manifestDb.GetFileInfo("MediaDomain", attachment.FileName);
                    if (fileInfo == null)
                    {
                        continue; // can't find the file
                    }
                    
                    var contentPath = fileInfo.GetContentPath(backupBasePath);
                    
                    var outputMediaRelativePath = Path.Combine("attachments", safeBaseName,
                        $"{attachment.RowId}_{attachment.TransferName}");
                    var outputMediaAbsolutePath = Path.Combine(outputDirectory, outputMediaRelativePath);
                    
                    MediaExportHelper.CopyMediaAndEmitHtml(
                        contentPath,
                        attachment.MimeType,
                        outputMediaRelativePath,
                        outputMediaAbsolutePath,
                        sb);
                }

                sb.AppendLine("</div>");
            }

            sb.AppendLine("</body></html>");

            File.WriteAllText(Path.Combine(outputDirectory, $"{safeBaseName}.html"), sb.ToString());
        }
    }
}