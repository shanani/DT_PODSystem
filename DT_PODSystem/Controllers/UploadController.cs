using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Helpers;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Controllers
{
    [Authorize]
    public class UploadController : Controller
    {
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<UploadController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UploadController(IFileUploadService fileUploadService, ILogger<UploadController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _fileUploadService = fileUploadService;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }


        // Replace the DeleteFile method in UploadController.cs

        [HttpPost]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    return Json(new { success = false, message = "File name is required" });
                }

                var result = await _fileUploadService.DeleteFileWithAttachmentsAsync(fileName);
                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    data = new
                    {
                        fileName = fileName,
                        attachmentsDeleted = result.AttachmentsDeleted
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file {FileName}", fileName);
                return Json(new { success = false, message = "Deletion failed: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "No file selected" });
                }

                // Use new storage system but return data in OLD format for JS compatibility
                var request = new FileUploadRequest
                {
                    IsTemporary = false, // Use permanent storage
                    UseMonthlyFolders = true,
                    Metadata = new Dictionary<string, object>
                    {
                        ["UploadedBy"] = User.Identity?.Name ?? "Unknown",
                        ["UploadedAt"] = DateTime.UtcNow,
                        ["UserAgent"] = Request.Headers["User-Agent"].ToString()
                    }
                };

                var result = await _fileUploadService.UploadFileAsync(file, request);

                if (result.Success)
                {
                    // BACKWARD COMPATIBILITY: Return data in OLD format
                    return Json(new
                    {
                        success = true,
                        message = "File uploaded successfully",
                        data = new
                        {
                            originalFileName = result.OriginalFileName,
                            savedFileName = result.SavedFileName,
                            filePath = result.FilePath, // Keep full path for backward compatibility
                            contentType = result.ContentType,
                            fileSize = result.FileSize,
                            pageCount = result.PageCount,
                            pdfVersion = result.PdfVersion,
                            hasFormFields = result.HasFormFields,
                            uploadedAt = result.UploadedAt.ToString("yyyy-MM-dd HH:mm:ss")
                        }
                    });
                }
                else
                {
                    return Json(new { success = false, message = result.Message, errors = result.ValidationErrors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File upload failed");
                return Json(new { success = false, message = "Upload failed: " + ex.Message });
            }
        }

        // BACKWARD COMPATIBILITY: Keep old method name, redirect to new implementation
        [HttpPost]
        public async Task<IActionResult> DeleteTempFile(string fileName)
        {
            return await DeleteFile(fileName);
        }



        // BACKWARD COMPATIBILITY: Keep old method name
        [HttpGet]
        public async Task<IActionResult> GetTempFiles()
        {
            try
            {
                var files = await _fileUploadService.GetUploadedFilesAsync();

                // BACKWARD COMPATIBILITY: Return in OLD format
                return Json(new
                {
                    success = true,
                    data = files.Select(f => new
                    {
                        originalFileName = f.OriginalFileName,
                        savedFileName = f.SavedFileName,
                        filePath = f.FilePath, // Keep full path for compatibility
                        contentType = f.ContentType,
                        fileSize = f.FileSize,
                        uploadedAt = f.UploadedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                        pageCount = f.PageCount,
                        pdfVersion = f.PdfVersion,
                        hasFormFields = f.HasFormFields
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get uploaded files");
                return Json(new { success = false, message = "Failed to retrieve files: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ValidateFile(IFormFile file)
        {
            try
            {
                if (file == null)
                {
                    return Json(new { isValid = false, message = "No file provided" });
                }

                var isValid = await _fileUploadService.ValidateFileAsync(file);
                return Json(new
                {
                    isValid = isValid,
                    message = isValid ? "File is valid" : "File validation failed",
                    fileInfo = new
                    {
                        name = file.FileName,
                        size = file.Length,
                        contentType = file.ContentType,
                        extension = Path.GetExtension(file.FileName)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File validation failed");
                return Json(new { isValid = false, message = "Validation error: " + ex.Message });
            }
        }

        // BACKWARD COMPATIBILITY: Keep old method name
        [HttpPost]
        public async Task<IActionResult> CleanupTempFiles()
        {
            try
            {
                if (!User.IsAdmin())
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                var result = await _fileUploadService.CleanupFilesAsync();
                return Json(new { success = result, message = result ? "Cleanup completed successfully" : "Cleanup failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Files cleanup failed");
                return Json(new { success = false, message = "Cleanup failed: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetUploadConfiguration()
        {
            return Json(new
            {
                maxFileSize = 10 * 1024 * 1024,
                allowedExtensions = new[] { ".pdf" },
                maxFiles = 5,
                acceptedMimeTypes = new[] { "application/pdf" },
                uploadUrl = "/Upload/Upload",
                deleteUrl = "/Upload/DeleteTempFile", // Keep old URL for JS compatibility
                validateUrl = "/Upload/ValidateFile"
            });
        }

        // BACKWARD COMPATIBILITY: Keep old Download method that serves files via controller
        [HttpGet]
        public async Task<IActionResult> Download(string fileName)
        {
            return await DownloadFile(fileName);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    return NotFound("File name is required");
                }

                var files = await _fileUploadService.GetUploadedFilesAsync();
                var file = files.FirstOrDefault(f => f.SavedFileName == fileName);

                if (file == null)
                {
                    return NotFound("File not found");
                }

                // Build full path from relative path
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, file.RelativePath.TrimStart('/'));

                if (!System.IO.File.Exists(fullPath))
                {
                    return NotFound("Physical file not found");
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                return File(fileBytes, file.ContentType, file.OriginalFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File download failed for {FileName}", fileName);
                return StatusCode(500, "Download failed");
            }
        }

        // BACKWARD COMPATIBILITY: Keep old Preview method that serves files via controller
        [HttpGet]
        public async Task<IActionResult> Preview(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    return NotFound("File name is required");
                }

                var files = await _fileUploadService.GetUploadedFilesAsync();
                var file = files.FirstOrDefault(f => f.SavedFileName == fileName);

                if (file == null)
                {
                    return NotFound("File not found");
                }

                // Build full path from relative path
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, file.RelativePath.TrimStart('/'));

                if (!System.IO.File.Exists(fullPath))
                {
                    return NotFound("Physical file not found");
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);

                // For PDF preview, return with inline disposition
                Response.Headers.Add("Content-Disposition", "inline");
                return File(fileBytes, file.ContentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File preview failed for {FileName}", fileName);
                return StatusCode(500, "Preview failed");
            }
        }

        [HttpPost]
        public async Task<IActionResult> BatchUpload(List<IFormFile> files)
        {
            try
            {
                if (files == null || !files.Any())
                {
                    return Json(new { success = false, message = "No files provided" });
                }

                var results = new List<object>();
                var successCount = 0;
                var failureCount = 0;

                foreach (var file in files)
                {
                    try
                    {
                        var request = new FileUploadRequest
                        {
                            IsTemporary = false,
                            UseMonthlyFolders = true,
                            Metadata = new Dictionary<string, object>
                            {
                                ["BatchUpload"] = true,
                                ["UploadedBy"] = User.Identity?.Name ?? "Unknown",
                                ["UploadedAt"] = DateTime.UtcNow
                            }
                        };

                        var result = await _fileUploadService.UploadFileAsync(file, request);

                        if (result.Success)
                        {
                            successCount++;
                            results.Add(new
                            {
                                fileName = file.FileName,
                                success = true,
                                data = new
                                {
                                    originalFileName = result.OriginalFileName,
                                    savedFileName = result.SavedFileName,
                                    fileSize = result.FileSize,
                                    pageCount = result.PageCount
                                }
                            });
                        }
                        else
                        {
                            failureCount++;
                            results.Add(new { fileName = file.FileName, success = false, message = result.Message, errors = result.ValidationErrors });
                        }
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        results.Add(new { fileName = file.FileName, success = false, message = ex.Message });
                    }
                }

                return Json(new
                {
                    success = successCount > 0,
                    message = $"Batch upload completed: {successCount} successful, {failureCount} failed",
                    summary = new
                    {
                        totalFiles = files.Count,
                        successfulUploads = successCount,
                        failedUploads = failureCount
                    },
                    results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch upload failed");
                return Json(new { success = false, message = "Batch upload failed: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetFileIcon(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var icon = extension switch
            {
                ".pdf" => "fa fa-file-pdf text-danger",
                ".doc" => "fa fa-file-word text-primary",
                ".docx" => "fa fa-file-word text-primary",
                ".xls" => "fa fa-file-excel text-success",
                ".xlsx" => "fa fa-file-excel text-success",
                ".jpg" => "fa fa-file-image text-warning",
                ".jpeg" => "fa fa-file-image text-warning",
                ".png" => "fa fa-file-image text-warning",
                ".gif" => "fa fa-file-image text-warning",
                _ => "fa fa-file text-secondary"
            };
            return Json(new { icon = icon, extension = extension.TrimStart('.') });
        }

        [HttpGet]
        public async Task<IActionResult> GetFileInfo(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    return Json(new { success = false, message = "File name is required" });
                }

                var files = await _fileUploadService.GetUploadedFilesAsync();
                var file = files.FirstOrDefault(f => f.SavedFileName == fileName);

                if (file == null)
                {
                    return Json(new { success = false, message = "File not found" });
                }

                var previewData = new
                {
                    originalFileName = file.OriginalFileName,
                    fileName = file.OriginalFileName,
                    fileSize = file.FileSize,
                    contentType = file.ContentType,
                    uploadedAt = file.UploadedAt,
                    pageCount = file.PageCount,
                    pdfVersion = file.PdfVersion,
                    hasFormFields = file.HasFormFields,
                    canPreview = file.ContentType == "application/pdf"
                };

                return Json(new { success = true, data = previewData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File info failed for {FileName}", fileName);
                return Json(new { success = false, message = "File info failed: " + ex.Message });
            }
        }

        // BACKWARD COMPATIBILITY: Keep old method name
        [HttpPost]
        public async Task<IActionResult> GetFilePreview(string fileName)
        {
            return await GetFileInfo(fileName);
        }

        [HttpGet]
        public IActionResult GetDropzoneConfig()
        {
            return Json(new
            {
                url = "/Upload/Upload",
                maxFilesize = 10,
                acceptedFiles = ".pdf",
                maxFiles = 5,
                addRemoveLinks = true,
                dictDefaultMessage = "Drop PDF files <b>here</b> or <b>click</b> to upload.<br><span class=\"dz-note\">(PDF files only - 10MB max)</span>",
                dictRemoveFile = "Remove",
                dictCancelUpload = "Cancel",
                dictUploadCanceled = "Upload cancelled",
                dictCancelUploadConfirmation = "Are you sure you want to cancel this upload?",
                dictRemoveFileConfirmation = "Are you sure you want to remove this file?",
                dictMaxFilesExceeded = "You can only upload 5 files at a time",
                dictFileTooBig = "File is too big. Maximum size is 10MB",
                dictInvalidFileType = "Only PDF files are allowed",
                dictResponseError = "Server responded with error",
                parallelUploads = 2,
                uploadMultiple = false,
                autoProcessQueue = true,
                createImageThumbnails = false
            });
        }
    }
}