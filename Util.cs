using System.Text.RegularExpressions;

namespace iPhoneBackupMessagesRenderer;

public static partial class Util
{
    [GeneratedRegex("\\.\\w{3,4}$")]
    private static partial Regex PathExtensionRegex();

    public static string ChangeFileExtension(string filePath, string newExtension)
    {
        if (!newExtension.StartsWith(".")) throw new ArgumentException("newExtension must start with '.'");
        return PathExtensionRegex().Replace(filePath, newExtension);
    }
    
    private static readonly DateTime BaseDate = new(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static DateTime ConvertAppleTimestamp(long timestamp)
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