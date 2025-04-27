using System.Text;
using System.Text.RegularExpressions;

namespace iPhoneBackupMessagesRenderer;

public static partial class Program
{
    // We don't want to bother generating output for short code numbers, these are typically just "Your item is ready for pickup" and not worthwhile
    [GeneratedRegex("\\b\\d{4,5}\\b")]
    private static partial Regex IsSmsShortCodeRegex();

    [GeneratedRegex("\\.\\w{3,4}$")]
    private static partial Regex PathExtensionRegex();

    public static void Main()
    {
        var myName = "Orion";
        string backupBasePath = @"/Users/orion/Downloads/MobileSyncBackup/GUID_HERE";
        string outputDirectory = $"/Users/orion/Dev/output_html/{myName}";

        var manifestDbPath = Path.Combine(backupBasePath, "Manifest.db");
        using var manifestDb = new ManifestDatabase(manifestDbPath);

        var smsDbFileInfo = manifestDb.GetFileInfoByRelativePath("Library/SMS/sms.db") ??
                            throw new Exception("Can't find sms.db in manifest");

        var addressBookDbFileInfo = manifestDb.GetFileInfoByRelativePath("Library/AddressBook/AddressBook.sqlitedb") ??
                                    throw new Exception("Can't find AddressBook.sqlitedb in manifest");

        using var addressBookDb =
            new AddressBookDatabase(addressBookDbFileInfo.GetContentPath(backupBasePath), myName);

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

                .image-attachment {
                    width: 50%;
                    border-radius: 20px;
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

                    var fileInfo = manifestDb.GetFileInfoByRelativePath(attachment.FileName);
                    if (fileInfo == null) continue; // can't find the file

                    var contentPath = fileInfo.GetContentPath(backupBasePath);

                    var outputMediaRelativePath = Path.Combine("attachments", safeBaseName,
                        $"{attachment.RowId}_{attachment.TransferName}");
                    var outputMediaAbsolutePath = Path.Combine(outputDirectory, outputMediaRelativePath);

                    switch (attachment.MimeType)
                    {
                        case "image/jpeg":
                            try
                            {
                                var newRelative = ChangeFileExtension(outputMediaRelativePath, ".avif");
                                var newAbsolute = ChangeFileExtension(outputMediaAbsolutePath, ".avif");

                                Console.WriteLine("Converting JPEG to {0}", newRelative);
                                ImageConverter.JpegToAvif(contentPath, newAbsolute);
                                
                                outputMediaRelativePath = newRelative;
                                outputMediaAbsolutePath = newAbsolute;
                                break;
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Could not convert to AVIF, falling back to direct copy");
                                goto default;
                            }

                        case "image/heic":
                            try
                            {
                                var newRelative = ChangeFileExtension(outputMediaRelativePath, ".avif");
                                var newAbsolute = ChangeFileExtension(outputMediaAbsolutePath, ".avif");

                                Console.WriteLine("Converting JPEG to {0}", newRelative);
                                ImageConverter.HeicToAvif(contentPath, newAbsolute);
                                
                                outputMediaRelativePath = newRelative;
                                outputMediaAbsolutePath = newAbsolute;
                                break;
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Could not convert to AVIF, falling back to direct copy");
                                goto default;
                            }

                        default:
                            Console.WriteLine("Copying to {0}", outputMediaRelativePath);
                            File.Copy(contentPath, outputMediaAbsolutePath, overwrite: true);
                            break;
                    }

                    sb.AppendLine($"<img class=\"image-attachment\" src=\"{outputMediaRelativePath}\" />");
                }

                sb.AppendLine("</div>");
            }

            sb.AppendLine("</body></html>");

            File.WriteAllText(Path.Combine(outputDirectory, $"{safeBaseName}.html"), sb.ToString());
        }
    }

    static string ChangeFileExtension(string filePath, string newExtension)
    {
        if (!newExtension.StartsWith(".")) throw new ArgumentException("newExtension must start with '.'");
        return PathExtensionRegex().Replace(filePath, newExtension);
    }
}