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
        Task<UpdatePrimaryFileResult> UpdatePrimaryFileWithAttachmentsAsync(int templateId, string primaryFileName);
        Task<SearchMappedFieldsResponse> SearchMappedFieldsAsync(SearchMappedFieldsRequest request);

        // ✅ UPDATED: Step-specific save methods for POD architecture
        /// <summary>
        /// Step 1: Save template technical details (PODId, NamingConvention, TechnicalNotes, etc.)
        /// </summary>
        Task<bool> SaveStep1DataAsync(int templateId, Step1DataDto stepData);

        /// <summary>
        /// Step 2: Save PDF file uploads and attachments
        /// </summary>
        Task<bool> SaveStep2DataAsync(int templateId, Step2DataDto stepData);

        // Template lifecycle methods (unchanged)
        Task<bool> FinalizeTemplateAsync(int templateId);

        /// <summary>
        /// Create template as child of POD - requires PODId
        /// </summary>
        Task<PdfTemplate> CreateTemplateForPODAsync(int podId, string namingConvention = "DOC_POD");

        // Validation and activation
        Task<TemplateValidationResult> ValidateAndActivateTemplateAsync(int templateId, FinalizeTemplateRequest request);
        Task<TemplateValidationResult> ValidateTemplateCompletenessAsync(int templateId);

        // ✅ UPDATED: Wizard state management for POD architecture
        /// <summary>
        /// Get wizard state - now works with POD parent-child relationship
        /// </summary>
        Task<TemplateWizardViewModel> GetWizardStateAsync(int step = 1, int? templateId = null);

        // Core template CRUD operations
        Task<PdfTemplate> CreateTemplateAsync(TemplateDefinitionDto definition);
        Task<PdfTemplate?> GetTemplateAsync(int id);
        Task<TemplateListViewModel> GetTemplateListAsync(TemplateFiltersViewModel filters);
        Task<bool> DeleteTemplateAsync(int id);
        Task<TemplateDefinitionDto> ExportTemplateAsync(int id);
        Task<bool> UpdatePrimaryFileAsync(int templateId, string primaryFileName);

        // Helper methods
        Task<List<TemplateAttachment>> GetTemplateAttachmentsAsync(int templateId);
        Task<List<FieldMapping>> GetTemplateFieldMappingsAsync(int templateId);
    }
}