using System.Collections.Generic;
using System.Threading.Tasks;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.Entities;
using DT_PODSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static DT_PODSystem.Services.Implementation.TemplateService;

namespace DT_PODSystem.Services.Interfaces
{
    /// <summary>
    /// Template Service Interface - Updated for POD Architecture
    /// Templates are now technical children of POD entities
    /// Step 1: Template Details (PODId + technical settings)
    /// Step 2: PDF Uploads
    /// Step 3: Field Mapping
    /// </summary>
    public interface ITemplateService
    {
        // Search and filter methods
        Task<List<MappedFieldInfo>> GetMappedFieldsInfoAsync(List<int> fieldIds);
        Task<List<TemplateFilterOption>> GetTemplatesForFilterAsync();
        
        Task<SearchMappedFieldsResponse> SearchMappedFieldsAsync(SearchMappedFieldsRequest request);

        // ✅ UPDATED: Step-specific save methods for POD architecture
        /// <summary>
        /// Step 1: Save template technical details (PODId, NamingConvention, TechnicalNotes, etc.)
        /// </summary>
        Task<bool> SaveStep1DataAsync(int templateId, Step1DataDto stepData);

        Task<TemplateDetailsViewModel> GetTemplateDetailsAsync(int? templateId = null);

        Task<TemplateFieldMappingViewModel> GetTemplateMappingDataAsync(int? templateId = null);

        // Template lifecycle methods (unchanged)
        Task<bool> FinalizeTemplateAsync(int templateId);

        /// <summary>
        /// Create template as child of POD - requires PODId
        /// </summary>
        Task<PdfTemplate> CreateTemplateForPODAsync(int podId, string namingConvention = "DOC_POD");

        // Validation and activation
        Task<TemplateValidationResult> ValidateAndActivateTemplateAsync(int templateId, FinalizeTemplateRequest request);
        Task<TemplateValidationResult> ValidateTemplateCompletenessAsync(int templateId);

       
        Task<PdfTemplate> CreateTemplateAsync(TemplateDefinitionDto definition);
        Task<PdfTemplate?> GetTemplateAsync(int id);
        Task<TemplateListViewModel> GetTemplateListAsync(TemplateFiltersViewModel filters);
        Task<bool> DeleteTemplateAsync(int id);
        Task<TemplateDefinitionDto> ExportTemplateAsync(int id);
        

        // Helper methods
        
        Task<List<FieldMapping>> GetTemplateFieldMappingsAsync(int templateId);
    }
}