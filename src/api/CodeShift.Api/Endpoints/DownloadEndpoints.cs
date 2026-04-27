using System.IO.Compression;
using CodeShift.Data;

namespace CodeShift.Api.Endpoints;

public static class DownloadEndpoints
{
    public static void MapDownloadEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}/download", async (Guid projectId, CodeShiftDbContext db) =>
        {
            var project = await db.Projects.FindAsync(projectId);
            if (project is null) return Results.NotFound();

            var extractPath = Path.Combine(Path.GetTempPath(), "codeshift", projectId.ToString());
            if (!Directory.Exists(extractPath))
                return Results.Problem("No files found for this project. Upload and analyze first.");

            var zipStream = new MemoryStream();
            ZipFile.CreateFromDirectory(extractPath, zipStream, CompressionLevel.Fastest, includeBaseDirectory: false);
            zipStream.Position = 0;

            var fileName = $"{project.Name.Replace(" ", "_")}_transformed.zip";
            return Results.File(zipStream, "application/zip", fileName);
        });

        app.MapGet("/api/projects/{projectId:guid}/download/file", async (Guid projectId, string path, CodeShiftDbContext db) =>
        {
            var project = await db.Projects.FindAsync(projectId);
            if (project is null) return Results.NotFound();

            if (!File.Exists(path))
                return Results.Problem($"File not found: {path}");

            var bytes = await File.ReadAllBytesAsync(path);
            var fileName = Path.GetFileName(path);
            return Results.File(bytes, "application/octet-stream", fileName);
        });
    }
}
