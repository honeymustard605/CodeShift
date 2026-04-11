using CodeShift.Data;
using CodeShift.Data.Entities;

namespace CodeShift.Api.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").WithTags("Projects");

        group.MapGet("/", async (CodeShiftDbContext db) =>
        {
            var projects = await db.Projects
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new { p.Id, p.Name, p.CreatedAt, p.Status })
                .ToListAsync();
            return Results.Ok(projects);
        });

        group.MapGet("/{id:guid}", async (Guid id, CodeShiftDbContext db) =>
        {
            var project = await db.Projects.FindAsync(id);
            return project is null ? Results.NotFound() : Results.Ok(project);
        });

        group.MapPost("/", async (CreateProjectRequest req, CodeShiftDbContext db) =>
        {
            var project = new ProjectEntity
            {
                Id = Guid.NewGuid(),
                Name = req.Name,
                CreatedAt = DateTime.UtcNow,
                Status = "pending"
            };
            db.Projects.Add(project);
            await db.SaveChangesAsync();
            return Results.Created($"/api/projects/{project.Id}", project);
        });

        group.MapDelete("/{id:guid}", async (Guid id, CodeShiftDbContext db) =>
        {
            var project = await db.Projects.FindAsync(id);
            if (project is null) return Results.NotFound();
            db.Projects.Remove(project);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}

public record CreateProjectRequest(string Name);
