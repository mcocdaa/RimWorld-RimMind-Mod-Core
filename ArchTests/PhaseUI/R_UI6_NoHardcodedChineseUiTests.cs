using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseUI;

public sealed class R_UI6_NoHardcodedChineseUiTests
{
    private static string SourceDir => ArchTestExtensions.FindSourceDirectory();

    [Fact]
    public void R_UI6_Source_ShouldNotContainHardcodedChineseStrings()
    {
        var violations = new List<string>();
        var roots = new[]
        {
            Path.Combine(SourceDir, "Infrastructure", "UI"),
            Path.Combine(SourceDir, "Presentation", "UI")
        };

        foreach (string root in roots)
        {
            foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}backup{Path.DirectorySeparatorChar}"))
                    continue;

                string text = File.ReadAllText(file);
                if (Regex.IsMatch(text, "\"[^\"\\r\\n]*[\\u4e00-\\u9fff]+[^\"\\r\\n]*\""))
                    violations.Add(file.Substring(SourceDir.Length + 1).Replace(Path.DirectorySeparatorChar, '/'));
            }
        }

        violations.Should().BeEmpty("CLEAN_UI_ERROR R-UI6: UI text must use Keyed XML translations, not hardcoded Chinese.");
    }
}
