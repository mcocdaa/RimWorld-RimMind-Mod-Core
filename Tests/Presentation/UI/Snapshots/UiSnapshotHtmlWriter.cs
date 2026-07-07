using System.Globalization;
using System.Net;
using System.Text;
using RimMind.Presentation.UI.Framework;
using UnityEngine;

namespace RimMind.Tests.Presentation.UI.Snapshots;

public static class UiSnapshotHtmlWriter
{
    public static string Write(RimMindUiDocument document)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html>");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset=\"utf-8\">");
        builder.AppendLine("  <style>");
        builder.AppendLine("    .viewport { position: relative; box-sizing: border-box; }");
        builder.AppendLine("    .element { position: absolute; box-sizing: border-box; border: 1px solid #777; padding: 2px 4px; font: 12px sans-serif; overflow: hidden; }");
        builder.AppendLine("    .panel { background: #f4f4f4; }");
        builder.AppendLine("    .selected { outline: 2px solid #2f6fed; }");
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.Append("  <div class=\"viewport\" data-id=\"").Append(Escape(document.Id)).Append("\" style=\"");
        AppendRectStyle(builder, document.Viewport);
        builder.AppendLine("\">");

        foreach (RimMindUiElement element in document.Elements)
        {
            builder.Append("    <div class=\"element ")
                .Append(ClassName(element.Kind));
            if (element.Selected)
            {
                builder.Append(" selected");
            }

            builder.Append("\" data-name=\"").Append(Escape(element.Name)).Append("\" style=\"");
            AppendRectStyle(builder, element.Rect);
            builder.Append("\">").Append(Escape(element.Text)).AppendLine("</div>");
        }

        builder.AppendLine("  </div>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    private static void AppendRectStyle(StringBuilder builder, Rect rect)
    {
        builder.Append("left:").Append(Format(rect.x)).Append("px;")
            .Append("top:").Append(Format(rect.y)).Append("px;")
            .Append("width:").Append(Format(rect.width)).Append("px;")
            .Append("height:").Append(Format(rect.height)).Append("px;");
    }

    private static string ClassName(RimMindUiElementKind kind)
        => kind.ToString().ToLowerInvariant();

    private static string Escape(string value)
        => WebUtility.HtmlEncode(value);

    private static string Format(float value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);
}
