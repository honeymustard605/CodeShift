using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using CodeShift.Core.Models;
using Microsoft.Extensions.Logging;

namespace CodeShift.Core.Services;

public class AiModernizationService
{
    private readonly AnthropicClient _client;
    private readonly ILogger<AiModernizationService> _logger;

    public AiModernizationService(AnthropicClient client, ILogger<AiModernizationService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<TransformResult> ModernizeAsync(
        string filePath, string targetFramework, string? userApiKey, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
            return Fail(filePath, targetFramework, $"File not found: {filePath}");

        var client = !string.IsNullOrWhiteSpace(userApiKey)
            ? new AnthropicClient(new Anthropic.SDK.APIAuthentication(userApiKey))
            : _client;

        var source = await File.ReadAllTextAsync(filePath, cancellationToken);
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var language = ext switch
        {
            ".cs" => "C#",
            ".vb" => "VB.NET",
            ".bas" or ".cls" or ".frm" => "VB6",
            _ => "unknown"
        };

        _logger.LogInformation("Sending {File} to Claude for AI modernization", filePath);

        var prompt = $"""
            You are a .NET migration expert. Modernize the following {language} code to {targetFramework}.

            Rules:
            - Convert to idiomatic C# if the source is VB.NET or VB6
            - Replace ADO.NET (SqlConnection/SqlCommand/DataTable) with Entity Framework Core
            - Replace COM objects (CreateObject) with .NET equivalents or TODO comments
            - Use constructor injection for dependencies (DbContext, ILogger, etc.)
            - Use async/await throughout
            - Use modern C# features (records, pattern matching, nullable reference types)
            - Add brief inline comments only where the migration decision is non-obvious
            - Return ONLY the modernized code — no explanation, no markdown fences

            Original {language} code:
            {source}
            """;

        var request = new MessageParameters
        {
            Model = "claude-haiku-4-5-20251001",
            MaxTokens = 4096,
            Messages =
            [
                new Message(RoleType.User, prompt)
            ]
        };

        var response = await client.Messages.GetClaudeMessageAsync(request, cancellationToken);
        var modernized = response.Content.OfType<TextContent>().FirstOrDefault()?.Text ?? string.Empty;

        return new TransformResult(
            FilePath: filePath,
            OriginalSource: source,
            TransformedSource: modernized,
            TargetFramework: targetFramework,
            AppliedRules: [$"AI modernization ({language} → {targetFramework} C#)"],
            Warnings: ["AI-generated code — review carefully before use."],
            Success: true);
    }

    private static TransformResult Fail(string filePath, string targetFramework, string error) =>
        new(filePath, string.Empty, string.Empty, targetFramework, [], [error], false);
}
