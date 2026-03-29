using Microsoft.Extensions.Options;

namespace FinMind.Worker;

public sealed class DownloadBackgroundService : BackgroundService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<DownloadBackgroundService> _logger;
    private readonly DownloadOptions _download;
    private readonly PythonOptions _python;

    public DownloadBackgroundService(
        IHostApplicationLifetime lifetime,
        ILogger<DownloadBackgroundService> logger,
        IOptions<DownloadOptions> download,
        IOptions<PythonOptions> python)
    {
        _lifetime = lifetime;
        _logger = logger;
        _download = download.Value;
        _python = python.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!PythonDownloadRunner.TryParseInitialDelay(_download.InitialDelay, out var initialDelay))
        {
            _logger.LogError("Invalid Download:InitialDelay '{Delay}'.", _download.InitialDelay);
            _lifetime.StopApplication();
            return;
        }

        if (initialDelay > TimeSpan.Zero)
        {
            _logger.LogInformation("Waiting {Delay} before first download.", initialDelay);
            try
            {
                await Task.Delay(initialDelay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        var repeat = _download.RepeatEveryHours > 0
            ? TimeSpan.FromHours(_download.RepeatEveryHours)
            : (TimeSpan?)null;

        if (repeat is not null)
        {
            _logger.LogInformation("Repeat every {Hours} hours.", _download.RepeatEveryHours);
        }
        else
        {
            _logger.LogInformation("Single run mode (RepeatEveryHours is 0); host will stop after the job.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunDownloadCycle(stoppingToken).ConfigureAwait(false);

            if (repeat is null || repeat.Value <= TimeSpan.Zero)
            {
                _lifetime.StopApplication();
                break;
            }

            _logger.LogInformation("Next run in {Delay}.", repeat.Value);
            try
            {
                await Task.Delay(repeat.Value, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunDownloadCycle(CancellationToken stoppingToken)
    {
        var symbols = _download.Symbols.Where(static s => !string.IsNullOrWhiteSpace(s)).Select(static s => s.Trim()).ToList();
        if (symbols.Count == 0)
        {
            _logger.LogError("Download:Symbols is empty. Configure at least one symbol.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_download.StartDate) || string.IsNullOrWhiteSpace(_download.EndDate))
        {
            _logger.LogError("Download:StartDate and Download:EndDate are required (yyyy-MM-dd).");
            return;
        }

        var scriptDir = PythonDownloadRunner.FindScriptDirectory(_download.ScriptFileName, _download.ScriptDirectory);
        if (scriptDir is null)
        {
            _logger.LogError(
                "Could not find {Script}. Set Download:ScriptDirectory to the repo folder or run from a path under the FinMind repo.",
                _download.ScriptFileName);
            return;
        }

        _logger.LogInformation(
            "Starting download for {Count} symbol(s), {Start}–{End}, working directory {Dir}.",
            symbols.Count,
            _download.StartDate,
            _download.EndDate,
            scriptDir);

        try
        {
            var result = await PythonDownloadRunner.RunNewAppAsync(
                    scriptDir,
                    _download.ScriptFileName,
                    symbols,
                    _download.StartDate.Trim(),
                    _download.EndDate.Trim(),
                    _python.Executable,
                    stoppingToken)
                .ConfigureAwait(false);

            if (!result.Started)
            {
                _logger.LogError("Failed to start Python: {Message}", result.Stderr);
                return;
            }

            if (!string.IsNullOrWhiteSpace(result.Stdout))
            {
                _logger.LogInformation("Python stdout:{NewLine}{Out}", Environment.NewLine, result.Stdout.TrimEnd());
            }

            if (!string.IsNullOrWhiteSpace(result.Stderr))
            {
                _logger.LogWarning("Python stderr:{NewLine}{Err}", Environment.NewLine, result.Stderr.TrimEnd());
            }

            if (result.ExitCode == 0)
            {
                _logger.LogInformation("Download finished successfully (exit 0).");
            }
            else
            {
                _logger.LogWarning("Download finished with exit code {Code}.", result.ExitCode);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Download cancelled.");
        }
    }
}
