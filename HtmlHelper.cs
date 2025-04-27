using System.Text;

namespace iPhoneBackupMessagesRenderer;

public class HtmlHelper
{
    public static void WriteCss(StringBuilder sb)
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
}