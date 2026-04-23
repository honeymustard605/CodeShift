# CodeShift — Interview Reference Guide

A technical deep-dive into every major design decision in the project. Use this to explain what was built, why, and how.

---

## What Is CodeShift?

CodeShift is a **legacy .NET modernization platform**. You point it at an old codebase — C#, VB.NET, or VB6 — and it gives you:

- A **dependency graph** showing how files and namespaces relate
- A **health score** (0–100) measuring how far the codebase is from modern .NET
- A **migration roadmap** broken into phases with task-level detail
- A **code transformer** that rewrites specific legacy patterns automatically

The workflow: upload a ZIP → analysis runs → dashboard shows findings → roadmap is generated → individual files can be transformed and previewed.

---

## Architecture Overview

```
React (Vite + TypeScript)       .NET 8 Minimal API
    ↕ HTTP/JSON                      ↕
  5173 (dev)              5079/80 (dev/prod)
                               ↕
                         PostgreSQL (EF Core)
```

Three distinct deployment units: frontend, API, database. In production these go to Azure App Service + Azure Database for PostgreSQL. Locally, PostgreSQL runs in Docker while the API and React app run natively for fast hot-reload.

---

## Backend: Project Structure

The API is a **single .NET solution with four projects**, not a monolith:

| Project | Responsibility |
|---|---|
| `CodeShift.Api` | HTTP layer only — endpoints, DI wiring, middleware |
| `CodeShift.Core` | All business logic — analyzers, services, domain models. Zero web dependencies. |
| `CodeShift.Data` | EF Core DbContext, entities, migrations |
| `CodeShift.Tests` | xUnit tests against Core directly |

**Why split this way?** `Core` has no reference to `Microsoft.AspNetCore` — it can be tested, reused, or swapped without touching HTTP concerns. `Data` is isolated so the schema can evolve independently. This is the layered architecture pattern interviewers want to see.

**Why Minimal APIs instead of controllers?** .NET 8 Minimal APIs are the current recommended approach for new APIs. Less ceremony, no base class inheritance, endpoints are just delegates. Works well with the route group pattern used here.

---

## Dependency Injection Setup

`Program.cs` registers everything as **scoped** (one instance per HTTP request):

```csharp
builder.Services.AddScoped<ICodebaseAnalyzer, AnalyzerRouter>();
builder.Services.AddScoped<CSharpAnalyzer>();
builder.Services.AddScoped<VbNetAnalyzer>();
builder.Services.AddScoped<Vb6Analyzer>();
builder.Services.AddScoped<DependencyGraphBuilder>();
builder.Services.AddScoped<HealthScoreCalculator>();
builder.Services.AddScoped<RoadmapGenerator>();
builder.Services.AddScoped<TransformEngine>();
```

`ICodebaseAnalyzer` is the abstraction the endpoints talk to. `AnalyzerRouter` is the concrete implementation injected for that interface — the endpoints never know which language analyzer runs.

**Why scoped and not singleton?** The analyzers do file I/O tied to a specific request's path. Scoped ensures each request gets a clean instance. Singleton would cause shared state bugs under concurrent requests.

---

## The Analyzer Pipeline

### Strategy Pattern + Router

The core design is a **Strategy pattern** with a router on top:

```
ICodebaseAnalyzer (interface)
    └── AnalyzerRouter (router — dispatches by language)
            ├── CSharpAnalyzer
            ├── VbNetAnalyzer
            └── Vb6Analyzer
```

`AnalyzerRouter` detects the dominant language by counting files by extension, then delegates to the right analyzer. The detection logic:

```csharp
if (vb6Files.Length > vbnFiles.Length && vb6Files.Length > csFiles.Length)
    return DetectedLanguage.Vb6;
if (vbnFiles.Length > csFiles.Length)
    return DetectedLanguage.VbNet;
return DetectedLanguage.CSharp;  // default
```

Adding a new language (e.g. COBOL) means adding one new class that implements `ICodebaseAnalyzer` and one new case in the router — nothing else changes.

### CSharpAnalyzer — Roslyn AST Parsing

The C# analyzer uses **Microsoft.CodeAnalysis (Roslyn)** to parse source files into a syntax tree rather than string-searching:

