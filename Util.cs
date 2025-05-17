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
}