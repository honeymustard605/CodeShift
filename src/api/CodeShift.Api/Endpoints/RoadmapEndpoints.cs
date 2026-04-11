using CodeShift.Core.Models;
using CodeShift.Core.Services;
using CodeShift.Data;

namespace CodeShift.Api.Endpoints;

public static class RoadmapEndpoints
{
    public static void MapRoadmapEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/roadmap").WithTags("Roadmap");

        group.MapPost("/generate", async (
            Guid projectId,
            RoadmapGenerator generator,
            CodeShiftDbContext db) =>
        {
            var project = await db.Projects.FindAsync(projectId);
            if (project is null) return Results.NotFound();
            if (project.AnalysisJson is null)
                return Results.BadRequest("Run analysis before generating a roadmap.");

            var analysis = System.Text.Json.JsonSerializer.Deserialize<AnalysisResult>(project.AnalysisJson)!;
            var roadmap = await generator.GenerateAsync(analysis, CancellationToken.None);

            project.RoadmapJson = System.Text.Json.JsonSerializer.Serialize(roadmap);
            await db.SaveChangesAsync();

            return Results.Ok(roadmap);
        });

        group.MapGet("/", async (Guid projectId, CodeShiftDbContext db) =>
        {
            var project = await db.Projects.FindAsync(projectId);
            if (project is null) return Results.NotFound();
            if (project.RoadmapJson is null) return Results.NoContent();

            var roadmap = System.Text.Json.JsonSerializer.Deserialize<MigrationRoadmap>(project.RoadmapJson);
            return Results.Ok(roadmap);
        });
    }
}
