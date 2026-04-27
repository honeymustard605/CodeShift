using CodeShift.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace CodeShift.Core.Analyzers;

public class CSharpAnalyzer : ICodebaseAnalyzer
{
    private readonly ILogger<CSharpAnalyzer> _logger;

    public CSharpAnalyzer(ILogger<CSharpAnalyzer> logger)
    {
        _logger = logger;
    }

    public bool CanAnalyze(string rootPath) =>
        Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories).Length > 0;

    public async Task<AnalysisResult> AnalyzeAsync(string rootPath, CancellationToken cancellationToken)
    {
        var csFiles = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories);
        _logger.LogInformation("Analyzing {Count} C# files in {Root}", csFiles.Length, rootPath);

        var projects = new List<DetectedProject>();
        var edges = new List<DependencyEdge>();
        var risks = new List<RiskFlag>();
        int totalLoc = 0;

        foreach (var file in csFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var source = await File.ReadAllTextAsync(file, cancellationToken);
            var tree = CSharpSyntaxTree.ParseText(source);
            var root = await tree.GetRootAsync(cancellationToken);

            totalLoc += source.Split('\n').Length;

            // Detect using directives for edge mapping
            var usings = root.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(u => u.Name?.ToString())
                .Where(n => n is not null)
                .ToList();

            foreach (var ns in usings)
            {
                edges.Add(new DependencyEdge(
                    Source: Path.GetFileNameWithoutExtension(file),
                    Target: ns!,
                    Kind: "using"));
            }

            // Flag legacy patterns

            // WebForms
            if (source.Contains("System.Web.UI") || source.Contains("WebForms"))
                risks.Add(new RiskFlag("WebForms", file, RiskLevel.High, "WebForms page detected — no direct .NET Core equivalent."));

            if (source.Contains("IsPostBack"))
                risks.Add(new RiskFlag("WebFormsLifecycle", file, RiskLevel.High, "WebForms page lifecycle (IsPostBack) — replace with Razor Pages or MVC controller."));

            if (source.Contains("GridView") || source.Contains("DataBind()"))
                risks.Add(new RiskFlag("WebFormsControls", file, RiskLevel.High, "WebForms server controls detected — no equivalent in .NET Core."));

            if (source.Contains("Response.Redirect"))
                risks.Add(new RiskFlag("ResponseRedirect", file, RiskLevel.Low, "Response.Redirect — replace with RedirectToAction or Results.Redirect."));

            // WCF
            if (source.Contains("System.ServiceModel"))
                risks.Add(new RiskFlag("WCF", file, RiskLevel.High, "WCF service reference — consider gRPC or minimal API."));

            if (source.Contains("ChannelFactory<") || source.Contains("BasicHttpBinding") || source.Contains("EndpointAddress"))
                risks.Add(new RiskFlag("WCFClient", file, RiskLevel.Critical, "WCF client proxy detected — replace with HttpClient or gRPC client."));

            if (source.Contains("[DataContract]") || source.Contains("[DataMember]"))
                risks.Add(new RiskFlag("WCFSerialization", file, RiskLevel.Medium, "WCF DataContract serialization — replace with System.Text.Json attributes."));

            // Web API 2
            if (source.Contains("ApiController") || source.Contains("IHttpActionResult"))
                risks.Add(new RiskFlag("WebApi2", file, RiskLevel.High, "ASP.NET Web API 2 detected — migrate to ASP.NET Core controllers or minimal API."));

            // ADO.NET / data access
            if (source.Contains("System.Data.SqlClient") || source.Contains("SqlConnection") || source.Contains("SqlCommand"))
                risks.Add(new RiskFlag("RawAdoNet", file, RiskLevel.High, "Raw ADO.NET SQL access — consider migrating to EF Core or Dapper."));

            if (source.Contains("ConfigurationManager.ConnectionStrings") || source.Contains("ConfigurationManager.AppSettings"))
                risks.Add(new RiskFlag("ConfigurationManager", file, RiskLevel.Medium, "ConfigurationManager detected — replace with IConfiguration and appsettings.json."));

            // Session state
            if (source.Contains("Session[") || source.Contains("Session.Add("))
                risks.Add(new RiskFlag("SessionState", file, RiskLevel.Medium, "In-process session state — replace with IDistributedCache or cookie-based auth."));

            // Threading
            if (source.Contains("Thread.Sleep"))
                risks.Add(new RiskFlag("ThreadSleep", file, RiskLevel.Medium, "Thread.Sleep blocks the thread — replace with await Task.Delay."));

            if (source.Contains("new Thread("))
                risks.Add(new RiskFlag("RawThread", file, RiskLevel.Medium, "Raw Thread instantiation — replace with Task.Run or BackgroundService."));

            // Obsolete HTTP / networking
            if (source.Contains("WebClient"))
                risks.Add(new RiskFlag("WebClient", file, RiskLevel.Medium, "WebClient is obsolete — replace with IHttpClientFactory / HttpClient."));

            // JSON
            if (source.Contains("Newtonsoft.Json") || source.Contains("JsonConvert"))
                risks.Add(new RiskFlag("NewtonsoftJson", file, RiskLevel.Low, "Newtonsoft.Json detected — consider migrating to System.Text.Json."));

            // HttpContext
            if (source.Contains("HttpContext.Current"))
                risks.Add(new RiskFlag("HttpContextCurrent", file, RiskLevel.High, "HttpContext.Current not available in .NET Core — inject IHttpContextAccessor."));
        }

        // Group into pseudo-projects by directory depth 1
        var groupedByDir = csFiles
            .GroupBy(f => Path.GetFileName(Path.GetDirectoryName(f) ?? rootPath))
            .Select(g => new DetectedProject(
                Name: g.Key,
                Language: "C#",
                FileCount: g.Count(),
                RootPath: Path.GetDirectoryName(g.First()) ?? rootPath,
                TargetFramework: DetectTargetFramework(g.Key, rootPath)))
            .ToList();

        projects.AddRange(groupedByDir);

        return new AnalysisResult(
            Language: "C#",
            Projects: projects,
            Dependencies: edges,
            Risks: risks,
            TotalFiles: csFiles.Length,
            TotalLoc: totalLoc,
            AnalyzedAt: DateTime.UtcNow);
    }

    private static string DetectTargetFramework(string projectDir, string rootPath)
    {
        var searchPath = Path.Combine(rootPath, projectDir);
        if (!Directory.Exists(searchPath))
            return "unknown";

        var csprojFiles = Directory.GetFiles(
            searchPath,
            "*.csproj",
            SearchOption.TopDirectoryOnly);

        if (csprojFiles.Length == 0) return "unknown";

        var content = File.ReadAllText(csprojFiles[0]);
        if (content.Contains("net8")) return "net8.0";
        if (content.Contains("net6")) return "net6.0";
        if (content.Contains("netcoreapp")) return "netcoreapp";
        if (content.Contains("net4")) return "net4x";
        return "unknown";
    }
}
