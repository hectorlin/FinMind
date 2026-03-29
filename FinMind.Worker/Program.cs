using FinMind.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddOptions<DownloadOptions>()
    .BindConfiguration(DownloadOptions.SectionName);
builder.Services
    .AddOptions<PythonOptions>()
    .BindConfiguration(PythonOptions.SectionName);
builder.Services.AddHostedService<DownloadBackgroundService>();

var host = builder.Build();
host.Run();
