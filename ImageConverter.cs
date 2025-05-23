using Octopus.Shellfish;

namespace iPhoneBackupMessagesRenderer;

public static class ImageConverter
{
    // Replace this 
    public static string? AvifEncPath { get; set; }
    private const int AvifQuality = 50; // 60-80 are typical

    public static void HeicToAvif(string inputHeicPath, string outputAvifPath)
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            HeicToJpeg(inputHeicPath, tmpFile);
            JpegOrPngToAvif(tmpFile, outputAvifPath);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }
    
    public static void JpegOrPngToAvif(string inputJpegPath, string outputAvifPath)
    {
        var result = new ShellCommand(AvifEncPath ?? throw new InvalidOperationException("Can't convert Jpeg or PNG to avif, no path to avifenc set"))
            .WithArguments(["-q", AvifQuality.ToString(), inputJpegPath, outputAvifPath])
            .Execute();
        
        if(result.ExitCode != 0) throw new Exception($"AvifEnc failed with code: {result.ExitCode}");
    }
    
    // We can't go from a HEIC directly to an AVIF
    // This does HEIC -> JPEG so we can then call JpegToAvif.
    // We make a "best" quality JPEG under the assumption it's temporary and will be deleted after AVIF conversion
    public static void HeicToJpeg(string inputHeicPath, string outputJpegPath)
    {
        // sips is builtin to macOS
        var result = new ShellCommand("sips")
            .WithArguments(["-s", "format", "jpeg", "-s", "formatOptions", "best", inputHeicPath, "--out", outputJpegPath])
            .Execute();
        
        if(result.ExitCode != 0) throw new Exception($"sips failed with code: {result.ExitCode}");
    }
}