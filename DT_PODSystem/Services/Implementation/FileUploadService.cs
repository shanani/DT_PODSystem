using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DT_PODSystem.Data;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.Entities;
using DT_PODSystem.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Services.Implementation
{
    public class FileUploadService : IFileUploadService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<FileUploadService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _baseUploadPath;
        private readonly long _maxFileSize;
        private readonly string[] _allowedExtensions;

        public FileUploadService(
            ApplicationDbContext context,
            IConfiguration config,
            ILogger<FileUploadService> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _config = config;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;

            // Use standardized path under wwwroot
            _baseUploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "templates");
            _maxFileSize = _config.GetValue<long>("FileUpload:MaxSize", 10 * 1024 * 1024);
            _allowedExtensions = _config.GetSection("FileUpload:AllowedExtensions").Get<string[]>() ?? new[] { ".pdf" };

            // Ensure base directory exists
            Directory.CreateDirectory(_baseUploadPath);
        }



        public async Task<DeleteFileResult> DeleteFileWithAttachmentsAsync(string fileName)
        {
            var result = new DeleteFileResult();

            try
            {
                var uploadedFile = await _context.UploadedFiles 
                        .Include(ta => ta.Template) // Load template to handle primary file logic
                    .Include(f => f.PODAttachments) // ← CRITICAL: Also load POD attachments
                        .ThenInclude(pa => pa.POD) // Load POD for primary file logic
                    .FirstOrDefaultAsync(f => f.SavedFileName == fileName && f.IsActive);

                if (uploadedFile == null)
                {
                    result.Message = "File not found";
                    return result;
                }
                 
                
                var podAttachmentsToDelete = uploadedFile.PODAttachments.ToList();
                

                // ✅ Handle POD Attachments
                if (podAttachmentsToDelete.Any())
                {
                    foreach (var attachment in podAttachmentsToDelete)
                    {
                        if (attachment.IsPrimary && attachment.POD != null)
                        {
                            _logger.LogInformation("Removing primary file designation from POD {PODId}", attachment.PODId);

                            // Find another attachment to make primary (if any exist)
                            var otherPODAttachments = await _context.PODAttachments
                                .Where(pa => pa.PODId == attachment.PODId && pa.Id != attachment.Id)
                                .ToListAsync();

                            if (otherPODAttachments.Any())
                            {
                                // Make the first remaining attachment primary
                                var newPrimary = otherPODAttachments.First();
                                newPrimary.IsPrimary = true;
                                _context.PODAttachments.Update(newPrimary);
                                _logger.LogInformation("Set POD attachment {AttachmentId} as new primary for POD {PODId}",
                                    newPrimary.Id, attachment.PODId);
                            }
                        }
                    }

                    // Remove POD attachments
                    _context.PODAttachments.RemoveRange(podAttachmentsToDelete);
                }

                // Delete physical file
                if (File.Exists(uploadedFile.FilePath))
                {
                    File.Delete(uploadedFile.FilePath);
                    _logger.LogInformation("Deleted physical file: {FilePath}", uploadedFile.FilePath);
                }
                else
                {
                    _logger.LogWarning("Physical file not found: {FilePath}", uploadedFile.FilePath);
                }

                // Delete UploadedFile record
                _context.UploadedFiles.Remove(uploadedFile);

                // Save all changes in single transaction
                await _context.SaveChangesAsync();

                result.Success = true;
               

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileName}. Inner Exception: {InnerException}",
                    fileName, ex.InnerException?.Message);
                result.Message = $"Error deleting file: {ex.Message}";
                if (ex.InnerException != null)
                {
                    result.Message += $" Inner: {ex.InnerException.Message}";
                }
                return result;
            }
        }


        public async Task<FileUploadDto> UploadFileAsync(IFormFile file, FileUploadRequest? request = null)
        {
            try
            {
                if (!await ValidateFileAsync(file))
                {
                    return new FileUploadDto
                    {
                        Success = false,
                        Message = "File validation failed",
                        ValidationErrors = new List<string> { "Invalid file type or size" }
                    };
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var monthFolder = request?.UseMonthlyFolders == true ? DateTime.UtcNow.ToString("yyyy-MM") : "";

                // Create storage directory
                var storageDir = string.IsNullOrEmpty(monthFolder)
                    ? _baseUploadPath
                    : Path.Combine(_baseUploadPath, monthFolder);

                Directory.CreateDirectory(storageDir);

                var fullPath = Path.Combine(storageDir, fileName);
                var relativePath = GetRelativePath(fullPath);

                string hash;

                // Calculate hash while copying file - more efficient
                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                using (var uploadStream = file.OpenReadStream())
                {
                    using var sha256 = SHA256.Create();

                    // Copy and hash simultaneously
                    var buffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = await uploadStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        sha256.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                    }

                    sha256.TransformFinalBlock(new byte[0], 0, 0);
                    hash = Convert.ToBase64String(sha256.Hash);
                }

                var uploadedFile = new UploadedFile
                {
                    OriginalFileName = file.FileName,
                    SavedFileName = fileName,
                    FilePath = fullPath,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    FileHash = hash,
                    IsTemporary = request?.IsTemporary ?? false, // Default to permanent storage
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                };

                _context.UploadedFiles.Add(uploadedFile);
                await _context.SaveChangesAsync();

                var result = new FileUploadDto
                {
                    Success = true,
                    OriginalFileName = file.FileName,
                    SavedFileName = fileName,
                    FilePath = fullPath,
                    RelativePath = relativePath,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    FileHash = hash,
                    UploadedAt = DateTime.UtcNow,
                    MonthFolder = monthFolder
                };

                if (file.ContentType == "application/pdf")
                {
                    result.PageCount = await GetPdfPageCountAsync(fullPath);
                    result.PdfVersion = await GetPdfVersionAsync(fullPath);
                    result.HasFormFields = await CheckPdfFormFieldsAsync(fullPath);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload failed for file {FileName}", file.FileName);
                return new FileUploadDto { Success = false, Message = "Upload failed: " + ex.Message };
            }
        }

        public async Task<bool> DeleteFileAsync(string fileName)
        {
            try
            {
                var uploadedFile = await _context.UploadedFiles
                    .FirstOrDefaultAsync(f => f.SavedFileName == fileName && f.IsActive);

                if (uploadedFile != null)
                {
                    if (File.Exists(uploadedFile.FilePath))
                    {
                        File.Delete(uploadedFile.FilePath);
                    }

                    _context.UploadedFiles.Remove(uploadedFile);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file {FileName}", fileName);
                return false;
            }
        }

        public async Task<bool> DeleteTempFileAsync(string fileName)
        {
            // Redirect to new DeleteFileAsync for backward compatibility
            return await DeleteFileAsync(fileName);
        }

        public async Task<List<FileUploadDto>> GetUploadedFilesAsync(string monthFolder = null)
        {
            var query = _context.UploadedFiles.Where(f => f.IsActive);

            if (!string.IsNullOrEmpty(monthFolder))
            {
                // Filter by month folder if specified
                query = query.Where(f => f.FilePath.Contains(monthFolder));
            }

            var files = await query.OrderByDescending(f => f.CreatedDate).ToListAsync();

            return files.Select(f => new FileUploadDto
            {
                Success = true,
                OriginalFileName = f.OriginalFileName,
                SavedFileName = f.SavedFileName,
                FilePath = f.FilePath,
                RelativePath = GetRelativePath(f.FilePath),
                ContentType = f.ContentType,
                FileSize = f.FileSize,
                FileHash = f.FileHash,
                UploadedAt = f.CreatedDate,
                MonthFolder = ExtractMonthFolder(f.FilePath)
            }).ToList();
        }

        public async Task<List<FileUploadDto>> GetTempFilesAsync()
        {
            // Redirect to new GetUploadedFilesAsync for backward compatibility
            return await GetUploadedFilesAsync();
        }

        public async Task<bool> ValidateFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return false;
            if (file.Length > _maxFileSize) return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension)) return false;

            using var stream = file.OpenReadStream();
            var buffer = new byte[512];
            await stream.ReadAsync(buffer, 0, 512);

            if (extension == ".pdf" && !IsPdfFile(buffer)) return false;

            return true;
        }

        public async Task<bool> CleanupFilesAsync(int olderThanDays = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
                var expiredFiles = await _context.UploadedFiles
                    .Where(f => f.CreatedDate < cutoffDate && f.IsActive)
                    .ToListAsync();

                foreach (var file in expiredFiles)
                {
                    if (File.Exists(file.FilePath))
                    {
                        File.Delete(file.FilePath);
                    }

                    _context.UploadedFiles.Remove(file);
                }

                await _context.SaveChangesAsync();

                // Clean up empty monthly folders
                await CleanupEmptyFoldersAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup files");
                return false;
            }
        }

        public async Task<bool> CleanupTempFilesAsync()
        {
            // Redirect to new CleanupFilesAsync for backward compatibility
            return await CleanupFilesAsync();
        }

        public async Task<List<string>> GetAvailableMonthlyFoldersAsync()
        {
            try
            {
                var folders = new List<string>();

                if (Directory.Exists(_baseUploadPath))
                {
                    var directories = Directory.GetDirectories(_baseUploadPath)
                        .Select(Path.GetFileName)
                        .Where(name => IsValidMonthFolder(name))
                        .OrderByDescending(name => name)
                        .ToList();

                    folders.AddRange(directories);
                }

                return folders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get monthly folders");
                return new List<string>();
            }
        }

        private string GetRelativePath(string fullPath)
        {
            var relativePath = Path.GetRelativePath(_webHostEnvironment.WebRootPath, fullPath);
            return "/" + relativePath.Replace("\\", "/");
        }

        private string ExtractMonthFolder(string filePath)
        {
            var relativePath = Path.GetRelativePath(_baseUploadPath, filePath);
            var parts = relativePath.Split(Path.DirectorySeparatorChar);

            if (parts.Length > 1 && IsValidMonthFolder(parts[0]))
            {
                return parts[0];
            }

            return string.Empty;
        }

        private bool IsValidMonthFolder(string folderName)
        {
            if (string.IsNullOrEmpty(folderName)) return false;

            return System.Text.RegularExpressions.Regex.IsMatch(folderName, @"^\d{4}-\d{2}$");
        }

        private async Task CleanupEmptyFoldersAsync()
        {
            try
            {
                if (Directory.Exists(_baseUploadPath))
                {
                    var directories = Directory.GetDirectories(_baseUploadPath);

                    foreach (var dir in directories)
                    {
                        if (!Directory.EnumerateFileSystemEntries(dir).Any())
                        {
                            Directory.Delete(dir);
                            _logger.LogInformation("Deleted empty folder: {FolderPath}", dir);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup empty folders");
            }
        }

        private bool IsPdfFile(byte[] buffer)
        {
            return buffer.Length >= 4 && buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46;
        }

        private async Task<int> GetPdfPageCountAsync(string filePath)
        {
            try
            {
                // TODO: Implement actual PDF page counting with PdfSharp or iTextSharp
                return 1;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<string> GetPdfVersionAsync(string filePath)
        {
            try
            {
                // TODO: Implement actual PDF version detection
                return "1.4";
            }
            catch
            {
                return null;
            }
        }

        private async Task<bool> CheckPdfFormFieldsAsync(string filePath)
        {
            try
            {
                // TODO: Implement actual PDF form fields detection
                return false;
            }
            catch
            {
                return false;
            }
        }
    }

    public class DeleteFileResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int AttachmentsDeleted { get; set; }
    }
}