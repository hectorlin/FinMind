namespace FinMind.Worker;

public sealed class DownloadOptions
{
    public const string SectionName = "Download";

    /// <summary>Stock symbols to download (K-bar via newapp.py).</summary>
    public List<string> Symbols { get; set; } = new();

    /// <summary>Start date, yyyy-MM-dd.</summary>
    public string StartDate { get; set; } = "";

    /// <summary>End date, yyyy-MM-dd.</summary>
    public string EndDate { get; set; } = "";

    /// <summary>0 = run once then exit the host; &gt; 0 = hours between runs.</summary>
    public double RepeatEveryHours { get; set; }

    /// <summary>Delay before the first run (Invariant: d.hh:mm:ss, hh:mm:ss, etc.).</summary>
    public string InitialDelay { get; set; } = "00:00:00";

    /// <summary>Python script file name in the repo root.</summary>
    public string ScriptFileName { get; set; } = "newapp.py";

    /// <summary>Optional absolute path to the folder that contains the script. If empty, search upward from the app base directory.</summary>
    public string? ScriptDirectory { get; set; }
}

public sealed class PythonOptions
{
    public const string SectionName = "Python";

    public string Executable { get; set; } = "python";
}
