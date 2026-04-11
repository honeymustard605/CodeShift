using CodeShift.Api.Endpoints;
using CodeShift.Core.Analyzers;
using CodeShift.Core.Services;
using CodeShift.Data;
using Microsoft.EntityFrameworkCore;

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

app.MapProjectEndpoints();
app.MapAnalysisEndpoints();
app.MapRoadmapEndpoints();
app.MapTransformEndpoints();

app.Run();