```csharp
var tree = CSharpSyntaxTree.ParseText(source);
var root = await tree.GetRootAsync(cancellationToken);

var usings = root.DescendantNodes()
    .OfType<UsingDirectiveSyntax>()
    .Select(u => u.Name?.ToString())
    ...
```

This produces accurate dependency edges — it knows these are `using` directives, not comments or strings that happen to contain a namespace name.

**Legacy pattern detection:** The analyzer also string-searches for known risk signals (`System.Web.UI`, `System.ServiceModel`, `WebForms`) and records them as `RiskFlag` objects with a severity level. These become the risk table on the dashboard.

**Framework detection:** Reads `.csproj` files and pattern-matches the `TargetFramework` element to categorize projects as `net4x`, `net6.0`, `net8.0`, etc.

### Vb6Analyzer — Line-by-Line Parsing

VB6 has no parser library, so this analyzer does **line-by-line text analysis**:

- `Object=` lines in `.vbp` project files → OCX/COM dependency edges
- `CreateObject(` → Critical risk (COM object instantiation, no .NET equivalent)
- `ADODB` / `ADODC` → High risk (legacy ADO data access)
- `MSFlexGrid` → Medium risk (legacy grid control)

VB6 itself is always flagged Critical at the result level because it requires the Visual Basic 6.0 runtime, which isn't supported on modern Windows Server.

---

## Domain Models

All domain models are C# **records** — immutable value types with structural equality and built-in deconstruction:

```csharp
public record AnalysisResult(
    string Language,
    List<DetectedProject> Projects,
    List<DependencyEdge> Dependencies,
    List<RiskFlag> Risks,
    int TotalFiles,
    int TotalLoc,
    DateTime AnalyzedAt);
```

Records serialize cleanly to JSON via `System.Text.Json` with no extra attributes. The analyzer returns a domain model; the API layer serializes it. `Core` never knows about serialization.

---

## Health Score Calculator

Scores a codebase 0–100 based on risk accumulation:

| Signal | Penalty |
|---|---|
| Critical risk flag | -20 |
| High risk flag | -10 |
| Medium risk flag | -5 |
| Low risk flag | -2 |
| VB6 language | -30 |
| Any .NET 4.x project | -10 |
| All projects on .NET 8 | +10 |

Score is clamped to [0, 100]. The number gives stakeholders a single metric to track progress across migration sprints. The `RoadmapGenerator` calls `HealthScoreCalculator` internally to influence phase ordering.

---

## Roadmap Generator

Takes the `AnalysisResult` and emits a `MigrationRoadmap` with ordered phases:

1. **Inventory & Baseline** — characterization tests, document integrations (always present)
2. **VB6 → VB.NET Conversion** — only added when language is VB6
3. **Risk Remediation** — generated from the actual high/critical `RiskFlag` list, up to 10 tasks
4. **Target Framework Migration** — .csproj updates, deprecated API replacement, config migration
5. **Validation & Deployment** — integration tests, performance benchmarks, blue/green cut-over

The roadmap is deterministic given the same input — no LLM calls in this version. Each phase has an `EstimatedWeeks` that rolls up to a total. The design intentionally mirrors what a consultant would produce manually, but generated in milliseconds.

---

## TransformEngine — Roslyn AST Rewriting

The transform engine uses **Roslyn's `CSharpSyntaxRewriter`** to perform AST-level code transformations — not find/replace strings:

### Rule: `HttpContext.Current` → `IHttpContextAccessor`

```csharp
internal class HttpContextCurrentRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        if (node.ToString() == "HttpContext.Current")
            return SyntaxFactory.ParseExpression("_httpContextAccessor.HttpContext")
                .WithTriviaFrom(node);  // preserves whitespace/comments
        return base.VisitMemberAccessExpression(node);
    }
}
```

`HttpContext.Current` is a static global that doesn't exist in .NET Core. This rewrite substitutes the injected `IHttpContextAccessor` pattern. `.WithTriviaFrom(node)` preserves the original whitespace and comments — the diff is minimal.

### Rule: `Thread.Sleep` → `await Task.Delay`

Rewrites blocking sleep calls to async equivalents. The engine also emits a warning that the enclosing method must be `async` — the rewriter changes the call site but can't add the `async` keyword to the method signature automatically.

### Preview vs. Apply

`PreviewAsync` runs the full transformation but does not write to disk — the frontend calls this to show the before/after diff. `TransformAsync` calls the same core logic and then writes the file. One path, two behaviors, controlled by a single `apply` flag.

