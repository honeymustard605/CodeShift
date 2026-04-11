using CodeShift.Core.Services;
using CodeShift.Data;

namespace CodeShift.Api.Endpoints;

public static class TransformEndpoints
{
    public static void MapTransformEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/transform").WithTags("Transform");

        group.MapPost("/", async (
            Guid projectId,
            TransformRequest req,
            TransformEngine engine,
            CodeShiftDbContext db) =>
        {
            var project = await db.Projects.FindAsync(projectId);
            if (project is null) return Results.NotFound();

            var result = await engine.TransformAsync(req.FilePath, req.TargetFramework, CancellationToken.None);
            return Results.Ok(result);
        });

        group.MapPost("/preview", async (
            Guid projectId,
            TransformRequest req,
            TransformEngine engine,
            CodeShiftDbContext db) =>
        {
            var project = await db.Projects.FindAsync(projectId);
            if (project is null) return Results.NotFound();

            var result = await engine.PreviewAsync(req.FilePath, req.TargetFramework, CancellationToken.None);
            return Results.Ok(result);
        });
    }
}

public record TransformRequest(string FilePath, string TargetFramework);
