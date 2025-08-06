using System.Collections.Generic;
using System.Threading.Tasks;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.Entities;
using DT_PODSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static DT_PODSystem.Services.Implementation.TemplateService;

namespace DT_PODSystem.Services.Interfaces
{
    public interface ITemplateService
    {
        Task<List<MappedFieldInfo>> GetMappedFieldsInfoAsync(List<int> fieldIds);
        Task<List<TemplateFilterOption>> GetTemplatesForFilterAsync();
        Task<UpdatePrimaryFileResult> UpdatePrimaryFileWithAttachmentsAsync(int templateId, string primaryFileName);
        /// <summary>
        /// Search mapped fields across all active templates
        /// </summary>
        /// <param name="request">Search request parameters</param>
        /// <returns>Search results with pagination</returns>
        Task<SearchMappedFieldsResponse> SearchMappedFieldsAsync(SearchMappedFieldsRequest request);

        // Step-specific save methods
        Task<bool> SaveStep1DataAsync(int templateId, Step1DataDto stepData);
        Task<bool> SaveStep2DataAsync(int templateId, Step2DataDto stepData);

        // Template lifecycle methods
        Task<bool> FinalizeTemplateAsync(int templateId);
        Task<PdfTemplate> CreateDraftTemplateAsync();

        // Validation and activation
        Task<TemplateValidationResult> ValidateAndActivateTemplateAsync(int templateId, FinalizeTemplateRequest request);
        Task<TemplateValidationResult> ValidateTemplateCompletenessAsync(int templateId);

        // Wizard state management
        Task<TemplateWizardViewModel> GetWizardStateAsync(int step = 1, int? templateId = null);

        // Core template CRUD operations
        Task<PdfTemplate> CreateTemplateAsync(TemplateDefinitionDto definition);
        Task<PdfTemplate?> GetTemplateAsync(int id);
        Task<TemplateListViewModel> GetTemplateListAsync(TemplateFiltersViewModel filters);
        Task<bool> DeleteTemplateAsync(int id);
        Task<TemplateDefinitionDto> ExportTemplateAsync(int id);
        Task<bool> UpdatePrimaryFileAsync(int templateId, string primaryFileName);
    }
}