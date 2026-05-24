using Microsoft.Extensions.Options;
using StreamCleaner.Models;
using StreamCleaner.Services;

namespace StreamCleaner
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ScannerSettings _settings;
        private readonly DuplicateAnalyzer _analyzer;
        private readonly ReportWriter _reportWriter;

        public Worker(
            ILogger<Worker> logger,
            IOptions<ScannerSettings> options,
            DuplicateAnalyzer analyzer,
            ReportWriter reportWriter)
        {
            _logger = logger;
            _settings = options.Value;
            _analyzer = analyzer;
            _reportWriter = reportWriter;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Serviço iniciado. Salvando relatórios em: {Path}", _settings.OutputPath);

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_settings.RunIntervalMinutes));

            do
            {
                try
                {
                    await RunScannerAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Erro inesperado durante a varredura.");
                }

            } while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested);
        }

        private async Task RunScannerAsync(CancellationToken stoppingToken)
        {
            var duplicateGroups = await _analyzer.FindDuplicatesAsync(
                _settings.TargetDirectories,
                _settings.AllowedExtensions,
                _settings.RemoveOlderFiles,
                _settings.MoveToBackupFolder,
                stoppingToken);

            var relatorioMestre = new
            {
                ScanTime = DateTimeOffset.Now,
                ScannedDirectories = _settings.TargetDirectories,
                RemoveOlderFilesEnabled = _settings.RemoveOlderFiles,
                BackupFolder = _settings.MoveToBackupFolder,
                TotalDuplicateGroups = duplicateGroups.Count,
                Duplicates = duplicateGroups
            };

            await _reportWriter.WriteConsolidatedLogAsync(_settings.OutputPath, relatorioMestre);

            _logger.LogInformation("Varredura concluída. Grupos de duplicados encontrados: {Count}", duplicateGroups.Count);
        }
    }
}
