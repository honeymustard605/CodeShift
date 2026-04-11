using CodeShift.Core.Analyzers;
using CodeShift.Core.Models;
using CodeShift.Data;

namespace CodeShift.Api.Endpoints;

public static class AnalysisEndpoints
{
    public static void MapAnalysisEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/analysis").WithTags("Analysis");

        group.MapPost("/", async (
            Guid projectId,
            AnalyzeRequest req,
            ICodebaseAnalyzer analyzer,
            CodeShiftDbContext db) =>
        {
            var project = await db.Projects.FindAsync(projectId);
            if (project is null) return Results.NotFound();

            var result = await analyzer.AnalyzeAsync(req.RootPath, CancellationToken.None);

            // Persist result snapshot
            project.Status = "analyzed";
            project.AnalysisJson = System.Text.Json.JsonSerializer.Serialize(result);
            await db.SaveChangesAsync();

            return Results.Ok(result);
        });

        group.MapGet("/", async (Guid projectId, CodeShiftDbContext db) =>
        {
            var project = await db.Projects.FindAsync(projectId);
            if (project is null) return Results.NotFound();
            if (project.AnalysisJson is null) return Results.NoContent();

            var result = System.Text.Json.JsonSerializer.Deserialize<AnalysisResult>(project.AnalysisJson);
            return Results.Ok(result);
        });
    }
}

public record AnalyzeRequest(string RootPath);
