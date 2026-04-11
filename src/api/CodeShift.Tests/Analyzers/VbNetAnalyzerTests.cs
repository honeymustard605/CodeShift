using CodeShift.Core.Analyzers;
using CodeShift.Core.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CodeShift.Tests.Analyzers;

public class VbNetAnalyzerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly VbNetAnalyzer _analyzer;

    public VbNetAnalyzerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _analyzer = new VbNetAnalyzer(NullLogger<VbNetAnalyzer>.Instance);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public async Task AnalyzeAsync_WithVbFiles_ReturnsVbNetLanguage()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Module1.vb"),
            "Imports System\nModule Module1\n    Sub Main()\n    End Sub\nEnd Module");

        var result = await _analyzer.AnalyzeAsync(_tempDir, CancellationToken.None);

        result.Language.Should().Be("VB.NET");
        result.TotalFiles.Should().Be(1);
    }

    [Fact]
    public async Task AnalyzeAsync_PowerPacksImport_FlagsHighRisk()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Form1.vb"),
            "Imports Microsoft.VisualBasic.PowerPacks\nPublic Class Form1\nEnd Class");

        var result = await _analyzer.AnalyzeAsync(_tempDir, CancellationToken.None);

        result.Risks.Should().Contain(r =>
            r.Category == "VBPowerPacks" && r.Level == RiskLevel.High);
    }

    [Fact]
    public async Task AnalyzeAsync_CreateObjectCall_FlagsMediumRisk()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Legacy.vb"),
            "Dim obj = CreateObject(\"Excel.Application\")");

        var result = await _analyzer.AnalyzeAsync(_tempDir, CancellationToken.None);

        result.Risks.Should().Contain(r =>
            r.Category == "LateBinding" && r.Level == RiskLevel.Medium);
    }

    [Fact]
    public async Task AnalyzeAsync_ImportsDirectives_AddsDependencyEdges()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "Data.vb"),
            "Imports System.Data\nImports System.Data.SqlClient\nPublic Class Foo\nEnd Class");

        var result = await _analyzer.AnalyzeAsync(_tempDir, CancellationToken.None);

        result.Dependencies.Should().Contain(e => e.Target == "System.Data");
        result.Dependencies.Should().Contain(e => e.Target == "System.Data.SqlClient");
    }
}