---

## Data Layer

### EF Core + PostgreSQL

`CodeShiftDbContext` is the single EF Core DbContext. Currently one entity:

```csharp
public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
```

`ProjectEntity` stores the project name, status string, and two JSON blob columns — `AnalysisJson` and `RoadmapJson`. This is a **JSON document inside a relational row** approach: the schema stays stable while the analysis and roadmap structures can evolve without migrations.

**Trade-off:** You can't query inside the JSON blobs with EF Core LINQ. For the current use case (load by project ID, deserialize in the service layer) this is fine. If querying by risk count or health score were needed, those would move to dedicated columns.

### Migrations

EF Core migrations are used for schema changes. The migration is generated from the `CodeShift.Data` project and applied via `dotnet ef database update`. Migrations are committed to source control so deployments are reproducible.

---

## API Endpoints

All endpoints use the **route group pattern** introduced in .NET 7:

```csharp
var group = app.MapGroup("/api/projects/{projectId:guid}/analysis").WithTags("Analysis");
group.MapPost("/", async (...) => { ... });
group.MapGet("/", async (...) => { ... });
```

The four endpoint files are registered in `Program.cs` via extension methods (`app.MapAnalysisEndpoints()`). Each group is tagged for Swagger grouping.

**Endpoint flow for analysis:**
1. `POST /api/projects/{id}/analysis` — receive `{ rootPath }`, load project from DB, call `ICodebaseAnalyzer.AnalyzeAsync`, serialize result to `ProjectEntity.AnalysisJson`, return the result
2. `GET /api/projects/{id}/analysis` — load project, deserialize stored JSON, return it

**Why store JSON instead of normalized tables?** Analysis results have deeply nested, variable-length lists. Normalizing them into relational tables adds joins without query benefit for this read pattern. JSON storage keeps the code simple and the API fast.

---

## Frontend

React + Vite + TypeScript + Tailwind. Key components:

| Component | What it does |
|---|---|
| `FileUpload.tsx` | Drag-and-drop ZIP upload via `react-dropzone` |
| `DependencyGraph.tsx` | Force-directed graph via `react-force-graph-2d` |
| `HealthScore.tsx` | Large score badge, color-coded by range |
| `RiskFlags.tsx` | Sortable risk table |
| `RoadmapTimeline.tsx` | Phase-by-phase migration plan |
| `DiffViewer.tsx` | Side-by-side before/after code diff for transforms |

Pages map directly to API resources: `DashboardPage` → analysis, `GraphPage` → dependencies, `RoadmapPage` → roadmap, `TransformPage` → transform preview.

**Vite proxy:** In development, Vite proxies `/api` requests to `http://localhost:5079`, so the React app and API appear same-origin. No CORS issues in dev; CORS middleware in the API handles production cross-origin calls.

---

## Infrastructure (Azure / Terraform)

Provisioned with Terraform:

- **Azure App Service** — runs the containerized .NET API (`Dockerfile.api`)
- **Azure Database for PostgreSQL Flexible Server** — managed Postgres
- **Azure Storage** — ZIP uploads, temp analysis files
- **Azure Key Vault** — connection strings, secrets (not in config files)

The `Dockerfile.api` is a multi-stage build: `sdk` image to compile, `aspnet` runtime image for the final container. Keeps the production image small.

CI/CD via GitHub Actions — `.github/workflows/deploy.yml` builds, tests, pushes the container, and deploys to App Service on merge to `main`.

---

## Key Design Decisions — Summary for Interview

| Decision | Rationale |
|---|---|
| Strategy pattern for analyzers | Open/closed — add languages without modifying existing code |
| Roslyn for C# parsing | AST is accurate; string matching produces false positives |
| Records for domain models | Immutable, clean JSON serialization, value equality for free |
| JSON blobs for analysis/roadmap | Avoids over-normalization for a read-heavy, variable-schema payload |
| Scoped DI lifetime | Request-isolated, safe for file I/O without shared state |
| Minimal APIs | Modern .NET pattern, less ceremony than MVC controllers |
| Core project has no web deps | Can be unit-tested and reused independently of the HTTP layer |
| Preview vs. Apply separation | Same transform logic, controlled by a flag — DRY without duplication |
| Health score as a number | Gives stakeholders a single trackable metric across sprints |
