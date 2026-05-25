using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StreamCleaner.Services
{
    public class ReportWriter
    {
        private readonly ILogger<ReportWriter> _logger;

        public ReportWriter(ILogger<ReportWriter> logger)
        {
            _logger = logger;
        }

        public async Task WriteLogFileAsync(string outputDirectory, string targetDir, List<List<string>> duplicateGroups)
        {
            try
            {
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                var logModel = new
                {
                    ScanTime = DateTimeOffset.Now,
                    TargetDirectory = targetDir,
                    TotalDuplicateGroups = duplicateGroups.Count,
                    Duplicates = duplicateGroups
                };

                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                string jsonContent = JsonSerializer.Serialize(logModel, jsonOptions);

                string safeDirName = Path.GetFileName(targetDir) ?? "root";
                if (string.IsNullOrWhiteSpace(safeDirName)) safeDirName = "drive";

                string fileName = $"scan_{safeDirName}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string fullPath = Path.Combine(outputDirectory, fileName);

                await File.WriteAllTextAsync(fullPath, jsonContent);

                _logger.LogInformation("Relatório de duplicados salvo com sucesso em: {Path}", fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao salvar o arquivo de log em {Directory}", outputDirectory);
            }
        }

        public async Task WriteConsolidatedLogAsync(string outputDirectory, object reportData)
        {
            try
            {
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                string jsonContent = JsonSerializer.Serialize(reportData, jsonOptions);

                string fileName = $"scan_consolidado_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string fullPath = Path.Combine(outputDirectory, fileName);

                await File.WriteAllTextAsync(fullPath, jsonContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao salvar o arquivo consolidado em {Directory}", outputDirectory);
            }
        }
    }
}
