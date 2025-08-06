using System.Collections.Generic;
using System.Threading.Tasks;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Services.Implementation;
using Microsoft.AspNetCore.Http;



namespace DT_PODSystem.Services.Interfaces
{
    public interface IFileUploadService
    {

        Task<DeleteFileResult> DeleteFileWithAttachmentsAsync(string fileName);

        // Core upload functionality
        Task<FileUploadDto> UploadFileAsync(IFormFile file, FileUploadRequest? request = null);

        // File management
        Task<bool> DeleteFileAsync(string fileName);
        Task<bool> ValidateFileAsync(IFormFile file);
        Task<List<FileUploadDto>> GetUploadedFilesAsync(string monthFolder = null);

        // Cleanup operations
        Task<bool> CleanupFilesAsync(int olderThanDays = 30);

        // Monthly folder management
        Task<List<string>> GetAvailableMonthlyFoldersAsync();

        // Backward compatibility methods
        Task<bool> DeleteTempFileAsync(string fileName);
        Task<List<FileUploadDto>> GetTempFilesAsync();
        Task<bool> CleanupTempFilesAsync();


    }
}