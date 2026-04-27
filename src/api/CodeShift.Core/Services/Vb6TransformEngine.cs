using System.Text;
using System.Text.RegularExpressions;
using CodeShift.Core.Models;
using Microsoft.Extensions.Logging;

namespace CodeShift.Core.Services;

public class Vb6TransformEngine
{
    private static readonly string[] Vb6Extensions = [".bas", ".cls", ".frm"];
    private readonly ILogger<Vb6TransformEngine> _logger;

    public Vb6TransformEngine(ILogger<Vb6TransformEngine> logger)
    {
        _logger = logger;
    }

    public Task<TransformResult> PreviewAsync(string filePath, string targetFramework, CancellationToken cancellationToken) =>
        TransformCoreAsync(filePath, targetFramework, cancellationToken);

    public async Task<TransformResult> TransformAsync(string filePath, string targetFramework, CancellationToken cancellationToken)
    {
        var result = await TransformCoreAsync(filePath, targetFramework, cancellationToken);
        if (result.Success)
        {
            var outPath = Path.ChangeExtension(filePath, ".cs");
            await File.WriteAllTextAsync(outPath, result.TransformedSource, cancellationToken);
        }
        return result;
    }

    private async Task<TransformResult> TransformCoreAsync(string filePath, string targetFramework, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
            return Fail(filePath, targetFramework, $"File not found: {filePath}");

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (!Vb6Extensions.Contains(ext))
            return Fail(filePath, targetFramework, "Vb6TransformEngine only handles .bas, .cls, .frm files.");

        var source = await File.ReadAllTextAsync(filePath, cancellationToken);
        var lines = source.Split('\n');
        var appliedRules = new HashSet<string>();
        var warnings = new List<string>();
        var output = new StringBuilder();

        var className = Path.GetFileNameWithoutExtension(filePath);
        output.AppendLine($"// Auto-generated scaffold from VB6: {Path.GetFileName(filePath)}");
        output.AppendLine("// TODO: Review all generated code — this is a best-effort conversion.");
        output.AppendLine();
        output.AppendLine($"public class {className}");
        output.AppendLine("{");

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');
            var transformed = TransformLine(line, appliedRules, warnings);
            if (transformed is not null)
                output.AppendLine("    " + transformed);
        }

        output.AppendLine("}");

        _logger.LogInformation("VB6 scaffold generated for {File}: {Count} rules applied", filePath, appliedRules.Count);

