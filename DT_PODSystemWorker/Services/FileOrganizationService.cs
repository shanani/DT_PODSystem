// ✅ FIXED: FileOrganizationService - Updated for new settings structure
using DT_PODSystemWorker.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DT_PODSystemWorker.Services
{
    public interface IFileOrganizationService
    {
        Task<string> OrganizeFileAsync(FileProcessInfo fileInfo, int processedFileId);
        Task MoveToErrorFolderAsync(FileProcessInfo fileInfo, string errorMessage);
    }

    public class FileOrganizationService : IFileOrganizationService
    {
        private readonly ILogger<FileOrganizationService> _logger;
        private readonly WorkerSettings _settings;
        private readonly FileOrganizationSettings _orgSettings;

        public FileOrganizationService(
            ILogger<FileOrganizationService> logger,
            IOptions<WorkerSettings> settings,
            IOptions<FileOrganizationSettings> orgSettings)
        {
            _logger = logger;
            _settings = settings.Value;
            _orgSettings = orgSettings.Value;
        }

        public async Task<string> OrganizeFileAsync(FileProcessInfo fileInfo, int processedFileId)
        {
            try
            {
                var targetPath = BuildTargetPath(fileInfo);

                // Ensure target directory exists
                var targetDir = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir!);
                    _logger.LogInformation($"Created directory: {targetDir}");
                }

                // Generate unique filename if file already exists
                targetPath = EnsureUniqueFileName(targetPath);

                // Move file to organized location
                File.Move(fileInfo.FilePath, targetPath);

                _logger.LogInformation($"Organized file: {fileInfo.FileName} -> {targetPath}");

                return targetPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error organizing file: {fileInfo.FileName}");
                throw;
            }
        }

        public async Task MoveToErrorFolderAsync(FileProcessInfo fileInfo, string errorMessage)
        {
            try
            {
                var errorDir = Path.Combine(_settings.ErrorFolderPath, DateTime.Now.ToString("yyyy-MM"));

                if (!Directory.Exists(errorDir))
                {
                    Directory.CreateDirectory(errorDir);
                }

                var errorFileName = $"ERROR_{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileName(fileInfo.FilePath)}";
                var errorPath = Path.Combine(errorDir, errorFileName);

                // Move file to error folder
                File.Move(fileInfo.FilePath, errorPath);

                // Create error log file
                var logFileName = Path.ChangeExtension(errorFileName, ".log");
                var logPath = Path.Combine(errorDir, logFileName);

                await File.WriteAllTextAsync(logPath,
                    $"File: {fileInfo.FileName}\n" +
                    $"Original Path: {fileInfo.FilePath}\n" +
                    $"Template ID: {fileInfo.TemplateId}\n" +
                    $"Period ID: {fileInfo.PeriodId}\n" +
                    $"Error Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                    $"Error Message: {errorMessage}\n");

                _logger.LogWarning($"Moved file to error folder: {fileInfo.FileName} - {errorMessage}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error moving file to error folder: {fileInfo.FileName}");
            }
        }

        private string BuildTargetPath(FileProcessInfo fileInfo)
        {
            var folderStructure = _orgSettings.FolderStructure;

            // ✅ FIXED: Handle all placeholders including {Category} and {Vendor}
            folderStructure = folderStructure
                .Replace("{Category}", SanitizeFolderName(fileInfo.Category ?? "Unknown"))
                .Replace("{Vendor}", SanitizeFolderName(fileInfo.Vendor ?? "Unknown"))
                .Replace("{Department}", SanitizeFolderName(fileInfo.Department ?? "Unknown"))
                .Replace("{Template}", SanitizeFolderName(fileInfo.TemplateName ?? "Template"))
                .Replace("{PeriodId}", fileInfo.PeriodId)
                .Replace("{Year}", fileInfo.PeriodId.Substring(0, 4))
                .Replace("{Month}", fileInfo.PeriodId.Substring(4, 2));

            var targetDir = Path.Combine(_settings.ProcessedFolderPath, folderStructure);
            var targetPath = Path.Combine(targetDir, fileInfo.FileName);

            return targetPath;
        }

        private string SanitizeFolderName(string folderName)
        {
            // Remove invalid characters for folder names
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", folderName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

            // Limit length and trim
            return sanitized.Length > 50 ? sanitized.Substring(0, 50).Trim() : sanitized.Trim();
        }

        private string EnsureUniqueFileName(string filePath)
        {
            if (!File.Exists(filePath))
                return filePath;

            var directory = Path.GetDirectoryName(filePath);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);

            var counter = 1;
            string uniquePath;

            do
            {
                var uniqueFileName = $"{fileNameWithoutExt}_{counter:D3}{extension}";
                uniquePath = Path.Combine(directory!, uniqueFileName);
                counter++;
            }
            while (File.Exists(uniquePath));

            return uniquePath;
        }
    }
}