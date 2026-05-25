using StreamCleaner.Models;
using System.Security.Cryptography;

namespace StreamCleaner.Services
{
    public class DuplicateAnalyzer
    {
        private readonly ILogger<DuplicateAnalyzer> _logger;

        public DuplicateAnalyzer(ILogger<DuplicateAnalyzer> logger)
        {
            _logger = logger;
        }

        public async Task<List<List<DuplicateFileAction>>> FindDuplicatesAsync(
            string[] directoryPaths,
            string[] allowedExtensions,
            bool removeOlderFiles,
            string moveToBackupFolder,
            CancellationToken stoppingToken)
        {
            var allFiles = new List<string>();

            foreach (var directory in directoryPaths)
            {
                if (Directory.Exists(directory))
                {
                    _logger.LogInformation("Mapeando arquivos em {Directory}...", directory);
                    allFiles.AddRange(GetFilesSafely(directory, allowedExtensions, stoppingToken));
                }
                else
                {
                    _logger.LogWarning("O diretório configurado não existe e será ignorado: {Directory}", directory);
                }
            }

            if (allFiles.Count == 0)
            {
                return new List<List<DuplicateFileAction>>();
            }

            _logger.LogInformation("Total de arquivos mapeados para análise: {Count}", allFiles.Count);

            var filesBySize = allFiles
                .Select(path => new FileInfo(path))
                .GroupBy(f => f.Length)
                .Where(g => g.Count() > 1);

            bool shouldMove = !string.IsNullOrWhiteSpace(moveToBackupFolder);
            bool shouldDelete = removeOlderFiles && !shouldMove;

            if (shouldMove && !Directory.Exists(moveToBackupFolder))
            {
                Directory.CreateDirectory(moveToBackupFolder);
            }

            var duplicateGroups = new List<List<DuplicateFileAction>>();

            foreach (var sizeGroup in filesBySize)
            {
                stoppingToken.ThrowIfCancellationRequested();

                var filesByHash = new Dictionary<string, List<string>>();

                foreach (var file in sizeGroup)
                {
                    try
                    {
                        string hash = await ComputeFileHashAsync(file.FullName, stoppingToken);

                        if (!filesByHash.ContainsKey(hash))
                            filesByHash[hash] = new List<string>();

                        filesByHash[hash].Add(file.FullName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("Erro ao ler hash de {File}: {Message}", file.FullName, ex.Message);
                    }
                }

                var duplicates = filesByHash.Values.Where(list => list.Count > 1).ToList();

                foreach (var duplicateList in duplicates)
                {
                    var actionGroup = new List<DuplicateFileAction>();

                    var fileInfos = duplicateList
                        .Select(f => new FileInfo(f))
                        .OrderByDescending(f => f.LastWriteTime)
                        .ToList();

                    for (int i = 0; i < fileInfos.Count; i++)
                    {
                        var file = fileInfos[i];
                        var action = new DuplicateFileAction
                        {
                            FilePath = file.FullName,
                            LastModified = file.LastWriteTime,
                            Status = "Mantido"
                        };

                        if (i > 0 && (shouldMove || shouldDelete))
                        {
                            try
                            {
                                if (shouldMove)
                                {
                                    string fileName = Path.GetFileNameWithoutExtension(file.FullName);
                                    string extension = Path.GetExtension(file.FullName);
                                    string uniqueSuffix = Guid.NewGuid().ToString("N")[..6];

                                    string destFileName = $"{fileName}_{uniqueSuffix}{extension}";
                                    string destPath = Path.Combine(moveToBackupFolder, destFileName);

                                    File.Move(file.FullName, destPath);
                                    action.Status = "Movido";
                                    _logger.LogInformation("BACKUP: Arquivo movido para quarentena -> {DestPath}", destPath);
                                }
                                else if (shouldDelete)
                                {
                                    File.Delete(file.FullName);
                                    action.Status = "Removido";
                                    _logger.LogInformation("LIXEIRA: Arquivo mais antigo removido -> {File}", file.FullName);
                                }
                            }
                            catch (Exception ex)
                            {
                                action.Status = shouldMove ? "Erro ao mover" : "Erro ao remover";
                                _logger.LogWarning("Falha ao processar a ação no arquivo {File}: {Message}", file.FullName, ex.Message);
                            }
                        }
                        actionGroup.Add(action);
                    }

                    duplicateGroups.Add(actionGroup);
                }
            }

            return duplicateGroups;
        }

        private async Task<string> ComputeFileHashAsync(string filePath, CancellationToken stoppingToken)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            using var sha256 = SHA256.Create();

            byte[] hashBytes = await sha256.ComputeHashAsync(stream, stoppingToken);
            return Convert.ToHexString(hashBytes);
        }

        private IEnumerable<string> GetFilesSafely(string rootDirectory, string[] allowedExtensions, CancellationToken stoppingToken)
        {
            var dirsToProcess = new Queue<string>();
            dirsToProcess.Enqueue(rootDirectory);

            while (dirsToProcess.Count > 0)
            {
                stoppingToken.ThrowIfCancellationRequested();
                string currentDir = dirsToProcess.Dequeue();

                string[] files = [];
                try
                {
                    files = Directory.GetFiles(currentDir);
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogWarning("Pasta ignorada (Acesso Negado): {Directory} | Motivo: {Message}", currentDir, ex.Message);
                    continue;
                }
                catch (DirectoryNotFoundException)
                {
                    _logger.LogWarning("Pasta ignorada (Não encontrada): {Directory}", currentDir);
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Pasta ignorada (Erro inesperado): {Directory} | Motivo: {Message}", currentDir, ex.Message);
                    continue;
                }

                foreach (var file in files)
                {
                    if (allowedExtensions.Length == 0 || allowedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                    {
                        yield return file;
                    }
                }

                try
                {
                    string[] subDirs = Directory.GetDirectories(currentDir);
                    foreach (var subDir in subDirs)
                    {
                        dirsToProcess.Enqueue(subDir);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogWarning("Subpastas ignoradas (Acesso Negado): {Directory} | Motivo: {Message}", currentDir, ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Erro ao ler subpastas de: {Directory} | Motivo: {Message}", currentDir, ex.Message);
                }
            }
        }
    }
}
