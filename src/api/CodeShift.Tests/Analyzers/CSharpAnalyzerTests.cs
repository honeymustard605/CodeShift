using CodeShift.Core.Analyzers;
using CodeShift.Core.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CodeShift.Tests.Analyzers;

public class CSharpAnalyzerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly CSharpAnalyzer _analyzer;

    public CSharpAnalyzerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _analyzer = new CSharpAnalyzer(NullLogger<CSharpAnalyzer>.Instance);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public async Task AnalyzeAsync_WithCsFiles_ReturnsResult()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Program.cs"),
            "using System;\nConsole.WriteLine(\"hello\");");

        var result = await _analyzer.AnalyzeAsync(_tempDir, CancellationToken.None);

        result.Language.Should().Be("C#");
        result.TotalFiles.Should().Be(1);
        result.TotalLoc.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AnalyzeAsync_WebFormsFile_FlagsHighRisk()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Default.aspx.cs"),
            "using System.Web.UI;\npublic partial class Default : Page { }");

        var result = await _analyzer.AnalyzeAsync(_tempDir, CancellationToken.None);

        result.Risks.Should().Contain(r =>
            r.Category == "WebForms" && r.Level == RiskLevel.High);
    }

    [Fact]
    public async Task AnalyzeAsync_WcfFile_FlagsHighRisk()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Service.cs"),
            "using System.ServiceModel;\n[ServiceContract] public interface IService { }");

        var result = await _analyzer.AnalyzeAsync(_tempDir, CancellationToken.None);

        result.Risks.Should().Contain(r =>
            r.Category == "WCF" && r.Level == RiskLevel.High);
    }

    [Fact]
    public void CanAnalyze_WithCsFiles_ReturnsTrue()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Foo.cs"), "// empty");
        _analyzer.CanAnalyze(_tempDir).Should().BeTrue();
    }

    [Fact]
    public void CanAnalyze_EmptyDirectory_ReturnsFalse()
    {
        _analyzer.CanAnalyze(_tempDir).Should().BeFalse();
    }
}
