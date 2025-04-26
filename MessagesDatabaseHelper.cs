using Microsoft.Data.Sqlite;

namespace iPhoneBackupMessagesRenderer;

public static class MessagesDatabaseHelper
{
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