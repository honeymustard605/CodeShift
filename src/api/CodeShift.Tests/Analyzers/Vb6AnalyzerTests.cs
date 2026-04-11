using CodeShift.Core.Analyzers;
using CodeShift.Core.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CodeShift.Tests.Analyzers;

public class Vb6AnalyzerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Vb6Analyzer _analyzer;

    public Vb6AnalyzerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _analyzer = new Vb6Analyzer(NullLogger<Vb6Analyzer>.Instance);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public async Task AnalyzeAsync_WithBasFile_ReturnsVb6Language()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Module1.bas"),
            "Attribute VB_Name = \"Module1\"\nSub Main()\nEnd Sub");

        var result = await _analyzer.AnalyzeAsync(_tempDir, CancellationToken.None);

        result.Language.Should().Be("VB6");
    }

    [Fact]
    public async Task AnalyzeAsync_AlwaysIncludesVb6RuntimeRisk()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Module1.bas"), "Sub Main()\nEnd Sub");

        var result = await _analyzer.AnalyzeAsync(_tempDir, CancellationToken.None);

        result.Risks.Should().Contain(r =>
            r.Category == "VB6Runtime" && r.Level == RiskLevel.Critical);
    }

    [Fact]
    public async Task AnalyzeAsync_CreateObjectInFrm_FlagsCritical()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Form1.frm"),
            "Private Sub Form_Load()\n    Dim o = CreateObject(\"ADODB.Connection\")\nEnd Sub");

        var result = await _analyzer.AnalyzeAsync(_tempDir, CancellationToken.None);

        result.Risks.Should().Contain(r =>
            r.Category == "COMObject" && r.Level == RiskLevel.Critical);
    }

    [Fact]
    public async Task AnalyzeAsync_AdoReference_FlagsHighRisk()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Data.cls"),
            "Attribute VB_Name = \"DataModule\"\nDim rs As ADODB.Recordset");

        var result = await _analyzer.AnalyzeAsync(_tempDir, CancellationToken.None);

        result.Risks.Should().Contain(r =>
            r.Category == "ADO" && r.Level == RiskLevel.High);
    }

    [Fact]
    public void CanAnalyze_WithVbpFile_ReturnsTrue()
    {
        File.WriteAllText(Path.Combine(_tempDir, "App.vbp"), "Type=Exe");
        _analyzer.CanAnalyze(_tempDir).Should().BeTrue();
    }
}
