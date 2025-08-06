using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using DT_PODSystem.Data;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.Entities;
using DT_PODSystem.Models.Enums;
using DT_PODSystem.Models.ViewModels;
using DT_PODSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DT_PODSystem.Services.Implementation
{
    public class TemplateService : ITemplateService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TemplateService> _logger;
        private readonly IPdfProcessingService _pdfProcessingService;

        public TemplateService(ApplicationDbContext context, IPdfProcessingService pdfProcessingService, ILogger<TemplateService> logger)
        {
            _context = context;
            _logger = logger;
            _pdfProcessingService = pdfProcessingService;
        }


        // ✅ Add this method to TemplateService.cs implementation
        public async Task<List<MappedFieldInfo>> GetMappedFieldsInfoAsync(List<int> fieldIds)
        {
            try
            {
                if (fieldIds == null || !fieldIds.Any())
                {
                    return new List<MappedFieldInfo>();
                }

                _logger.LogInformation("Getting mapped fields info for {Count} field IDs", fieldIds.Count);

                var fieldsInfo = await _context.FieldMappings
                    .Include(fm => fm.Template)
                    .Where(fm => fieldIds.Contains(fm.Id) && fm.Template.Status == TemplateStatus.Active)
                    .Select(fm => new MappedFieldInfo
                    {
                        FieldId = fm.Id,
                        FieldName = fm.FieldName,
                        DisplayName = fm.DisplayName ?? fm.FieldName,
                        TemplateName = fm.Template.Name,
                        TemplateId = fm.TemplateId,
                        Description = fm.Description ?? "",
                        DataType = fm.DataType.ToString()
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} active mapped fields out of {RequestedCount} requested",
                    fieldsInfo.Count, fieldIds.Count);

                return fieldsInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting mapped fields info for field IDs: {FieldIds}",
                    string.Join(",", fieldIds));
                throw;
            }
        }

        /// <summary>
        /// Get active templates for filter dropdowns
        /// </summary>
        public async Task<List<TemplateFilterOption>> GetTemplatesForFilterAsync()
        {
            try
            {
                _logger.LogInformation("Getting active templates for filter dropdown");

                var templates = await _context.PdfTemplates
                    .Include(t => t.Category)
                    .Include(t => t.Vendor)
                    .Include(t => t.Department)
                        .ThenInclude(d => d.GeneralDirectorate)
                    .Where(t => t.Status == TemplateStatus.Active)
                    .OrderBy(t => t.Name)
                    .Select(t => new TemplateFilterOption
                    {
                        Id = t.Id,
                        Name = t.Name,
                        CategoryName = t.Category != null ? t.Category.Name : "",
                        VendorName = t.Vendor != null ? t.Vendor.Name : "",
                        DepartmentName = t.Department != null ? t.Department.Name : "",
                        GeneralDirectorateName = t.Department != null && t.Department.GeneralDirectorate != null ?
                            t.Department.GeneralDirectorate.Name : "",
                        FieldCount = t.FieldMappings.Count(fm => fm.IsActive)
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} active templates for filter", templates.Count);

                return templates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting templates for filter");
                throw new InvalidOperationException("Failed to get templates for filter", ex);
            }
        }


        // Add this to TemplateService.cs

        public class UpdatePrimaryFileResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public int AttachmentsCreated { get; set; }
            public int AttachmentsUpdated { get; set; }
        }

        /// <summary>
        /// Update primary file selection and ensure all uploaded files are linked as TemplateAttachments
        /// </summary>
        public async Task<UpdatePrimaryFileResult> UpdatePrimaryFileWithAttachmentsAsync(int templateId, string primaryFileName)
        {
            var result = new UpdatePrimaryFileResult();

            try
            {
                var template = await _context.PdfTemplates
                    .Include(t => t.Attachments)
                        .ThenInclude(a => a.UploadedFile)
                    .FirstOrDefaultAsync(t => t.Id == templateId);

                if (template == null)
                {
                    result.Message = $"Template {templateId} not found";
                    return result;
                }

                // Get all uploaded files that are not yet linked to this template
                var existingAttachmentFileNames = template.Attachments
                    .Select(a => a.SavedFileName)
                    .ToHashSet();

                // Find uploaded files that should be linked but aren't yet
                var unlinkedFiles = await _context.UploadedFiles
                    .Where(f => f.IsActive && !existingAttachmentFileNames.Contains(f.SavedFileName))
                    .ToListAsync();

                var attachmentsToCreate = new List<TemplateAttachment>();
                var attachmentsCreated = 0;

                // Create TemplateAttachments for any unlinked files
                foreach (var uploadedFile in unlinkedFiles)
                {
                    var attachment = new TemplateAttachment
                    {
                        TemplateId = templateId,
                        UploadedFileId = uploadedFile.Id,
                        OriginalFileName = uploadedFile.OriginalFileName,
                        SavedFileName = uploadedFile.SavedFileName,
                        FilePath = uploadedFile.FilePath,
                        Type = uploadedFile.SavedFileName == primaryFileName ? AttachmentType.Original : AttachmentType.Reference,
                        IsPrimary = uploadedFile.SavedFileName == primaryFileName,
                        DisplayOrder = 0,
                        HasFormFields = false,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = "System"
                    };

                    attachmentsToCreate.Add(attachment);
                    attachmentsCreated++;
                }

                // Add new attachments to context
                if (attachmentsToCreate.Any())
                {
                    _context.TemplateAttachments.AddRange(attachmentsToCreate);
                }

                // Update existing attachments - reset all to not primary
                var attachmentsUpdated = 0;
                foreach (var attachment in template.Attachments)
                {
                    var wasPrimary = attachment.IsPrimary;
                    var wasOriginal = attachment.Type == AttachmentType.Original;

                    attachment.IsPrimary = attachment.SavedFileName == primaryFileName;
                    attachment.Type = attachment.SavedFileName == primaryFileName ? AttachmentType.Original : AttachmentType.Reference;

                    if (wasPrimary != attachment.IsPrimary || wasOriginal != (attachment.Type == AttachmentType.Original))
                    {
                        attachmentsUpdated++;
                    }
                }

                // Verify the primary file exists (either in existing attachments or new ones)
                var primaryFileExists = template.Attachments.Any(a => a.SavedFileName == primaryFileName) ||
                                       attachmentsToCreate.Any(a => a.SavedFileName == primaryFileName);

                if (!primaryFileExists)
                {
                    result.Message = $"Primary file '{primaryFileName}' not found in uploaded files";
                    return result;
                }

                // Update template timestamp
                template.ModifiedDate = DateTime.UtcNow;
                template.ModifiedBy = "System";

                // Save all changes
                await _context.SaveChangesAsync();

                result.Success = true;
                result.Message = $"Primary file updated successfully. Created {attachmentsCreated} new attachments, updated {attachmentsUpdated} existing attachments.";
                result.AttachmentsCreated = attachmentsCreated;
                result.AttachmentsUpdated = attachmentsUpdated;

                _logger.LogInformation("Primary file updated to {PrimaryFileName} for template {TemplateId}. Created {Created} attachments, updated {Updated} attachments.",
                    primaryFileName, templateId, attachmentsCreated, attachmentsUpdated);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating primary file with attachments for template {TemplateId}", templateId);
                result.Message = $"Error updating primary file: {ex.Message}";
                return result;
            }
        }



        // Add this method to your TemplateService.cs

        /// <summary>
        /// Update primary file selection for a template
        /// </summary>
        public async Task<bool> UpdatePrimaryFileAsync(int templateId, string primaryFileName)
        {
            try
            {
                var template = await _context.PdfTemplates
                    .Include(t => t.Attachments)
                    .FirstOrDefaultAsync(t => t.Id == templateId);

                if (template == null)
                {
                    _logger.LogWarning("Template {TemplateId} not found for primary file update", templateId);
                    return false;
                }

                // Find the attachment with the specified filename
                var targetAttachment = template.Attachments
                    .FirstOrDefault(a => a.SavedFileName == primaryFileName);

                if (targetAttachment == null)
                {
                    _logger.LogWarning("File {PrimaryFileName} not found in template {TemplateId} attachments",
                        primaryFileName, templateId);
                    return false;
                }

                // Reset all attachments to not primary
                foreach (var attachment in template.Attachments)
                {
                    attachment.IsPrimary = false;
                    attachment.Type = AttachmentType.Reference;
                }

                // Set the target attachment as primary
                targetAttachment.IsPrimary = true;
                targetAttachment.Type = AttachmentType.Original;

                // Update template timestamp
                template.ModifiedDate = DateTime.UtcNow;
                template.ModifiedBy = "System";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Primary file updated to {PrimaryFileName} for template {TemplateId}",
                    primaryFileName, templateId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating primary file for template {TemplateId}", templateId);
                return false;
            }
        }


        // Update this method in Services/Implementation/TemplateService.cs
        public async Task<SearchMappedFieldsResponse> SearchMappedFieldsAsync(SearchMappedFieldsRequest request)
        {
            try
            {
                _logger.LogInformation("Searching mapped fields with criteria: {SearchTerm}, TemplateIds: {TemplateIds}",
                    request.SearchTerm, request.TemplateIds != null ? string.Join(",", request.TemplateIds) : "All");

                // Validate parameters
                if (request.PageSize <= 0 || request.PageSize > 100)
                {
                    throw new ArgumentException("Page size must be between 1 and 100");
                }

                if (request.Page < 0)
                {
                    throw new ArgumentException("Page number cannot be negative");
                }

                // Build the query - search across all field mappings from active templates
                var query = _context.FieldMappings
                    .Include(fm => fm.Template)
                        .ThenInclude(t => t.Category)
                    .Include(fm => fm.Template)
                        .ThenInclude(t => t.Department)
                            .ThenInclude(d => d.GeneralDirectorate)
                    .Include(fm => fm.Template)
                        .ThenInclude(t => t.Vendor)
                    .Where(fm => fm.Template.Status == TemplateStatus.Active)
                    .AsQueryable();

                // ✅ NEW: Apply multi-template filter if provided
                if (request.TemplateIds != null && request.TemplateIds.Any())
                {
                    query = query.Where(fm => request.TemplateIds.Contains(fm.TemplateId));
                }

                // Apply search filter if provided (minimum 2 characters)
                if (!string.IsNullOrWhiteSpace(request.SearchTerm) && request.SearchTerm.Length >= 2)
                {
                    var searchTermLower = request.SearchTerm.ToLower();
                    query = query.Where(fm =>
                        fm.FieldName.ToLower().Contains(searchTermLower) ||
                        (fm.DisplayName != null && fm.DisplayName.ToLower().Contains(searchTermLower)) ||
                        fm.Template.Name.ToLower().Contains(searchTermLower) ||
                        (fm.Template.Category != null && fm.Template.Category.Name.ToLower().Contains(searchTermLower)) ||
                        (fm.Template.Vendor != null && fm.Template.Vendor.Name.ToLower().Contains(searchTermLower)) ||
                        (fm.Template.Department != null && fm.Template.Department.Name.ToLower().Contains(searchTermLower)) ||
                        (fm.Template.Department != null &&
                         fm.Template.Department.GeneralDirectorate != null &&
                         fm.Template.Department.GeneralDirectorate.Name.ToLower().Contains(searchTermLower))
                    );
                }

                // ✅ NEW: Apply additional filters if provided
                if (!string.IsNullOrWhiteSpace(request.CategoryName))
                {
                    query = query.Where(fm => fm.Template.Category != null &&
                        fm.Template.Category.Name.ToLower().Contains(request.CategoryName.ToLower()));
                }

                if (!string.IsNullOrWhiteSpace(request.VendorName))
                {
                    query = query.Where(fm => fm.Template.Vendor != null &&
                        fm.Template.Vendor.Name.ToLower().Contains(request.VendorName.ToLower()));
                }

                if (!string.IsNullOrWhiteSpace(request.DepartmentName))
                {
                    query = query.Where(fm => fm.Template.Department != null &&
                        fm.Template.Department.Name.ToLower().Contains(request.DepartmentName.ToLower()));
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply ordering and pagination
                var results = await query
                    .OrderBy(fm => fm.Template.Name)
                    .ThenBy(fm => fm.FieldName)
                    .Skip(request.Page * request.PageSize)
                    .Take(request.PageSize)
                    .Select(fm => new MappedFieldSearchResult
                    {
                        FieldId = fm.Id,
                        FieldName = fm.FieldName,
                        DisplayName = fm.DisplayName ?? fm.FieldName,
                        DataType = fm.DataType.ToString(),
                        Description = fm.Description ?? "",
                        TemplateId = fm.TemplateId,
                        TemplateName = fm.Template.Name,
                        CategoryName = fm.Template.Category != null ? fm.Template.Category.Name : "",
                        VendorName = fm.Template.Vendor != null ? fm.Template.Vendor.Name : "",
                        DepartmentName = fm.Template.Department != null ? fm.Template.Department.Name : "",
                        GeneralDirectorateName = fm.Template.Department != null &&
                                              fm.Template.Department.GeneralDirectorate != null ?
                                              fm.Template.Department.GeneralDirectorate.Name : ""
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {ResultCount} of {TotalCount} mapped fields for search term: {SearchTerm}, TemplateIds: {TemplateIds}",
                    results.Count, totalCount, request.SearchTerm, request.TemplateIds != null ? string.Join(",", request.TemplateIds) : "All");

                return new SearchMappedFieldsResponse
                {
                    Results = results,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    HasMore = (request.Page + 1) * request.PageSize < totalCount
                };
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching mapped fields");
                throw new InvalidOperationException("Failed to search mapped fields", ex);
            }
        }

        public async Task<List<TemplateAttachment>> GetTemplateAttachmentsAsync(int templateId)
        {
            return await _context.TemplateAttachments
                .Include(a => a.UploadedFile)
                .Where(a => a.TemplateId == templateId)
                .OrderBy(a => a.Type)
                .ThenBy(a => a.DisplayOrder)
                .ToListAsync();
        }

        // Helper: Get template field mappings
        public async Task<List<FieldMapping>> GetTemplateFieldMappingsAsync(int templateId)
        {
            return await _context.FieldMappings
                .Where(f => f.TemplateId == templateId)
                .OrderBy(f => f.PageNumber)
                .ThenBy(f => f.Y)
                .ThenBy(f => f.X)
                .ToListAsync();
        }


        // Create draft template
        public async Task<PdfTemplate> CreateDraftTemplateAsync()
        {
            var template = new PdfTemplate
            {
                Name = $"Draft Template {DateTime.Now:yyyy-MM-dd HH:mm}",
                Description = "Template in development",
                NamingConvention = "DOC_POD_yyyyMM",
                Status = TemplateStatus.Draft,
                Version = "1.0",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "System",
                IsActive = true,
                CategoryId = 1,
                DepartmentId = 1,
                VendorId = null
            };

            _context.PdfTemplates.Add(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created draft template with ID {TemplateId}", template.Id);
            return template;
        }

        // Get template
        public async Task<PdfTemplate?> GetTemplateAsync(int id)
        {
            return await _context.PdfTemplates
                .Include(t => t.Category)
                .Include(t => t.Department).ThenInclude(d => d.GeneralDirectorate)
                .Include(t => t.Vendor)
                .Include(t => t.Attachments).ThenInclude(a => a.UploadedFile)
                .Include(t => t.FieldMappings)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        // ✅ UPDATED GetWizardStateAsync method - removed Step 4 references
        public async Task<TemplateWizardViewModel> GetWizardStateAsync(int step = 1, int? templateId = null)
        {
            var model = new TemplateWizardViewModel
            {
                CurrentStep = step,
                TemplateId = templateId ?? 0
            };

            // Load lookup data for Step 2
            var categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            var departments = await _context.Departments.Include(d => d.GeneralDirectorate).Where(d => d.IsActive).ToListAsync();
            var vendors = await _context.Vendors.Where(v => v.IsActive).ToListAsync();

            // Initialize Step2 ViewModel with lookup data
            model.Step2.Categories = categories.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList();
            model.Step2.Departments = departments.Select(d => new SelectListItem { Value = d.Id.ToString(), Text = $"{d.GeneralDirectorate.Name} - {d.Name}" }).ToList();
            model.Step2.Vendors = vendors.Select(v => new SelectListItem { Value = v.Id.ToString(), Text = v.Name }).ToList();

            if (templateId.HasValue && templateId.Value > 0)
            {
                var template = await _context.PdfTemplates
                    .Include(t => t.Attachments).ThenInclude(a => a.UploadedFile)
                    .Include(t => t.FieldMappings)
                    .FirstOrDefaultAsync(t => t.Id == templateId.Value);

                if (template != null)
                {

                    // Find the primary attachment first
                    var primaryAttachment = template.Attachments.FirstOrDefault(a => a.IsPrimary);
                    var primaryFileName = primaryAttachment?.SavedFileName;

                    // Map to Step1 ViewModel
                    model.Step1.UploadedFiles = template.Attachments.Select(a => new FileUploadDto
                    {
                        OriginalFileName = a.UploadedFile.OriginalFileName,
                        SavedFileName = a.UploadedFile.SavedFileName,
                        FilePath = a.UploadedFile.FilePath,
                        FileSize = a.UploadedFile.FileSize,
                        ContentType = a.UploadedFile.ContentType,
                        IsPrimary = a.IsPrimary,
                        Success = true,
                        UploadedAt = a.UploadedFile.CreatedDate,
                        PageCount = a.PageCount ?? 0,
                        PdfVersion = a.PdfVersion,
                        HasFormFields = a.HasFormFields
                    }).OrderByDescending(f => f.IsPrimary) // Primary file first
                      .ThenBy(f => f.UploadedAt) // Then by upload date
                      .ToList();

                    // Set primary file information
                    model.Step1.PrimaryFileId = primaryAttachment?.Id ?? 0;
                    model.Step1.PrimaryFileName = primaryFileName; // Add this property

                    // Map to Step2 ViewModel
                    model.Step2.Name = template.Name;
                    model.Step2.NamingConvention = template.NamingConvention;
                    model.Step2.Description = template.Description;
                    model.Step2.CategoryId = template.CategoryId;
                    model.Step2.DepartmentId = template.DepartmentId;
                    model.Step2.VendorId = template.VendorId;
                    model.Step2.Status = template.Status;
                    model.Step2.RequiresApproval = template.RequiresApproval;
                    model.Step2.IsFinancialData = template.IsFinancialData;
                    model.Step2.ProcessingPriority = template.ProcessingPriority;

                    // Map to Step3 ViewModel
                    model.Step3.FieldMappings = template.FieldMappings.Select(fm => new FieldMappingDto
                    {
                        Id = fm.Id,
                        FieldName = fm.FieldName,
                        DisplayName = fm.DisplayName,
                        Description = fm.Description ?? string.Empty,
                        X = (double)fm.X,
                        Y = (double)fm.Y,
                        Width = (double)fm.Width,
                        Height = (double)fm.Height,
                        PageNumber = fm.PageNumber,
                        IsRequired = fm.IsRequired
                    }).ToList();

                    // ✅ ADD: Load anchor points for Step 3
                    var TemplateAnchors = await _pdfProcessingService.GetTemplateAnchorsAsync(templateId.Value);
                    model.Step3.TemplateAnchors = TemplateAnchors;
                }
            }

            return model;
        }

        // Legacy method for compatibility
        public async Task<bool> SaveWizardProgressAsync(int templateId, object progressData)
        {
            // This can be implemented if needed for backward compatibility
            return true;
        }



        // Save Step 2 data - Template details and metadata (FIXED)
        public async Task<bool> SaveStep2DataAsync(int templateId, Step2DataDto stepData)
        {
            try
            {
                var template = await _context.PdfTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId);

                if (template == null)
                    return false;

                // Update template properties
                template.Name = stepData.Name;
                template.NamingConvention = stepData.NamingConvention; // ✅ FIXED: This now contains only prefix
                template.CategoryId = stepData.CategoryId;
                template.DepartmentId = stepData.DepartmentId;
                template.VendorId = stepData.VendorId;
                template.Description = stepData.Description;
                template.IsActive = stepData.IsActive;

                // ✅ FIX: Add the missing properties
                template.RequiresApproval = stepData.RequiresApproval;
                template.IsFinancialData = stepData.IsFinancialData;
                template.ProcessingPriority = stepData.ProcessingPriority;

                template.ModifiedDate = DateTime.UtcNow;
                template.ModifiedBy = "System";

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Step 2 data for template {TemplateId}", templateId);
                return false;
            }
        }

        // Save Step 1 data - File uploads and attachments
        public async Task<bool> SaveStep1DataAsync(int templateId, Step1DataDto stepData)
        {
            try
            {
                var template = await _context.PdfTemplates
                    .Include(t => t.Attachments)
                    .FirstOrDefaultAsync(t => t.Id == templateId);

                if (template == null)
                    return false;

                // Clear existing attachments
                _context.TemplateAttachments.RemoveRange(template.Attachments);

                // Add new attachments
                foreach (var file in stepData.UploadedFiles)
                {
                    // First create/get UploadedFile record
                    var uploadedFile = new UploadedFile
                    {
                        OriginalFileName = file.OriginalFileName,
                        SavedFileName = file.SavedFileName,
                        FilePath = file.FilePath,
                        FileSize = file.FileSize,
                        ContentType = file.ContentType,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = "System"
                    };
                    _context.UploadedFiles.Add(uploadedFile);
                    await _context.SaveChangesAsync(); // Save to get ID

                    // Then create TemplateAttachment
                    var attachment = new TemplateAttachment
                    {
                        TemplateId = templateId,
                        UploadedFileId = uploadedFile.Id,
                        Type = file.SavedFileName == stepData.PrimaryFileName ?
                               AttachmentType.Original : AttachmentType.Reference,
                        IsPrimary = file.SavedFileName == stepData.PrimaryFileName,
                        DisplayOrder = 0,
                        HasFormFields = false,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = "System"
                    };

                    _context.TemplateAttachments.Add(attachment);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Step 1 data for template {TemplateId}", templateId);
                return false;
            }
        }

        public async Task<TemplateValidationResult> ValidateTemplateCompletenessAsync(int templateId)
        {
            var result = new TemplateValidationResult();

            try
            {
                var template = await _context.PdfTemplates
                    .Include(t => t.Attachments)
                    .Include(t => t.FieldMappings)
                    .FirstOrDefaultAsync(t => t.Id == templateId);

                if (template == null)
                {
                    result.Errors.Add("Template not found");
                    return result;
                }

                // Validate Step 1: Files
                if (!template.Attachments.Any())
                {
                    result.Errors.Add("At least one PDF file must be uploaded");
                }

                if (!template.Attachments.Any(a => a.Type == AttachmentType.Original))
                {
                    result.Errors.Add("A primary PDF file must be selected");
                }

                // Validate Step 2: Template details
                if (string.IsNullOrWhiteSpace(template.Name))
                {
                    result.Errors.Add("Template name is required");
                }

                if (template.CategoryId <= 0)
                {
                    result.Errors.Add("Category selection is required");
                }

                if (template.DepartmentId <= 0)
                {
                    result.Errors.Add("Department selection is required");
                }

                // Validate Step 3: Field mappings (optional - add warnings)
                if (!template.FieldMappings.Any())
                {
                    result.Warnings.Add("No field mappings defined - template will have limited functionality");
                }

                result.IsValid = !result.Errors.Any();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating template {TemplateId}", templateId);
                result.Errors.Add("Validation failed due to system error");
                return result;
            }
        }

        // Finalize template - change status from Draft to Active
        public async Task<bool> FinalizeTemplateAsync(int templateId)
        {
            try
            {
                var template = await _context.PdfTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId);

                if (template == null)
                    return false;

                // Update status and timestamps
                template.Status = TemplateStatus.Active;
                template.ModifiedDate = DateTime.UtcNow;
                template.ModifiedBy = "System";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Template {TemplateId} finalized successfully", templateId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing template {TemplateId}", templateId);
                return false;
            }
        }

        // Helper: Update template status
        public async Task<bool> UpdateTemplateStatusAsync(int templateId, TemplateStatus status)
        {
            try
            {
                var template = await _context.PdfTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId);

                if (template == null)
                    return false;

                template.Status = status;
                template.ModifiedDate = DateTime.UtcNow;
                template.ModifiedBy = "System";

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating template status for {TemplateId}", templateId);
                return false;
            }
        }



        public async Task<TemplateValidationResult> ValidateAndActivateTemplateAsync(int templateId, FinalizeTemplateRequest request)
        {
            var result = new TemplateValidationResult();

            try
            {
                if (request.ProgressData != null)
                {
                    await SaveWizardProgressAsync(templateId, request.ProgressData);
                }

                var template = await GetTemplateAsync(templateId);
                if (template == null)
                {
                    result.Errors.Add("Template not found");
                    return result;
                }

                if (string.IsNullOrEmpty(template.Name))
                    result.Errors.Add("Template name is required");

                if (template.CategoryId == 0)
                    result.Errors.Add("Domain (Category) is required");

                if (template.DepartmentId == 0)
                    result.Errors.Add("Department is required");

                if (!template.VendorId.HasValue)
                    result.Errors.Add("Vendor (owner) is required");

                if (!template.Attachments.Any())
                    result.Errors.Add("At least one PDF file is required");

                if (!template.FieldMappings.Any())
                    result.Errors.Add("At least one field mapping is required");

                if (!result.Errors.Any())
                {
                    template.Status = TemplateStatus.Active;
                    template.ModifiedDate = DateTime.UtcNow;
                    template.ModifiedBy = "System";

                    if (!string.IsNullOrEmpty(request.ActivationNotes))
                    {
                        template.Description += $"\n\nActivation Notes: {request.ActivationNotes}";
                    }

                    await _context.SaveChangesAsync();
                    result.IsValid = true;

                    _logger.LogInformation("Activated template {TemplateId}", templateId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate/activate template {TemplateId}", templateId);
                result.Errors.Add($"Validation failed: {ex.Message}");
                return result;
            }
        }

        public async Task<TemplateWizardViewModel> SaveWizardStepAsync(int step, object stepData)
        {
            return new TemplateWizardViewModel { CurrentStep = step };
        }

        public async Task<PdfTemplate> CreateTemplateAsync(TemplateDefinitionDto definition)
        {
            var template = new PdfTemplate
            {
                Name = definition.Name,
                Description = definition.Description,
                CategoryId = definition.CategoryId,
                DepartmentId = definition.DepartmentId,
                VendorId = definition.VendorId,
                Status = TemplateStatus.Draft,
                CreatedDate = DateTime.UtcNow
            };

            _context.PdfTemplates.Add(template);
            await _context.SaveChangesAsync();
            return template;
        }

        public async Task<TemplateListViewModel> GetTemplateListAsync(TemplateFiltersViewModel filters)
        {
            var query = _context.PdfTemplates
                .Include(t => t.Category)
                .Include(t => t.Department).ThenInclude(d => d.GeneralDirectorate)
                .Include(t => t.Vendor)
                .Where(t => t.IsActive);

            if (!string.IsNullOrEmpty(filters.SearchTerm))
            {
                query = query.Where(t => t.Name.Contains(filters.SearchTerm) || t.Description.Contains(filters.SearchTerm));
            }

            if (filters.Status.HasValue)
            {
                query = query.Where(t => t.Status == filters.Status.Value);
            }

            if (filters.CategoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == filters.CategoryId.Value);
            }

            if (filters.DepartmentId.HasValue)
            {
                query = query.Where(t => t.DepartmentId == filters.DepartmentId.Value);
            }

            if (filters.VendorId.HasValue)
            {
                query = query.Where(t => t.VendorId == filters.VendorId.Value);
            }

            var totalCount = await query.CountAsync();

            var templates = await query
                .Skip((filters.Pagination.CurrentPage - 1) * filters.Pagination.PageSize)
                .Take(filters.Pagination.PageSize)
                .ToListAsync();

            var templateItems = templates.Select(t => new TemplateListItemViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Status = t.Status,
                CategoryName = t.Category.Name,
                DepartmentName = $"{t.Department.GeneralDirectorate.Name} - {t.Department.Name}",
                VendorName = t.Vendor?.Name ?? "No Vendor",
                CreatedDate = t.CreatedDate,
                ModifiedDate = t.ModifiedDate ?? t.CreatedDate,
                ProcessedCount = t.ProcessedCount,
                IsFinancialData = t.IsFinancialData,
                RequiresApproval = t.RequiresApproval,
                CreatedBy = t.CreatedBy ?? "System"
            }).ToList();

            return new TemplateListViewModel
            {
                Templates = templateItems,
                Filters = filters,
                Pagination = new PaginationViewModel
                {
                    CurrentPage = filters.Pagination.CurrentPage,
                    PageSize = filters.Pagination.PageSize,
                    TotalItems = totalCount
                }
            };
        }

        public async Task<bool> DeleteTemplateAsync(int id)
        {
            var template = await _context.PdfTemplates.FindAsync(id);
            if (template == null) return false;

            _context.PdfTemplates.Remove(template);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<TemplateDefinitionDto> ExportTemplateAsync(int id)
        {
            var template = await GetTemplateAsync(id);
            if (template == null) throw new ArgumentException($"Template {id} not found");

            return new TemplateDefinitionDto
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                CategoryId = template.CategoryId,
                DepartmentId = template.DepartmentId,
                VendorId = template.VendorId,
                Status = template.Status
            };
        }

    }
}