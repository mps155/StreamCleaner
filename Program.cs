using Serilog;
using StreamCleaner;
using StreamCleaner.Models;
using StreamCleaner.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration));

builder.Services.Configure<ScannerSettings>(
    builder.Configuration.GetSection("ScannerSettings"));

builder.Services.AddSingleton<DuplicateAnalyzer>();
builder.Services.AddSingleton<ReportWriter>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();