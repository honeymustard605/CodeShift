using Anthropic.SDK;
using CodeShift.Api.Endpoints;
using CodeShift.Core.Analyzers;
using CodeShift.Core.Services;
using CodeShift.Data;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<CodeShiftDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Core services
builder.Services.AddScoped<ICodebaseAnalyzer, AnalyzerRouter>();
builder.Services.AddScoped<CSharpAnalyzer>();
builder.Services.AddScoped<VbNetAnalyzer>();
builder.Services.AddScoped<Vb6Analyzer>();
builder.Services.AddScoped<DependencyGraphBuilder>();
builder.Services.AddScoped<HealthScoreCalculator>();
builder.Services.AddScoped<RoadmapGenerator>();
builder.Services.AddScoped<TransformEngine>();
builder.Services.AddScoped<Vb6TransformEngine>();
builder.Services.AddScoped<VbNetTransformEngine>();

var anthropicApiKey = builder.Configuration["Anthropic:ApiKey"] ?? string.Empty;
builder.Services.AddSingleton(new AnthropicClient(new Anthropic.SDK.APIAuthentication(anthropicApiKey)));
builder.Services.AddScoped<AiModernizationService>();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("modernize-per-ip", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                Window = TimeSpan.FromHours(1),
                SegmentsPerWindow = 6,
                PermitLimit = 20,
                QueueLimit = 0
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(p => p
        .WithOrigins(builder.Configuration["Cors:Origin"] ?? "http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRateLimiter();

app.MapProjectEndpoints();
app.MapAnalysisEndpoints();
app.MapRoadmapEndpoints();
app.MapTransformEndpoints();
app.MapDownloadEndpoints();

app.Run();
