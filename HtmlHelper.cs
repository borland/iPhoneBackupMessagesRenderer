using System.Text;

namespace iPhoneBackupMessagesRenderer;

public static class HtmlHelper
{
    public static void WriteAppleMessagesCss(StringBuilder sb)
    {
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
    }

    public static void WriteWhatsAppCss(StringBuilder sb)
    {
        sb.AppendLine("<style>");
        // Lang=css
        sb.AppendLine(
            """
            body {
                font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
                background-color: #ece5dd;
                padding: 20px;
            }
            
            .message {
                max-width: 75%;
                padding: 10px 14px;
                border-radius: 12px;
                margin: 10px;
                display: flex;
                flex-direction: column;
                clear: both;
                box-shadow: 0 1px 1px rgba(0, 0, 0, 0.1);
                word-wrap: break-word;
                position: relative;
            }
            
            .image-attachment {
                width: 70%;
                border-radius: 12px;
                margin-top: 5px;
            }
            
            .from-me {
                background-color: #dcf8c6;
                color: black;
                align-self: flex-end;
                margin-left: auto;
            }
            
            .from-them {
                background-color: white;
                color: black;
                align-self: flex-start;
                margin-right: auto;
            }
            
            .subtitle {
                font-size: 0.75em;
                color: #999;
                margin-top: 4px;
                text-align: right;
            }
            
            .from-them .subtitle {
                text-align: left;
            }
            """);
        sb.AppendLine("</style>");
    }
}