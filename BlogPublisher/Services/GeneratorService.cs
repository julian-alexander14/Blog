using System.Text;
using BlogPublisher.Models;

namespace BlogPublisher.Services;

public partial class GeneratorService(SiteConfig config)
{
    private readonly LatexService _latexService = new();

    private const string Templte = """
                                   <!DOCTYPE html>
                                   <html lang="en">
                                   <head>
                                       <meta charset="UTF-8">
                                       <meta name="viewport" content="width=device-width, initial-scale=1.0">
                                       <title>{{TITLE}}</title>
                                       <script src="https://polyfill.io/v3/polyfill.min.js?features=es6"></script>
                                       <script id="MathJax-script" async src="https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-mml-chtml.js"></script>
                                       <style>
                                           * { margin: 0; padding: 0; box-sizing: border-box; }
                                           body { 
                                               max-width: 920px; 
                                               margin: 0 auto; 
                                               padding: 1.5rem; 
                                               font-family: sans-serif; 
                                               line-height: 1.6; 
                                               color: #000; 
                                               background: #fff;
                                           }
                                           header { margin-bottom: 1.5rem; }
                                           h1 { font-size: 1.5rem; font-weight: bold; margin-bottom: 0.5rem; }
                                           h2 { font-size: 1.25rem; font-weight: bold; margin: 1.5rem 0 0.75rem; }
                                           h3 { font-size: 1.1rem; font-weight: bold; margin: 1.25rem 0 0.5rem; }
                                           .meta { color: #555; font-size: 0.9rem; margin-bottom: 1rem; }
                                           p { margin: 0.75rem 0; }
                                           a { color: #00e; text-decoration: underline; }
                                           a:visited { color: #551a8b; }
                                           pre { 
                                               background: #f5f5f5; 
                                               border: 1px solid #ccc; 
                                               padding: 0.75rem; 
                                               overflow-x: auto; 
                                               font-size: 0.9rem;
                                               margin: 0.75rem 0;
                                           }
                                           code { font-family: monospace; font-size: 0.95em; }
                                           ul, ol { margin: 0.75rem 0; padding-left: 2rem; }
                                           li { margin: 0.25rem 0; }
                                           img { max-width: 100%; height: auto; margin: 0.75rem 0; }
                                           table { border-collapse: collapse; margin: 0.75rem 0; }
                                           th, td { border: 1px solid #ccc; padding: 0.5rem; text-align: left; }
                                           blockquote { border-left: 3px solid #ccc; margin: 0.75rem 0; padding-left: 1rem; color: #555; }
                                           footer { margin-top: 3rem; padding-top: 1rem; border-top: 1px solid #ccc; color: #555; font-size: 0.9rem; }
                                           hr { border: none; border-top: 1px solid #ccc; margin: 1.5rem 0; }
                                           nav { margin-bottom: 1rem; }
                                           nav a { margin-right: 1rem; }
                                       </style>
                                   </head>
                                   <body>
                                       {{NAV}}
                                       <header>
                                           <h1>{{TITLE}}</h1>
                                           <div class="meta">{{DATE}} — {{AUTHOR}}</div>
                                       </header>
                                       <main>
                                           {{CONTENT}}
                                       </main>
                                       <footer>
                                           {{YEAR}}
                                       </footer>
                                   </body>
                                   </html>
                                   """;

    public async Task GenerateSiteAsync(string contentDir, string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        // Copy and validate assets
        var assetsDir = Path.Combine(contentDir, "assets");
        var outputAssetsDir = Path.Combine(outputDir, "assets");
        
        if (Directory.Exists(assetsDir))
        {
            Directory.CreateDirectory(outputAssetsDir);
            foreach (var file in Directory.GetFiles(assetsDir))
            {
                var fileInfo = new FileInfo(file);
                var destFile = Path.Combine(outputAssetsDir, Path.GetFileName(file));
                
                // Validate asset
                if (fileInfo.Length == 0)
                {
                    Console.WriteLine($"Warning: Empty asset file: {file}");
                    continue;
                }
                
                if (fileInfo.Length > 10 * 1024 * 1024) // 10MB
                {
                    Console.WriteLine($"Warning: Large asset (>10MB): {file} ({fileInfo.Length / 1024 / 1024}MB)");
                }
                
                File.Copy(file, destFile, true);
                Console.WriteLine($"Copied asset: {Path.GetFileName(file)}");
            }
        }

        // Process Posts
        var postsDir = Path.Combine(contentDir, "posts");
        if (!Directory.Exists(postsDir)) return;

        var posts = new List<BlogPost>();

        foreach (var file in Directory.GetFiles(postsDir, "*.tex"))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            
            var (date, slug) = ParseFileName(fileName);
            var latexContent = await File.ReadAllTextAsync(file);
            var title = ExtractTitle(latexContent) ?? slug;
            var htmlContent = await LatexService.ConvertToHtmlAsync(file);

            var post = new BlogPost(slug, title, date, htmlContent, config.Author);
            posts.Add(post);

            var postHtml = Templte
                .Replace("{{NAV}}", "<nav><a href=\"index.html\">&larr; Home</a></nav>")
                .Replace("{{TITLE}}", post.Title)
                .Replace("{{DATE}}", post.Date.ToString("MMMM dd, yyyy"))
                .Replace("{{AUTHOR}}", post.Author)
                .Replace("{{CONTENT}}", post.ContentHtml)
                .Replace("{{YEAR}}", config.Footer.Replace("{{YEAR}}", DateTime.Now.Year.ToString()));
                
            var outputFile = Path.Combine(outputDir, $"{slug}.html");
            await File.WriteAllTextAsync(outputFile, postHtml);
            Console.WriteLine($"Generated: {outputFile}");
        }

        await GenerateIndexAsync(posts, outputDir);
    }

    private async Task GenerateIndexAsync(List<BlogPost> posts, string outputDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<ul>");
        foreach (var post in posts.OrderByDescending(p => p.Date))
        {
            sb.AppendLine($"<li><span>{post.Date:yyyy-MM-dd}</span> - <a href=\"{post.Slug}.html\">{post.Title}</a></li>");
        }
        sb.AppendLine("</ul>");

        var indexHtml = Templte
            .Replace("{{NAV}}", "")
            .Replace("{{TITLE}}", config.SiteTitle)
            .Replace("{{DATE}}", "")
            .Replace("{{AUTHOR}}", "")
            .Replace("{{CONTENT}}", sb.ToString())
            .Replace("{{YEAR}}", config.Footer.Replace("{{YEAR}}", DateTime.Now.Year.ToString()));
            
        await File.WriteAllTextAsync(Path.Combine(outputDir, "index.html"), indexHtml);
    }

    private (DateTime, string) ParseFileName(string fileName)
    {
        // Expected: 2023-10-27-my-post
        var parts = fileName.Split('-');
        if (parts.Length >= 3 && DateTime.TryParse($"{parts[0]}-{parts[1]}-{parts[2]}", out var date))
        {
            var slug = string.Join('-', parts.Skip(3));
            return (date, slug);
        }
        return (DateTime.Now, fileName);
    }

    private static string? ExtractTitle(string content)
    {
        // Look for \title{...}
        var match = TitleRegex().Match(content);
        return match.Success ? match.Groups[1].Value : null;
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"\\title\{([^}]+)\}")]
    private static partial System.Text.RegularExpressions.Regex TitleRegex();
}
