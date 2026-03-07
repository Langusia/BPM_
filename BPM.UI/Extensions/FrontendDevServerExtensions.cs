using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BPM.UI.Extensions;

public static class FrontendDevServerExtensions
{
    public static void StartFrontendDevServer(this WebApplication app, string frontendRelativePath)
    {
        var solutionDir = FindSolutionDirectory(AppContext.BaseDirectory);
        if (solutionDir is null) return;

        var frontendPath = Path.GetFullPath(Path.Combine(solutionDir, frontendRelativePath));
        if (!Directory.Exists(frontendPath)) return;

        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("BPM.UI.FrontendDevServer");
        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

        var nodeModulesPath = Path.Combine(frontendPath, "node_modules");
        if (!Directory.Exists(nodeModulesPath))
        {
            logger.LogInformation("Installing frontend dependencies in {Path}...", frontendPath);
            var installProcess = new System.Diagnostics.Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "npm",
                    Arguments = "install",
                    WorkingDirectory = frontendPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            installProcess.Start();
            installProcess.WaitForExit();
            if (installProcess.ExitCode != 0)
            {
                var error = installProcess.StandardError.ReadToEnd();
                logger.LogError("npm install failed: {Error}", error);
                return;
            }
        }

        logger.LogInformation("Starting frontend dev server in {Path}...", frontendPath);

        var devProcess = new System.Diagnostics.Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "npm",
                Arguments = "run dev",
                WorkingDirectory = frontendPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        devProcess.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                logger.LogInformation("[frontend] {Data}", e.Data);
        };
        devProcess.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                logger.LogWarning("[frontend] {Data}", e.Data);
        };

        devProcess.Start();
        devProcess.BeginOutputReadLine();
        devProcess.BeginErrorReadLine();

        lifetime.ApplicationStopping.Register(() =>
        {
            if (!devProcess.HasExited)
            {
                logger.LogInformation("Stopping frontend dev server...");
                try
                {
                    devProcess.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error stopping frontend dev server");
                }
            }
        });
    }

    private static string? FindSolutionDirectory(string startPath)
    {
        var dir = startPath;
        while (dir is not null)
        {
            if (Directory.GetFiles(dir, "*.sln").Length > 0)
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        return null;
    }
}
