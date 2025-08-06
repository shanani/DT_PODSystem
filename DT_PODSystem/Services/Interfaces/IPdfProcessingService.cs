using System.Collections.Generic;
using System.Threading.Tasks;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.ViewModels;

namespace DT_PODSystem.Services.Interfaces
{
    public interface IPdfProcessingService
    {

        Task<DeleteResult> RemoveFieldMappingWithUsageCheckAsync(int templateId, int fieldMappingId);
        Task<string> RenderPdfPageAsync(string filePath, int pageNumber, decimal zoomLevel = 1.0m);

        Task<bool> DeleteFieldMappingAsync(int id);
        Task<string> PreviewFieldExtractionAsync(int fieldMappingId, string filePath);
        Task<List<AutoDetectedFieldViewModel>> AutoDetectFieldsAsync(string filePath);


        // ✅ NEW AJAX METHODS for Field Mapping CRUD
        Task<FieldMappingDto> AddFieldMappingAsync(int templateId, FieldMappingDto fieldMapping);
        Task<FieldMappingDto> UpdateFieldMappingAsync(int fieldMappingId, FieldMappingDto fieldMapping);
        Task<bool> RemoveFieldMappingAsync(int templateId, int fieldMappingId);
        Task<List<FieldMappingDto>> GetFieldMappingsAsync(int templateId);
        Task<FieldMappingDto?> GetFieldMappingAsync(int fieldMappingId);
        Task<TemplateAnchorDto> AddTemplateAnchorAsync(int templateId, TemplateAnchorDto anchorPointDto);
        Task<TemplateAnchorDto> UpdateTemplateAnchorAsync(int anchorPointId, TemplateAnchorDto anchorPointDto);
        Task<bool> RemoveTemplateAnchorAsync(int templateId, int anchorPointId);
        Task<TemplateAnchorDto> GetTemplateAnchorAsync(int anchorPointId);
        Task<List<TemplateAnchorDto>> GetTemplateAnchorsAsync(int templateId);
    }
}