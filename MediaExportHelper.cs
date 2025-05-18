using System.Text;

namespace iPhoneBackupMessagesRenderer;

public static class MediaExportHelper
{
    public static void CopyMediaAndEmitHtml(string contentPath, string? mimeType, string outputMediaRelativePath, string outputMediaAbsolutePath,
        StringBuilder sb)
    {
        switch (mimeType)
        {
            case "video/mp4":
            case "video/quicktime":
            case "video/3gpp":
                Console.WriteLine("Copying {0} to {1}", mimeType, outputMediaRelativePath);
                File.Copy(contentPath, outputMediaAbsolutePath, overwrite: true);

                sb.AppendLine("<video class=\"image-attachment\" controls>");

                // Even though the mimetype for .MOV movies is typically set to video/quicktime, they don't work in
                // browsers other than safari unless we lie and say the type is video/mp4.
                // Leave 3gpp alone.
                var videoMimeType = mimeType == "video/quicktime"
                    ? "video/mp4"
                    : mimeType;

                sb.AppendLine($"  <source src=\"{outputMediaRelativePath}\" type=\"{videoMimeType}\" />");
                sb.AppendLine("</video>");
                break;

            case "image/jpeg":
            case "image/png":
                try
                {
                    var newRelative = Util.ChangeFileExtension(outputMediaRelativePath, ".avif");
                    var newAbsolute = Util.ChangeFileExtension(outputMediaAbsolutePath, ".avif");

                    Console.WriteLine("Converting {0} to {1}", mimeType, newRelative);
                    ImageConverter.JpegOrPngToAvif(contentPath, newAbsolute);

                    // assume it's an image if we can't tell what else it might have been
                    sb.AppendLine($"<img class=\"image-attachment\" src=\"{newRelative}\" />");
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
                    var newRelative = Util.ChangeFileExtension(outputMediaRelativePath, ".avif");
                    var newAbsolute = Util.ChangeFileExtension(outputMediaAbsolutePath, ".avif");

                    Console.WriteLine("Converting JPEG to {0}", newRelative);
                    ImageConverter.HeicToAvif(contentPath, newAbsolute);

                    // assume it's an image if we can't tell what else it might have been
                    sb.AppendLine($"<img class=\"image-attachment\" src=\"{newRelative}\" />");
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
                // assume it's an image if we can't tell what else it might have been
                sb.AppendLine($"<img class=\"image-attachment\" src=\"{outputMediaRelativePath}\" />");
                break;
        }
    }
}