        return new TransformResult(
            FilePath: filePath,
            OriginalSource: source,
            TransformedSource: output.ToString(),
            TargetFramework: targetFramework,
            AppliedRules: [.. appliedRules],
            Warnings: warnings,
            Success: true);
    }

    private static string? TransformLine(string line, HashSet<string> appliedRules, List<string> warnings)
    {
        var trimmed = line.TrimStart();

        // Strip VB6 file metadata — not meaningful in C#
        if (trimmed.StartsWith("VERSION ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("Attribute ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("Option Explicit", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("Begin ", StringComparison.OrdinalIgnoreCase) ||
            Regex.IsMatch(trimmed, @"^Object\s*=", RegexOptions.IgnoreCase) ||
            trimmed == "End")
            return null;

        if (string.IsNullOrWhiteSpace(trimmed))
            return string.Empty;

        // Comments
        if (trimmed.StartsWith("'"))
        {
            appliedRules.Add("' → // comment");
            return "//" + trimmed[1..];
        }

        // Closing block keywords
        if (Regex.IsMatch(trimmed, @"^End\s+(Sub|Function|If|Select)", RegexOptions.IgnoreCase))
        {
            appliedRules.Add("End Sub/Function/If → }");
            return "}";
        }
        if (trimmed.Equals("Loop", StringComparison.OrdinalIgnoreCase) ||
            Regex.IsMatch(trimmed, @"^Next(\s|$)", RegexOptions.IgnoreCase))
        {
            appliedRules.Add("Loop / Next → }");
            return "}";
        }
        if (trimmed.Equals("Else", StringComparison.OrdinalIgnoreCase))
        {
            appliedRules.Add("Else → } else {");
            return "} else {";
        }
        if (Regex.IsMatch(trimmed, @"^ElseIf\s+(.+)\s+Then$", RegexOptions.IgnoreCase))
        {
            var cond = Regex.Match(trimmed, @"^ElseIf\s+(.+)\s+Then$", RegexOptions.IgnoreCase).Groups[1].Value;
            appliedRules.Add("ElseIf → } else if (...) {");
            return $"}} else if ({ConvertCondition(cond)}) {{";
        }

        var result = trimmed;

        // Sub / Function declarations
        result = TransformDeclaration(result, appliedRules);

        // Variable declarations
        result = TransformDim(result, appliedRules);

        // MsgBox
        result = Regex.Replace(result, @"(?i)^MsgBox\s+(.+)", m =>
        {
            appliedRules.Add("MsgBox → Console.WriteLine");
            return $"Console.WriteLine({m.Groups[1].Value.Trim()});";
        });

        // Set x = CreateObject(...)
        result = Regex.Replace(result, @"(?i)^Set\s+(\w+)\s*=\s*CreateObject\((.+)\)", m =>
        {
            appliedRules.Add("CreateObject → TODO comment");
            warnings.Add($"CreateObject: map '{m.Groups[2].Value.Trim()}' to a .NET equivalent.");
            return $"// TODO: Replace COM — {m.Groups[2].Value.Trim()}\nvar {m.Groups[1].Value} = null;";
        });

        // Set x = Nothing
        result = Regex.Replace(result, @"(?i)^Set\s+(\w+)\s*=\s*Nothing", m =>
        {
            appliedRules.Add("Set x = Nothing → x = null");
            return $"{m.Groups[1].Value} = null;";
        });

        // Call statement
        result = Regex.Replace(result, @"(?i)^Call\s+(.+)", m =>
        {
            appliedRules.Add("Call → direct invocation");
            return $"{m.Groups[1].Value};";
        });

        // If ... Then
        result = Regex.Replace(result, @"(?i)^If\s+(.+)\s+Then$", m =>
        {
            appliedRules.Add("If...Then → if (...) {");
            return $"if ({ConvertCondition(m.Groups[1].Value)}) {{";
        });

        // Do While
        result = Regex.Replace(result, @"(?i)^Do While\s+(.+)", m =>
        {
            appliedRules.Add("Do While → while (...) {");
            return $"while ({ConvertCondition(m.Groups[1].Value)}) {{";
        });

        // For x = n To m
        result = Regex.Replace(result, @"(?i)^For\s+(\w+)\s*=\s*(.+?)\s+To\s+(.+)", m =>
        {
            appliedRules.Add("For...To → for (...) {");
            var v = m.Groups[1].Value;
            return $"for (int {v} = {m.Groups[2].Value.Trim()}; {v} <= {m.Groups[3].Value.Trim()}; {v}++) {{";
        });

        // Boolean / string concat
        result = Regex.Replace(result, @"\bTrue\b", "true");
        result = Regex.Replace(result, @"\bFalse\b", "false");
        result = result.Replace(" & ", " + ");

        return result;
    }

    private static string TransformDeclaration(string line, HashSet<string> appliedRules)
    {
        var sub = Regex.Match(line, @"(?i)^(Public|Private|Friend)?\s*Sub\s+(\w+)\s*\(([^)]*)\)");
        if (sub.Success)
        {
            appliedRules.Add("Sub → void method");
            var access = NormalizeAccess(sub.Groups[1].Value);
            return $"{access}void {sub.Groups[2].Value}({TransformParams(sub.Groups[3].Value)})";
        }

        var func = Regex.Match(line, @"(?i)^(Public|Private|Friend)?\s*Function\s+(\w+)\s*\(([^)]*)\)\s*As\s+(\w+)");
        if (func.Success)
        {
            appliedRules.Add("Function → typed method");
            var access = NormalizeAccess(func.Groups[1].Value);
            return $"{access}{MapType(func.Groups[4].Value)} {func.Groups[2].Value}({TransformParams(func.Groups[3].Value)})";
        }

        return line;
    }

    private static string TransformDim(string line, HashSet<string> appliedRules)
    {
        var m = Regex.Match(line, @"(?i)^(Dim|Public|Private)\s+(\w+)\s+As\s+(\w+)");
        if (m.Success)
        {
            appliedRules.Add("Dim x As Type → typed declaration");
            return $"{MapType(m.Groups[3].Value)} {m.Groups[2].Value};";
        }
        return line;
    }

    private static string ConvertCondition(string cond) =>
        cond.Replace(" And ", " && ").Replace(" Or ", " || ").Replace(" Not ", " !")
            .Replace("<>", "!=").Replace("True", "true").Replace("False", "false");

    private static string NormalizeAccess(string vb) => vb.ToLowerInvariant() switch
    {
        "public" => "public ",
        "private" => "private ",
        "friend" => "internal ",
        _ => "public "
    };

    private static string MapType(string vb) => vb.ToLowerInvariant() switch
    {
        "string" => "string",
        "integer" => "int",
        "long" => "long",
        "double" => "double",
        "single" => "float",
        "boolean" => "bool",
        "date" => "DateTime",
        "variant" => "object /* Variant */",
        "object" => "object",
        "byte" => "byte",
        _ => vb
    };

    private static string TransformParams(string vb6Params)
    {
        if (string.IsNullOrWhiteSpace(vb6Params)) return string.Empty;
        return string.Join(", ", vb6Params.Split(',').Select(p =>
        {
            var m = Regex.Match(p.Trim(), @"(?i)(\w+)\s+As\s+(\w+)");
            return m.Success ? $"{MapType(m.Groups[2].Value)} {m.Groups[1].Value}" : p.Trim();
        }));
    }

    private static TransformResult Fail(string filePath, string targetFramework, string error) =>
        new(filePath, string.Empty, string.Empty, targetFramework, [], [error], false);
}
