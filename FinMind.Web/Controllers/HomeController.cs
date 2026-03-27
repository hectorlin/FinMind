using System.Diagnostics;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using FinMind.Web.Models;

namespace FinMind.Web.Controllers;

public class HomeController : Controller
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public HomeController(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new FetchRequestViewModel
        {
            ErrorMessage = TempData["DownloadError"] as string ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(FetchRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.StartDate > model.EndDate)
        {
            ModelState.AddModelError(string.Empty, "Start Date must be earlier than or equal to End Date.");
            return View(model);
        }

        var configuredPythonExe = _configuration["Python:Executable"] ?? "python";
        var scriptPath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "app.py"));
        if (!System.IO.File.Exists(scriptPath))
        {
            model.ErrorMessage = $"Python script not found: {scriptPath}";
            return View(model);
        }

        var scriptArgs = new[]
        {
            $"\"{scriptPath}\"",
            "--symbol", model.Symbol.Trim(),
            "--start-date", model.StartDate!.Value.ToString("yyyy-MM-dd"),
            "--end-date", model.EndDate!.Value.ToString("yyyy-MM-dd")
        };

        ProcessRunResult finalResult = new(false, null, string.Empty, string.Empty);
        var pythonCandidates = GetPythonCandidates(configuredPythonExe).ToList();
        foreach (var candidate in pythonCandidates)
        {
            var runResult = await RunProcessAsync(candidate.FileName, candidate.ArgumentsPrefix, scriptArgs, scriptPath);
            if (runResult.Started)
            {
                finalResult = runResult;
                if (runResult.ExitCode != 9009)
                {
                    break;
                }
            }
            else
            {
                finalResult = runResult;
            }
        }

        model.CommandOutput = string.Join(
            Environment.NewLine,
            new[] { finalResult.Stdout, finalResult.Stderr }.Where(text => !string.IsNullOrWhiteSpace(text)));

        if (!finalResult.Started)
        {
            model.ErrorMessage = "Cannot start Python. Configure `Python:Executable` in appsettings.json (e.g. C:\\Python313\\python.exe) or install Python Launcher (`py`).";
        }
        else if (finalResult.ExitCode == 0)
        {
            model.OutputMessage = "Python script finished successfully.";
        }
        else
        {
            model.ErrorMessage = $"Python script failed with exit code {finalResult.ExitCode}.";
        }

        return View(model);
    }

    private IEnumerable<PythonCommandCandidate> GetPythonCandidates(string configuredPythonExe)
    {
        if (!string.IsNullOrWhiteSpace(configuredPythonExe))
        {
            yield return new PythonCommandCandidate(configuredPythonExe, string.Empty);
        }

        yield return new PythonCommandCandidate("python", string.Empty);
        yield return new PythonCommandCandidate("py", "-3");

        foreach (var path in DiscoverPythonExePaths())
        {
            yield return new PythonCommandCandidate(path, string.Empty);
        }
    }

    private IEnumerable<string> DiscoverPythonExePaths()
    {
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        }.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct();

        foreach (var root in roots)
        {
            var candidates = new[]
            {
                Path.Combine(root, "Programs", "Python"),
                Path.Combine(root, "Python")
            }.Distinct();

            foreach (var candidateDir in candidates)
            {
                if (!Directory.Exists(candidateDir))
                {
                    continue;
                }

                foreach (var versionDir in Directory.GetDirectories(candidateDir, "Python*"))
                {
                    var exePath = Path.Combine(versionDir, "python.exe");
                    if (System.IO.File.Exists(exePath))
                    {
                        yield return exePath;
                    }
                }
            }
        }
    }

    private async Task<ProcessRunResult> RunProcessAsync(string fileName, string argumentPrefix, string[] scriptArgs, string scriptPath)
    {
        var allArguments = string.IsNullOrWhiteSpace(argumentPrefix)
            ? string.Join(" ", scriptArgs)
            : $"{argumentPrefix} {string.Join(" ", scriptArgs)}";

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = allArguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? _environment.ContentRootPath
        };

        try
        {
            using var process = new Process { StartInfo = psi };
            process.Start();
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            return new ProcessRunResult(true, process.ExitCode, stdout, stderr);
        }
        catch (Win32Exception ex)
        {
            return new ProcessRunResult(false, null, string.Empty, ex.Message);
        }
    }

    private sealed record ProcessRunResult(bool Started, int? ExitCode, string Stdout, string Stderr);
    private sealed record PythonCommandCandidate(string FileName, string ArgumentsPrefix);

    [HttpGet]
    public IActionResult DownloadLatestCsv()
    {
        var scriptRoot = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, ".."));
        var latestFile = Directory
            .GetFiles(scriptRoot, "output_*.csv", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .FirstOrDefault();

        if (latestFile is null)
        {
            TempData["DownloadError"] = "No generated CSV file found yet. Please run Python first.";
            return RedirectToAction(nameof(Index));
        }

        var stream = new FileStream(latestFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return File(stream, "text/csv", latestFile.Name);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
