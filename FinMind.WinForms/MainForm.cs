using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

namespace FinMind.WinForms;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
        dtpEnd.Value = DateTime.Today;
        dtpStart.Value = DateTime.Today.AddDays(-7);
        TryLoadPythonFromAppsettings();
    }

    private void TryLoadPythonFromAppsettings()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("Python", out var py) ||
                !py.TryGetProperty("Executable", out var exe))
            {
                return;
            }

            var value = exe.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                txtPythonExe.Text = value.Trim();
            }
        }
        catch (JsonException)
        {
            // ignore invalid JSON
        }
    }

    private static string? FindScriptRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var script = Path.Combine(dir.FullName, "newapp.py");
            if (File.Exists(script))
            {
                return dir.FullName;
            }
        }

        return null;
    }

    private static IEnumerable<string> ParseSymbols(string text)
    {
        return text
            .Split([',', ';', '\r', '\n', '\t', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(static s => s.Trim())
            .Where(static s => s.Length > 0)
            .Distinct(StringComparer.Ordinal);
    }

    private IEnumerable<PythonCommandCandidate> GetPythonCandidates(string configured)
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

    private static ProcessRunResult RunProcess(string fileName, string argumentPrefix, string[] scriptArgs, string workingDirectory)
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
            using var process = new Process { StartInfo = psi };
            process.Start();
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return new ProcessRunResult(true, process.ExitCode, stdout, stderr);
        }
        catch (Win32Exception ex)
        {
            return new ProcessRunResult(false, null, string.Empty, ex.Message);
        }
    }

    private async void BtnRun_Click(object? sender, EventArgs e)
    {
        var symbols = ParseSymbols(txtSymbols.Text).ToList();
        if (symbols.Count == 0)
        {
            MessageBox.Show(this, "Enter at least one stock symbol.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var start = dtpStart.Value.Date;
        var end = dtpEnd.Value.Date;
        if (start > end)
        {
            MessageBox.Show(this, "Start date must be earlier than or equal to end date.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var scriptRoot = FindScriptRoot();
        if (scriptRoot is null)
        {
            MessageBox.Show(
                this,
                "Could not find newapp.py. Run the app from the FinMind repo (or place newapp.py in a parent folder of the executable).",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var scriptPath = Path.Combine(scriptRoot, "newapp.py");
        if (!File.Exists(scriptPath))
        {
            MessageBox.Show(this, $"Script not found: {scriptPath}", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var symbolsArg = string.Join(",", symbols);
        var scriptArgs = new[]
        {
            $"\"{scriptPath}\"",
            "--symbols",
            $"\"{symbolsArg}\"",
            "--start-date",
            start.ToString("yyyy-MM-dd"),
            "--end-date",
            end.ToString("yyyy-MM-dd")
        };

        btnRun.Enabled = false;
        txtOutput.Clear();
        lblHint.Text = "Running…";

        try
        {
            var configured = txtPythonExe.Text.Trim();
            var finalResult = await Task.Run(() =>
            {
                ProcessRunResult last = new(false, null, string.Empty, string.Empty);
                foreach (var candidate in GetPythonCandidates(configured).ToList())
                {
                    var runResult = RunProcess(candidate.FileName, candidate.ArgumentsPrefix, scriptArgs, scriptRoot);
                    last = runResult;
                    if (runResult.Started && runResult.ExitCode != 9009)
                    {
                        return runResult;
                    }
                }

                return last;
            }).ConfigureAwait(true);

            var combined = string.Join(
                Environment.NewLine,
                new[] { finalResult.Stdout, finalResult.Stderr }.Where(static t => !string.IsNullOrWhiteSpace(t)));
            txtOutput.Text = combined;

            if (!finalResult.Started)
            {
                lblHint.Text =
                    "Cannot start Python. Set Python path in appsettings.json (Python:Executable) or in the text box (e.g. full path to python.exe).";
                MessageBox.Show(this, lblHint.Text, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (finalResult.ExitCode == 0)
            {
                lblHint.Text = "Python finished successfully. CSV files are next to newapp.py.";
            }
            else
            {
                lblHint.Text = $"Python exited with code {finalResult.ExitCode}.";
                MessageBox.Show(this, lblHint.Text, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        finally
        {
            btnRun.Enabled = true;
        }
    }

    private void BtnOpenOutputFolder_Click(object? sender, EventArgs e)
    {
        var scriptRoot = FindScriptRoot();
        if (scriptRoot is null || !Directory.Exists(scriptRoot))
        {
            MessageBox.Show(this, "Output folder not found (newapp.py location).", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = scriptRoot,
            UseShellExecute = true
        });
    }

    private void BtnOpenLatestCsv_Click(object? sender, EventArgs e)
    {
        var scriptRoot = FindScriptRoot();
        if (scriptRoot is null || !Directory.Exists(scriptRoot))
        {
            MessageBox.Show(this, "Output folder not found.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var latest = Directory
            .GetFiles(scriptRoot, "output_*.csv", SearchOption.TopDirectoryOnly)
            .Select(static path => new FileInfo(path))
            .OrderByDescending(static f => f.LastWriteTimeUtc)
            .FirstOrDefault();

        if (latest is null)
        {
            MessageBox.Show(this, "No output_*.csv file found yet.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = latest.FullName,
            UseShellExecute = true
        });
    }

    private sealed record ProcessRunResult(bool Started, int? ExitCode, string Stdout, string Stderr);

    private sealed record PythonCommandCandidate(string FileName, string ArgumentsPrefix);
}
