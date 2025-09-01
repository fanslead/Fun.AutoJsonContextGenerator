using System.Text.Json;

namespace Fun.AutoJsonContextGenerator;

public class GeneratorConfig
{
    public List<string> Namespaces { get; set; } = new();
    public bool IncludeBaseTypes { get; set; } = true;
    public List<string> CollectionTemplates { get; set; } = new()
    {
        "System.Collections.Generic.List<{0}>",
        "{0}[]",
        "System.Collections.Generic.Dictionary<string, {0}>"
    };

    public static GeneratorConfig Load(string projectDir)
    {
        var jsonPath = Path.Combine(projectDir, "autojsonconfig.json");
        if (File.Exists(jsonPath))
        {
            return JsonSerializer.Deserialize<GeneratorConfig>(File.ReadAllText(jsonPath)) ?? new GeneratorConfig();
        }

        var editorConfigPath = Path.Combine(projectDir, ".editorconfig");
        if (File.Exists(editorConfigPath))
        {
            var cfg = new GeneratorConfig();
            foreach (var line in File.ReadAllLines(editorConfigPath))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("autojson.namespaces"))
                    cfg.Namespaces = trimmed.Split('=')[1].Trim().Split(';').Select(s => s.Trim()).ToList();
                else if (trimmed.StartsWith("autojson.includeBaseTypes"))
                    cfg.IncludeBaseTypes = bool.Parse(trimmed.Split('=')[1].Trim());
                else if (trimmed.StartsWith("autojson.collectionTemplates"))
                    cfg.CollectionTemplates = trimmed.Split('=')[1].Trim().Split(',').Select(s => s.Trim()).ToList();
            }
            return cfg;
        }

        return new GeneratorConfig();
    }
}