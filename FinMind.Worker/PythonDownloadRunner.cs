using System.Diagnostics;
using System.Globalization;

namespace FinMind.Worker;

internal static class PythonDownloadRunner
{
    internal static string? FindScriptDirectory(string scriptFileName, string? configuredDirectory)
    {
        if (!string.IsNullOrWhiteSpace(configuredDirectory))
        {
            var dir = Path.GetFullPath(configuredDirectory.Trim());
            var script = Path.Combine(dir, scriptFileName);
            return File.Exists(script) ? dir : null;
        }

        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var script = Path.Combine(dir.FullName, scriptFileName);
            if (File.Exists(script))
            {
                return dir.FullName;
            }
        }

        return null;
    }

    internal static async Task<ProcessRunResult> RunNewAppAsync(
        string scriptDirectory,
        string scriptFileName,
        IReadOnlyList<string> symbols,
        string startDateYmd,
        string endDateYmd,
        string configuredPythonExe,
        CancellationToken cancellationToken)
    {
        var scriptPath = Path.Combine(scriptDirectory, scriptFileName);
        if (!File.Exists(scriptPath))
        {
            return new ProcessRunResult(false, null, string.Empty, $"Script not found: {scriptPath}");
        }

        var symbolsArg = string.Join(",", symbols.Select(s => s.Trim()).Where(s => s.Length > 0));
        if (symbolsArg.Length == 0)
        {
            return new ProcessRunResult(false, null, string.Empty, "No symbols after trimming.");
        }

        var scriptArgs = new[]
        {
            $"\"{scriptPath}\"",
            "--symbols",
            $"\"{symbolsArg}\"",
            "--start-date",
            startDateYmd,
            "--end-date",
            endDateYmd
        };

        ProcessRunResult last = new(false, null, string.Empty, string.Empty);
        foreach (var candidate in GetPythonCandidates(configuredPythonExe))
        {
            cancellationToken.ThrowIfCancellationRequested();
            last = await RunProcessAsync(candidate.FileName, candidate.ArgumentsPrefix, scriptArgs, scriptDirectory, cancellationToken)
                .ConfigureAwait(false);
            if (last.Started && last.ExitCode != 9009)
            {
                return last;
            }
        }

        return last;
    }

    private static IEnumerable<PythonCommandCandidate> GetPythonCandidates(string configured)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            yield return new PythonCommandCandidate(configured.Trim(), string.Empty);
        }

        yield return new PythonCommandCandidate("python", string.Empty);
        yield return new PythonCommandCandidate("py", "-3");

        foreach (var path in DiscoverPythonExePaths())
        {
            yield return new PythonCommandCandidate(path, string.Empty);
        }
    }

    private static IEnumerable<string> DiscoverPythonExePaths()
    {
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        }.Where(static path => !string.IsNullOrWhiteSpace(path)).Distinct();

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
                    if (File.Exists(exePath))
                    {
                        yield return exePath;
                    }
                }
            }
        }
    }

    private static async Task<ProcessRunResult> RunProcessAsync(
        string fileName,
        string argumentPrefix,
        string[] scriptArgs,
        string workingDirectory,
        CancellationToken cancellationToken)
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
            WorkingDirectory = workingDirectory
        };

        try
        {
            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.Start();
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);
            return new ProcessRunResult(true, process.ExitCode, stdout, stderr);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new ProcessRunResult(false, null, string.Empty, ex.Message);
        }
    }

    internal static bool TryParseInitialDelay(string? value, out TimeSpan delay)
    {
        delay = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out delay);
    }

    internal readonly record struct ProcessRunResult(bool Started, int? ExitCode, string Stdout, string Stderr);

    private sealed record PythonCommandCandidate(string FileName, string ArgumentsPrefix);
}
