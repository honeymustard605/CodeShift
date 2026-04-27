using CodeShift.Core.Services;
using CodeShift.Data;

namespace CodeShift.Api.Endpoints;

public static class TransformEndpoints
{
    private static readonly string[] Vb6Extensions = [".bas", ".cls", ".frm"];

    public static void MapTransformEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/transform").WithTags("Transform");

        group.MapPost("/", async (
            Guid projectId,
            TransformRequest req,
            TransformEngine engine,
            Vb6TransformEngine vb6Engine,
            VbNetTransformEngine vbNetEngine,
            CodeShiftDbContext db) =>
        {
            var project = await db.Projects.FindAsync(projectId);
            if (project is null) return Results.NotFound();

            var result = Route(req.FilePath) switch
            {
                Lang.Vb6   => await vb6Engine.TransformAsync(req.FilePath, req.TargetFramework, CancellationToken.None),
                Lang.VbNet => await vbNetEngine.TransformAsync(req.FilePath, req.TargetFramework, CancellationToken.None),
                _          => await engine.TransformAsync(req.FilePath, req.TargetFramework, CancellationToken.None),
            };

            return Results.Ok(result);
        });

        group.MapPost("/preview", async (
            Guid projectId,
            TransformRequest req,
            TransformEngine engine,
            Vb6TransformEngine vb6Engine,
            VbNetTransformEngine vbNetEngine,
            CodeShiftDbContext db) =>
        {
            var project = await db.Projects.FindAsync(projectId);
            if (project is null) return Results.NotFound();

            var result = Route(req.FilePath) switch
            {
                Lang.Vb6   => await vb6Engine.PreviewAsync(req.FilePath, req.TargetFramework, CancellationToken.None),
                Lang.VbNet => await vbNetEngine.PreviewAsync(req.FilePath, req.TargetFramework, CancellationToken.None),
                _          => await engine.PreviewAsync(req.FilePath, req.TargetFramework, CancellationToken.None),
            };

            return Results.Ok(result);
        });

        group.MapPost("/apply-content", async (
            Guid projectId,
            ApplyContentRequest req,
            CodeShiftDbContext db) =>
        {
            var project = await db.Projects.FindAsync(projectId);
            if (project is null) return Results.NotFound();

            if (!File.Exists(req.FilePath))
                return Results.Problem($"File not found: {req.FilePath}");

            // Write to a .cs file alongside the original
            var outPath = Path.ChangeExtension(req.FilePath, ".cs");
            await File.WriteAllTextAsync(outPath, req.Content);
            return Results.Ok(new { path = outPath });
        });

        group.MapPost("/modernize", async (
            Guid projectId,
            TransformRequest req,
            AiModernizationService ai,
            HttpContext httpContext,
            CodeShiftDbContext db) =>
        {
            var project = await db.Projects.FindAsync(projectId);
            if (project is null) return Results.NotFound();

            var userApiKey = httpContext.Request.Headers["X-Anthropic-Key"].FirstOrDefault();
            var result = await ai.ModernizeAsync(req.FilePath, req.TargetFramework, userApiKey, CancellationToken.None);
            return Results.Ok(result);
        }).RequireRateLimiting("modernize-per-ip");
    }

    private enum Lang { CSharp, Vb6, VbNet }

    private static Lang Route(string filePath) =>
        Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".bas" or ".cls" or ".frm" => Lang.Vb6,
            ".vb"                      => Lang.VbNet,
            _                          => Lang.CSharp,
        };
}

public record TransformRequest(string FilePath, string TargetFramework);
public record ApplyContentRequest(string FilePath, string Content);