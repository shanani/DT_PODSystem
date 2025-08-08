using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
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
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DT_PODSystem.Services.Implementation
{
    /// <summary>
    /// Template Service Implementation - Updated for POD Architecture
    /// Templates are now technical children of POD entities
    /// Step 1: Template Details (PODId + technical settings)
    /// Step 2: PDF Uploads  
    /// Step 3: Field Mapping
    /// </summary>
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



        public async Task<TemplateWizardViewModel> GetWizardStateAsync(int step = 1, int? templateId = null)
        {
            var model = new TemplateWizardViewModel
            {
                CurrentStep = step,
                TemplateId = templateId ?? 0
            };

            try
            {
                if (step == 1)
                {
                    await PopulatePODsListAsync(model.Step1);
                }

                if (templateId.HasValue && templateId.Value > 0)
                {
                    var template = await _context.PdfTemplates
                        .Include(t => t.POD)
                            .ThenInclude(p => p.Category)
                        .Include(t => t.POD)
                            .ThenInclude(p => p.Department)
                        .Include(t => t.POD)
                            .ThenInclude(p => p.Vendor)
                        .FirstOrDefaultAsync(t => t.Id == templateId.Value);

                    if (template != null)
                    {
                        model.TemplateId = template.Id;
                        model.PODId = template.PODId;

                        if (step == 1)
                        {
                            model.Step1.TemplateId = template.Id;
                            model.Step1.Title = template.Title ?? "";
                            model.Step1.PODId = template.PODId;
                            model.Step1.NamingConvention = template.NamingConvention ?? "DOC_POD";
                            model.Step1.TechnicalNotes = template.TechnicalNotes ?? "";
                            model.Step1.HasFormFields = template.HasFormFields;
                            model.Step1.ProcessingPriority = template.ProcessingPriority;
                            model.Step1.Version = template.Version ?? "1.0";

                            // ✅ MINIMAL FIX: Create new POD object with only needed properties
                            if (template.POD != null)
                            {
                                model.Step1.SelectedPOD = new POD
                                {
                                    Id = template.POD.Id,
                                    Name = template.POD.Name,
                                    PODCode = template.POD.PODCode,
                                    Description = template.POD.Description,
                                    CategoryId = template.POD.CategoryId,
                                    DepartmentId = template.POD.DepartmentId,
                                    VendorId = template.POD.VendorId,
                                    RequiresApproval = template.POD.RequiresApproval,
                                    IsFinancialData = template.POD.IsFinancialData,
                                    ProcessingPriority = template.POD.ProcessingPriority,

                                    // ✅ CRITICAL: Create simple objects for related entities without circular references
                                    Category = template.POD.Category != null ? new Category
                                    {
                                        Id = template.POD.Category.Id,
                                        Name = template.POD.Category.Name
                                    } : null,

                                    Department = template.POD.Department != null ? new Department
                                    {
                                        Id = template.POD.Department.Id,
                                        Name = template.POD.Department.Name
                                    } : null,

                                    Vendor = template.POD.Vendor != null ? new Vendor
                                    {
                                        Id = template.POD.Vendor.Id,
                                        Name = template.POD.Vendor.Name
                                    } : null

                                    // ✅ NO navigation collections that cause cycles
                                    // Templates = null (don't set this)
                                };
                            }
                        }
                    }
                }
                else
                {
                    if (step == 1)
                    {
                        model.Step1.TemplateId = 0;
                    }
                }

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading wizard state for template {TemplateId}, step {Step}", templateId, step);
                return model;
            }
        }



        /// <summary>
        /// ✅ NEW: Helper method to populate PODs list for dropdown
        /// </summary>
        private async Task PopulatePODsListAsync(Step1TemplateDetailsViewModel step1Model)
        {
            try
            {
                var pods = await _context.PODs
                    //.Where(p => p.IsActive && p.Status == PODStatus.Active)
                    .OrderBy(p => p.Name)
                    .Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = $"{p.Name} ({p.PODCode})",
                        Selected = false
                    })
                    .ToListAsync();

                step1Model.PODs = pods;

                _logger.LogInformation("Populated {Count} PODs for dropdown selection", pods.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating PODs list for dropdown");
                step1Model.PODs = new List<SelectListItem>();
            }
        }

        /// <summary>
        /// ✅ ALTERNATIVE: If you need the POD entity for display, create a safe DTO instead
        /// </summary>
        private PODSummaryDto CreateSafePODSummary(POD pod)
        {
            return new PODSummaryDto
            {
                Id = pod.Id,
                Name = pod.Name,
                PODCode = pod.PODCode,
                Description = pod.Description,
                CategoryName = pod.Category?.Name,
                DepartmentName = pod.Department?.Name,
                VendorName = pod.Vendor?.Name,
                Status = pod.Status,
                ProcessingPriority = pod.ProcessingPriority
            };
        }

        /// <summary>
        /// DTO to safely pass POD information without circular references
        /// </summary>
        public class PODSummaryDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string PODCode { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string? CategoryName { get; set; }
            public string? DepartmentName { get; set; }
            public string? VendorName { get; set; }
            public PODStatus Status { get; set; }
            public int ProcessingPriority { get; set; }
        }
        private List<WizardStepViewModel> GenerateStepInfo(int currentStep)
        {
            return new List<WizardStepViewModel>
    {
        new WizardStepViewModel
        {
            StepNumber = 1,
            Title = "Template Details",
            Description = "Select POD and configure template settings",
            Icon = "fa-cogs",
            IsActive = currentStep == 1,
            IsCompleted = currentStep > 1,
            IsAccessible = true
        },
        new WizardStepViewModel
        {
            StepNumber = 2,
            Title = "Upload Files",
            Description = "Upload PDF templates and attachments",
            Icon = "fa-upload",
            IsActive = currentStep == 2,
            IsCompleted = currentStep > 2,
            IsAccessible = currentStep >= 2
        },
        new WizardStepViewModel
        {
            StepNumber = 3,
            Title = "Field Mapping",
            Description = "Map PDF fields for data extraction",
            Icon = "fa-map-marker-alt",
            IsActive = currentStep == 3,
            IsCompleted = false,
            IsAccessible = currentStep >= 3
        }
    };
        }
        // ✅ UPDATED: GetMappedFieldsInfoAsync - Now includes POD information
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
                        .ThenInclude(t => t.POD) // ✅ NEW: Include POD parent
                    .Where(fm => fieldIds.Contains(fm.Id) && fm.Template.Status == TemplateStatus.Active)
                    .Select(fm => new MappedFieldInfo
                    {
                        FieldId = fm.Id,
                        FieldName = fm.FieldName,
                        DisplayName = fm.DisplayName ?? fm.FieldName,
                        TemplateName = fm.Template.POD.Name, // ✅ UPDATED: Now shows POD name
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

        // ✅ UPDATED: GetTemplatesForFilterAsync - Now returns POD information
        public async Task<List<TemplateFilterOption>> GetTemplatesForFilterAsync()
        {
            try
            {
                _logger.LogInformation("Getting active templates for filter dropdown");

                var templates = await _context.PdfTemplates
                    .Include(t => t.POD)
                        .ThenInclude(p => p.Category)
                    .Include(t => t.POD)
                        .ThenInclude(p => p.Vendor)
                    .Include(t => t.POD)
                        .ThenInclude(p => p.Department)
                            .ThenInclude(d => d.GeneralDirectorate)
                    .Where(t => t.Status == TemplateStatus.Active)
                    .OrderBy(t => t.POD.Name) // ✅ UPDATED: Order by POD name
                    .Select(t => new TemplateFilterOption
                    {
                        Id = t.Id,
                        PODId = t.PODId, // ✅ NEW
                        PODName = t.POD.Name, // ✅ NEW: Primary display name
                        NamingConvention = t.NamingConvention, // ✅ UPDATED: Technical naming only
                        CategoryName = t.POD.Category != null ? t.POD.Category.Name : "",
                        VendorName = t.POD.Vendor != null ? t.POD.Vendor.Name : "",
                        DepartmentName = t.POD.Department != null ? t.POD.Department.Name : "",
                        GeneralDirectorateName = t.POD.Department != null && t.POD.Department.GeneralDirectorate != null ?
                            t.POD.Department.GeneralDirectorate.Name : "",
                        FieldCount = t.FieldMappings.Count(fm => fm.IsActive),
                        Status = t.Status, // ✅ NEW
                        Version = t.Version ?? "1.0" // ✅ NEW
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

        // UpdatePrimaryFileWithAttachmentsAsync (unchanged)
        public class UpdatePrimaryFileResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public int AttachmentsCreated { get; set; }
            public int AttachmentsUpdated { get; set; }
        }

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
                    .Select(a => a.UploadedFile.SavedFileName) // ✅ FIXED: Access via navigation
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
                        UploadedFileId = uploadedFile.Id, // ✅ CLEAN: Only reference to central file
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

                    attachment.IsPrimary = attachment.UploadedFile.SavedFileName == primaryFileName; // ✅ FIXED: Access via navigation
                    attachment.Type = attachment.UploadedFile.SavedFileName == primaryFileName ? AttachmentType.Original : AttachmentType.Reference;

                    if (wasPrimary != attachment.IsPrimary || wasOriginal != (attachment.Type == AttachmentType.Original))
                    {
                        attachmentsUpdated++;
                    }
                }

                // Verify the primary file exists (either in existing attachments or new ones)
                var primaryFileExists = template.Attachments.Any(a => a.UploadedFile.SavedFileName == primaryFileName) ||
                                       attachmentsToCreate.Any(a => a.UploadedFile.SavedFileName == primaryFileName);

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

        // ✅ UPDATED: SearchMappedFieldsAsync - Now includes POD information
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
                        .ThenInclude(t => t.POD)
                            .ThenInclude(p => p.Category)
                    .Include(fm => fm.Template)
                        .ThenInclude(t => t.POD)
                            .ThenInclude(p => p.Department)
                                .ThenInclude(d => d.GeneralDirectorate)
                    .Include(fm => fm.Template)
                        .ThenInclude(t => t.POD)
                            .ThenInclude(p => p.Vendor)
                    .Where(fm => fm.Template.Status == TemplateStatus.Active)
                    .AsQueryable();

                // Apply multi-template filter if provided
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
                        fm.Template.POD.Name.ToLower().Contains(searchTermLower) || // ✅ UPDATED: Search POD name
                        (fm.Template.POD.Category != null && fm.Template.POD.Category.Name.ToLower().Contains(searchTermLower)) ||
                        (fm.Template.POD.Vendor != null && fm.Template.POD.Vendor.Name.ToLower().Contains(searchTermLower)) ||
                        (fm.Template.POD.Department != null && fm.Template.POD.Department.Name.ToLower().Contains(searchTermLower)) ||
                        (fm.Template.POD.Department != null &&
                         fm.Template.POD.Department.GeneralDirectorate != null &&
                         fm.Template.POD.Department.GeneralDirectorate.Name.ToLower().Contains(searchTermLower))
                    );
                }

                // Apply additional filters if provided
                if (!string.IsNullOrWhiteSpace(request.CategoryName))
                {
                    query = query.Where(fm => fm.Template.POD.Category != null &&
                        fm.Template.POD.Category.Name.ToLower().Contains(request.CategoryName.ToLower()));
                }

                if (!string.IsNullOrWhiteSpace(request.VendorName))
                {
                    query = query.Where(fm => fm.Template.POD.Vendor != null &&
                        fm.Template.POD.Vendor.Name.ToLower().Contains(request.VendorName.ToLower()));
                }

                if (!string.IsNullOrWhiteSpace(request.DepartmentName))
                {
                    query = query.Where(fm => fm.Template.POD.Department != null &&
                        fm.Template.POD.Department.Name.ToLower().Contains(request.DepartmentName.ToLower()));
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply ordering and pagination
                var results = await query
                    .OrderBy(fm => fm.Template.POD.Name) // ✅ UPDATED: Order by POD name
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
                        PODId = fm.Template.PODId, // ✅ NEW
                        PODName = fm.Template.POD.Name, // ✅ NEW: Primary display name
                        PODCode = fm.Template.POD.PODCode, // ✅ NEW
                        TemplateNamingConvention = fm.Template.NamingConvention, // ✅ UPDATED: Technical name only
                        CategoryName = fm.Template.POD.Category != null ? fm.Template.POD.Category.Name : "",
                        VendorName = fm.Template.POD.Vendor != null ? fm.Template.POD.Vendor.Name : "",
                        DepartmentName = fm.Template.POD.Department != null ? fm.Template.POD.Department.Name : "",
                        GeneralDirectorateName = fm.Template.POD.Department != null &&
                                              fm.Template.POD.Department.GeneralDirectorate != null ?
                                              fm.Template.POD.Department.GeneralDirectorate.Name : ""
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

        // ✅ NEW: CreateTemplateForPODAsync - Create template as child of POD
        public async Task<PdfTemplate> CreateTemplateForPODAsync(int podId, string namingConvention = "DOC_POD")
        {
            try
            {
                // Verify POD exists
                var pod = await _context.PODs.FirstOrDefaultAsync(p => p.Id == podId);
                if (pod == null)
                {
                    throw new ArgumentException($"POD with ID {podId} not found");
                }

                var template = new PdfTemplate
                {
                    PODId = podId, // ✅ NEW: Parent POD reference
                    NamingConvention = namingConvention,
                    Status = TemplateStatus.Draft,
                    Version = "1.0",
                    ProcessingPriority = pod.ProcessingPriority, // Inherit from POD
                    TechnicalNotes = "Template created for POD processing",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System",
                    IsActive = true
                };

                _context.PdfTemplates.Add(template);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created template with ID {TemplateId} for POD {PODId}", template.Id, podId);
                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating template for POD {PODId}", podId);
                throw;
            }
        }

        // ✅ UPDATED: GetTemplateAsync - Now includes POD parent
        public async Task<PdfTemplate?> GetTemplateAsync(int id)
        {
            return await _context.PdfTemplates
                .Include(t => t.POD)
                    .ThenInclude(p => p.Category)
                .Include(t => t.POD)
                    .ThenInclude(p => p.Department)
                        .ThenInclude(d => d.GeneralDirectorate)
                .Include(t => t.POD)
                    .ThenInclude(p => p.Vendor)
                .Include(t => t.Attachments)
                    .ThenInclude(a => a.UploadedFile) // ✅ CLEAN: Access file data via navigation
                .Include(t => t.FieldMappings)
                .FirstOrDefaultAsync(t => t.Id == id);
        }



        public async Task<bool> SaveStep1DataAsync(int templateId, Step1DataDto stepData)
        {
            try
            {
                var template = await _context.PdfTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId);

                if (template == null)
                {
                    _logger.LogWarning("Template {TemplateId} not found for Step 1 save", templateId);
                    return false;
                }


                //template.PODId= stepData.PODId;

                // Template identification
                template.Title = stepData.Title ?? template.Title;

                // Technical PDF processing configuration
                template.NamingConvention = stepData.NamingConvention ?? template.NamingConvention;
                template.Status = stepData.Status;
                template.Version = stepData.Version ?? template.Version;

                // Technical processing settings
                template.ProcessingPriority = stepData.ProcessingPriority;

                // Approval fields (only update if provided, usually handled elsewhere)
                if (!string.IsNullOrEmpty(stepData.ApprovedBy))
                    template.ApprovedBy = stepData.ApprovedBy;
                if (stepData.ApprovalDate.HasValue)
                    template.ApprovalDate = stepData.ApprovalDate;

                // Processing tracking fields are typically read-only, but include for completeness
                if (stepData.LastProcessedDate.HasValue)
                    template.LastProcessedDate = stepData.LastProcessedDate;
                template.ProcessedCount = stepData.ProcessedCount;

                // Technical notes
                template.TechnicalNotes = stepData.TechnicalNotes;

                // PDF-specific settings
                template.HasFormFields = stepData.HasFormFields;
                template.ExpectedPdfVersion = stepData.ExpectedPdfVersion;
                template.ExpectedPageCount = stepData.ExpectedPageCount;

                // Base entity fields
                template.IsActive = stepData.IsActive;

                // Audit fields (inherited from BaseEntity)
                template.ModifiedDate = DateTime.UtcNow;
                template.ModifiedBy = "System";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully saved Step 1 data for template {TemplateId}", templateId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Step 1 data for template {TemplateId}", templateId);
                return false;
            }
        }


        // ✅ UPDATED: SaveStep2DataAsync - Now saves PDF uploads (was Step1)
        public async Task<bool> SaveStep2DataAsync(int templateId, Step2DataDto stepData)
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
                    // Find or create UploadedFile record
                    var uploadedFile = await _context.UploadedFiles
                        .FirstOrDefaultAsync(f => f.SavedFileName == file.SavedFileName);

                    if (uploadedFile == null)
                    {
                        uploadedFile = new UploadedFile
                        {
                            OriginalFileName = file.OriginalFileName,
                            SavedFileName = file.SavedFileName,
                            FilePath = file.FilePath,
                            FileSize = file.FileSize,
                            ContentType = file.ContentType,
                            CreatedDate = DateTime.UtcNow,
                            CreatedBy = "System",
                            IsActive = true
                        };
                        _context.UploadedFiles.Add(uploadedFile);
                        await _context.SaveChangesAsync(); // Save to get ID
                    }

                    // Create clean TemplateAttachment (no file duplication)
                    var attachment = new TemplateAttachment
                    {
                        TemplateId = templateId,
                        UploadedFileId = uploadedFile.Id, // ✅ CLEAN: Only reference to central file
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

                _logger.LogInformation("Saved Step 2 data for template {TemplateId} with {FileCount} files",
                    templateId, stepData.UploadedFiles.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Step 2 data for template {TemplateId}", templateId);
                return false;
            }
        }

        // Validation, finalization, and other methods remain unchanged...
        public async Task<TemplateValidationResult> ValidateTemplateCompletenessAsync(int templateId)
        {
            var result = new TemplateValidationResult();

            try
            {
                var template = await _context.PdfTemplates
                    .Include(t => t.POD)
                    .Include(t => t.Attachments)
                    .Include(t => t.FieldMappings)
                    .FirstOrDefaultAsync(t => t.Id == templateId);

                if (template == null)
                {
                    result.Errors.Add("Template not found");
                    return result;
                }

                // Validate Step 2: Files (was Step 1)
                if (!template.Attachments.Any())
                {
                    result.Errors.Add("At least one PDF file must be uploaded");
                }

                if (!template.Attachments.Any(a => a.Type == AttachmentType.Original))
                {
                    result.Errors.Add("A primary PDF file must be selected");
                }

                // Validate Step 1: Template technical details
                if (string.IsNullOrWhiteSpace(template.NamingConvention))
                {
                    result.Errors.Add("Naming convention is required");
                }

                // ✅ UPDATED: Validate POD parent exists and has required fields
                if (template.POD == null)
                {
                    result.Errors.Add("Template must belong to a POD");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(template.POD.Name))
                    {
                        result.Errors.Add("POD name is required");
                    }

                    if (template.POD.CategoryId <= 0)
                    {
                        result.Errors.Add("POD category selection is required");
                    }

                    if (template.POD.DepartmentId <= 0)
                    {
                        result.Errors.Add("POD department selection is required");
                    }
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

        // ✅ UPDATED: ValidateAndActivateTemplateAsync - Now validates POD parent
        public async Task<TemplateValidationResult> ValidateAndActivateTemplateAsync(int templateId, FinalizeTemplateRequest request)
        {
            var result = new TemplateValidationResult();

            try
            {
                var template = await GetTemplateAsync(templateId);
                if (template == null)
                {
                    result.Errors.Add("Template not found");
                    return result;
                }

                // ✅ UPDATED: Validate POD parent instead of template directly
                if (template.POD == null)
                {
                    result.Errors.Add("Template must belong to a POD");
                    return result;
                }

                if (string.IsNullOrEmpty(template.POD.Name))
                    result.Errors.Add("POD name is required");

                if (template.POD.CategoryId == 0)
                    result.Errors.Add("POD category is required");

                if (template.POD.DepartmentId == 0)
                    result.Errors.Add("POD department is required");

                if (string.IsNullOrWhiteSpace(template.NamingConvention))
                    result.Errors.Add("Template naming convention is required");

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
                        template.TechnicalNotes += $"\n\nActivation Notes: {request.ActivationNotes}";
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

        // ✅ UPDATED: CreateTemplateAsync - Now requires POD parent
        public async Task<PdfTemplate> CreateTemplateAsync(TemplateDefinitionDto definition)
        {
            var template = new PdfTemplate
            {
                PODId = definition.PODId, // ✅ NEW: Parent POD reference
                NamingConvention = definition.NamingConvention ?? "DOC_POD",
                Status = TemplateStatus.Draft,
                Version = definition.Version ?? "1.0",
                ProcessingPriority = definition.ProcessingPriority,
                TechnicalNotes = definition.TechnicalNotes,
                HasFormFields = definition.HasFormFields,
                ExpectedPdfVersion = definition.ExpectedPdfVersion,
                ExpectedPageCount = definition.ExpectedPageCount,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };

            _context.PdfTemplates.Add(template);
            await _context.SaveChangesAsync();
            return template;
        }

        // ✅ UPDATED: GetTemplateListAsync - Now shows POD information
        public async Task<TemplateListViewModel> GetTemplateListAsync(TemplateFiltersViewModel filters)
        {
            var query = _context.PdfTemplates
                .Include(t => t.POD)
                    .ThenInclude(p => p.Category)
                .Include(t => t.POD)
                    .ThenInclude(p => p.Department)
                        .ThenInclude(d => d.GeneralDirectorate)
                .Include(t => t.POD)
                    .ThenInclude(p => p.Vendor)
                .Where(t => t.IsActive);

            if (!string.IsNullOrEmpty(filters.SearchTerm))
            {
                query = query.Where(t => t.POD.Name.Contains(filters.SearchTerm) ||
                                         t.POD.Description!.Contains(filters.SearchTerm) ||
                                         t.NamingConvention.Contains(filters.SearchTerm));
            }

            if (filters.Status.HasValue)
            {
                query = query.Where(t => t.Status == filters.Status.Value);
            }

            if (filters.CategoryId.HasValue)
            {
                query = query.Where(t => t.POD.CategoryId == filters.CategoryId.Value);
            }

            if (filters.DepartmentId.HasValue)
            {
                query = query.Where(t => t.POD.DepartmentId == filters.DepartmentId.Value);
            }

            if (filters.VendorId.HasValue)
            {
                query = query.Where(t => t.POD.VendorId == filters.VendorId.Value);
            }

            var totalCount = await query.CountAsync();

            var templates = await query
                .Skip((filters.Pagination.CurrentPage - 1) * filters.Pagination.PageSize)
                .Take(filters.Pagination.PageSize)
                .ToListAsync();

            var templateItems = templates.Select(t => new TemplateListItemViewModel
            {
                Id = t.Id,
                Name = t.POD.Name, // ✅ UPDATED: Show POD name as primary name
                Description = t.POD.Description, // ✅ UPDATED: Show POD description
                Status = t.Status,
                CategoryName = t.POD.Category.Name,
                DepartmentName = $"{t.POD.Department.GeneralDirectorate.Name} - {t.POD.Department.Name}",
                VendorName = t.POD.Vendor?.Name ?? "No Vendor",
                CreatedDate = t.CreatedDate,
                ModifiedDate = t.ModifiedDate ?? t.CreatedDate,
                ProcessedCount = t.ProcessedCount,
                IsFinancialData = t.POD.IsFinancialData, // ✅ UPDATED: From POD
                RequiresApproval = t.POD.RequiresApproval, // ✅ UPDATED: From POD
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

        // ✅ UPDATED: ExportTemplateAsync - Now includes POD information
        public async Task<TemplateDefinitionDto> ExportTemplateAsync(int id)
        {
            var template = await GetTemplateAsync(id);
            if (template == null)
                throw new ArgumentException($"Template {id} not found");

            return new TemplateDefinitionDto
            {
                Id = template.Id,
                PODId = template.PODId, // ✅ NEW
                NamingConvention = template.NamingConvention,
                Status = template.Status,
                Version = template.Version,
                ProcessingPriority = template.ProcessingPriority,
                TechnicalNotes = template.TechnicalNotes,
                HasFormFields = template.HasFormFields,
                ExpectedPdfVersion = template.ExpectedPdfVersion,
                ExpectedPageCount = template.ExpectedPageCount
            };
        }

        // Helper methods (unchanged signatures, updated implementations)
        public async Task<List<TemplateAttachment>> GetTemplateAttachmentsAsync(int templateId)
        {
            return await _context.TemplateAttachments
                .Include(a => a.UploadedFile) // ✅ CLEAN: Access file data via navigation
                .Where(a => a.TemplateId == templateId)
                .OrderBy(a => a.Type)
                .ThenBy(a => a.DisplayOrder)
                .ToListAsync();
        }

        public async Task<List<FieldMapping>> GetTemplateFieldMappingsAsync(int templateId)
        {
            return await _context.FieldMappings
                .Where(f => f.TemplateId == templateId)
                .OrderBy(f => f.PageNumber)
                .ThenBy(f => f.Y)
                .ThenBy(f => f.X)
                .ToListAsync();
        }

        public async Task<bool> UpdatePrimaryFileAsync(int templateId, string primaryFileName)
        {
            try
            {
                var template = await _context.PdfTemplates
                    .Include(t => t.Attachments)
                        .ThenInclude(a => a.UploadedFile)
                    .FirstOrDefaultAsync(t => t.Id == templateId);

                if (template == null)
                {
                    _logger.LogWarning("Template {TemplateId} not found for primary file update", templateId);
                    return false;
                }

                // Find the attachment with the specified filename
                var targetAttachment = template.Attachments
                    .FirstOrDefault(a => a.UploadedFile.SavedFileName == primaryFileName); // ✅ CLEAN: Access via navigation

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

        public async Task<bool> DeleteTemplateAsync(int id)
        {
            var template = await _context.PdfTemplates.FindAsync(id);
            if (template == null) return false;

            _context.PdfTemplates.Remove(template);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}