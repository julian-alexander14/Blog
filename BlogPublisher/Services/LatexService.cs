using System.Diagnostics;

namespace BlogPublisher.Services;

public class LatexService
{
    public static async Task<string> ConvertToHtmlAsync(string latexFilePath)
    {
        try 
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "pandoc",
                Arguments = $"\"{latexFilePath}\" -f latex -t html --mathjax",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null) throw new Exception("Failed to start pandoc.");
            
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var html = await stdoutTask;
            var error = await stderrTask;

            return process.ExitCode != 0 ? throw new Exception($"Pandoc failed: {error}") : html;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting LaTeX: {ex.Message}");
            return $"<p><em>Error converting LaTeX (Pandoc might be missing): {Path.GetFileName(latexFilePath)}</em></p><pre>{await File.ReadAllTextAsync(latexFilePath)}</pre>";
        }
    }
}
