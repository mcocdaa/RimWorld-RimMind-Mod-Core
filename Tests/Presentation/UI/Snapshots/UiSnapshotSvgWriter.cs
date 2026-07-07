using System.Globalization;
using System.Net;
using System.Text;
using RimMind.Presentation.UI.Framework;

namespace RimMind.Tests.Presentation.UI.Snapshots;

public static class UiSnapshotSvgWriter
{
    public static string Write(RimMindUiDocument document)
    {
        string width = Format(document.Viewport.width);
        string height = Format(document.Viewport.height);
        var builder = new StringBuilder();

        builder.Append("<svg xmlns=\"http://www.w3.org/2000/svg\"")
            .Append(" width=\"").Append(width).Append("\"")
            .Append(" height=\"").Append(height).Append("\"")
            .Append(" viewBox=\"")
            .Append(Format(document.Viewport.x)).Append(' ')
            .Append(Format(document.Viewport.y)).Append(' ')
            .Append(width).Append(' ')
            .Append(height).Append("\"")
            .Append(" data-id=\"").Append(Escape(document.Id)).AppendLine("\">");

        foreach (var element in document.Elements)
        {
            builder.Append("  <rect class=\"")
                .Append(ClassName(element.Kind));
            if (element.Selected)
            {
                builder.Append(" selected");
            }

            builder.Append("\" data-name=\"").Append(Escape(element.Name)).Append("\"")
                .Append(" x=\"").Append(Format(element.Rect.x)).Append("\"")
                .Append(" y=\"").Append(Format(element.Rect.y)).Append("\"")
                .Append(" width=\"").Append(Format(element.Rect.width)).Append("\"")
                .Append(" height=\"").Append(Format(element.Rect.height)).Append("\"")
                .AppendLine(" fill=\"none\" stroke=\"#777\" />");

            if (!string.IsNullOrEmpty(element.Text))
            {
                builder.Append("  <text data-name=\"").Append(Escape(element.Name)).Append("\"")
                    .Append(" x=\"").Append(Format(element.Rect.x + 4f)).Append("\"")
                    .Append(" y=\"").Append(Format(element.Rect.y + 16f)).Append("\"")
                    .Append(" font-family=\"sans-serif\" font-size=\"12\">")
                    .Append(Escape(element.Text))
                    .AppendLine("</text>");
            }
        }

        builder.AppendLine("</svg>");
        return builder.ToString();
    }

    private static string ClassName(RimMindUiElementKind kind)
        => kind.ToString().ToLowerInvariant();

    private static string Escape(string value)
        => WebUtility.HtmlEncode(value);

    private static string Format(float value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);
}
