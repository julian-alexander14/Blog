using System.Text.Json;
using BlogPublisher.Models;
using BlogPublisher.Services;

Console.WriteLine("Starting Blog Publisher...");

// Default to "content" and "dist" in the current directory
var currentDir = Directory.GetCurrentDirectory();
var contentDir = Path.Combine(currentDir, "content");
var outputDir = Path.Combine(currentDir, "dist");
var configPath = Path.Combine(currentDir, "config.json");

if (!Directory.Exists(contentDir) && Directory.Exists(Path.Combine(currentDir, "..", "content")))
{
    contentDir = Path.Combine(currentDir, "..", "content");
    outputDir = Path.Combine(currentDir, "..", "dist");
    configPath = Path.Combine(currentDir, "..", "config.json");
}

if (args.Length >= 1) contentDir = args[0];
if (args.Length >= 2) outputDir = args[1];
if (args.Length >= 3) configPath = args[2];

Console.WriteLine($"Content Directory: {contentDir}");
Console.WriteLine($"Output Directory: {outputDir}");
Console.WriteLine($"Config File: {configPath}");

// Load configuration
SiteConfig config;
if (File.Exists(configPath))
{
    var configJson = await File.ReadAllTextAsync(configPath);
    config = JsonSerializer.Deserialize<SiteConfig>(configJson, new JsonSerializerOptions 
    { 
        PropertyNameCaseInsensitive = true 
    }) ?? throw new Exception("Failed to parse config.json");
    Console.WriteLine($"Loaded config: {config.SiteTitle}");
}
else
{
    Console.WriteLine("Warning: config.json not found. Using defaults.");
    config = new SiteConfig("My Blog", "Missing", "https://blog.missing.com", "A blog", "{{YEAR}}");
}

var generator = new GeneratorService(config);
await generator.GenerateSiteAsync(contentDir, outputDir);

Console.WriteLine("Site generation complete.");
