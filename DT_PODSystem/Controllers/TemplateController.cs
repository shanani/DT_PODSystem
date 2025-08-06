using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Helpers;
using DT_PODSystem.Helpers;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.Entities;
using DT_PODSystem.Models.Enums;
using DT_PODSystem.Models.ViewModels;
using DT_PODSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Controllers
{
    [Authorize]
    public class TemplateController : Controller
    {
        private readonly ITemplateService _templateService;
        private readonly IPdfProcessingService _pdfProcessingService;
        private readonly ILogger<TemplateController> _logger;

        public TemplateController(
            ITemplateService templateService,
            IPdfProcessingService pdfProcessingService,
            ILogger<TemplateController> logger)
        {
            _templateService = templateService;
            _pdfProcessingService = pdfProcessingService;
            _logger = logger;
        }

        
        // Wizard method - Updated to open with empty model, no auto template creation
        public async Task<IActionResult> Wizard(int step = 1, int? id = null)
        {
            try
            {
                // ✅ REMOVED: Auto-creation of draft template
                // ✅ NEW: If no ID provided, open with empty wizard model
                if (id == null)
                {
                    _logger.LogInformation("Opening wizard with empty model for new POD creation, step {Step}", step);

                    // Create empty wizard model for new POD creation
                    var emptyModel = await _templateService.GetWizardStateAsync(step, null);
                    emptyModel.CurrentStep = step;
                    emptyModel.TotalSteps = 2;
                    emptyModel.PODId = 0; // New POD
                    emptyModel.TemplateId = 0; // New Template

                    return View(emptyModel);
                }

                // Validate step range - 3 steps only
                if (step < 1 || step > 3)
                {
                    _logger.LogWarning("Invalid wizard step {Step} for ID {Id}", step, id);
                    return Redirect($"/Template/Wizard?step=1&id={id}");
                }

                // Load existing wizard state (could be POD or Template)
                var model = await _templateService.GetWizardStateAsync(step, id);
                if (model == null)
                {
                    _logger.LogError("Failed to load wizard state for ID {Id}, step {Step}", id.Value, step);
                    TempData.Error("Failed to load wizard. Please try again.", popup: false);
                    return RedirectToAction("Index");
                }

                // ✅ ENSURE these are set correctly for 3-step wizard
                model.CurrentStep = step;
                model.TotalSteps = 3;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in wizard for ID {Id}, step {Step}", id, step);
                TempData.Error("An error occurred. Please try again.", popup: false);
                return RedirectToAction("Index");
            }
        }

        // <summary>
        /// Get mapped fields information by field IDs for canvas synchronization
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetMappedFieldsInfo([FromBody] GetMappedFieldsInfoRequest request)
        {
            try
            {
                _logger.LogInformation("Getting mapped fields info for {Count} field IDs", request.FieldIds?.Count ?? 0);

                if (request?.FieldIds == null || !request.FieldIds.Any())
                {
                    return Json(new
                    {
                        success = true,
                        fields = new List<object>(),
                        message = "No field IDs provided"
                    });
                }

                // Use the template service to get field information
                var fieldsInfo = await _templateService.GetMappedFieldsInfoAsync(request.FieldIds);

                _logger.LogInformation("Found {Count} mapped fields out of {RequestedCount} requested",
                    fieldsInfo.Count, request.FieldIds.Count);

                return Json(new
                {
                    success = true,
                    fields = fieldsInfo,
                    message = $"Retrieved {fieldsInfo.Count} field(s) information"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting mapped fields info for IDs: {FieldIds}",
                    string.Join(",", request?.FieldIds ?? new List<int>()));

                return Json(new
                {
                    success = false,
                    message = "Error retrieving field information",
                    fields = new List<object>()
                });
            }
        }




        /// <summary>
        /// Update primary file selection via AJAX and ensure TemplateAttachments are created
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdatePrimaryFile([FromBody] UpdatePrimaryFileRequest request)
        {
            try
            {
                if (request?.TemplateId == null || string.IsNullOrEmpty(request.PrimaryFileName))
                {
                    return Json(new { success = false, message = "Invalid request data" });
                }

                _logger.LogInformation("Updating primary file for template {TemplateId} to {PrimaryFileName}",
                    request.TemplateId, request.PrimaryFileName);

                var result = await _templateService.UpdatePrimaryFileWithAttachmentsAsync(request.TemplateId, request.PrimaryFileName);

                if (result.Success)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Primary file updated successfully",
                        data = new
                        {
                            primaryFileName = request.PrimaryFileName,
                            attachmentsCreated = result.AttachmentsCreated,
                            attachmentsUpdated = result.AttachmentsUpdated
                        }
                    });
                }
                else
                {
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating primary file for template {TemplateId}", request?.TemplateId);
                return Json(new { success = false, message = "An error occurred while updating primary file" });
            }
        }


        // Add this new method to TemplateController.cs

        /// <summary>
        /// Get templates list for dropdown filters (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTemplatesForFilter()
        {
            try
            {
                _logger.LogInformation("Getting templates for filter dropdown");

                var templates = await _templateService.GetTemplatesForFilterAsync();

                return Json(new
                {
                    success = true,
                    data = templates,
                    count = templates.Count,
                    message = $"Retrieved {templates.Count} active templates"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting templates for filter");
                return Json(new
                {
                    success = false,
                    message = "Error retrieving templates",
                    data = new List<object>(),
                    count = 0
                });
            }
        }

        // Update the existing SearchMappedFields method to handle template filter
        [HttpPost]
        public async Task<IActionResult> SearchMappedFields([FromBody] SearchMappedFieldsRequest request)
        {
            try
            {
                _logger.LogInformation("Searching mapped fields with term: {SearchTerm}, TemplateIds: {TemplateIds}, Page: {Page}, PageSize: {PageSize}",
                    request.SearchTerm, request.TemplateIds != null ? string.Join(",", request.TemplateIds) : "All", request.Page, request.PageSize);

                // Validate request parameters
                if (request.PageSize <= 0 || request.PageSize > 100)
                {
                    request.PageSize = 20; // Default page size
                }

                if (request.Page < 0)
                {
                    request.Page = 0; // Default to first page
                }

                // Use template service to search mapped fields
                var searchResults = await _templateService.SearchMappedFieldsAsync(request);

                _logger.LogInformation("Found {TotalCount} mapped fields matching search criteria", searchResults.TotalCount);

                return Json(new
                {
                    success = true,
                    results = searchResults.Results,
                    totalCount = searchResults.TotalCount,
                    page = request.Page,
                    pageSize = request.PageSize,
                    hasMore = (request.Page + 1) * request.PageSize < searchResults.TotalCount,
                    message = searchResults.Results.Any() ?
                        $"Found {searchResults.TotalCount} mapped field(s)" :
                        "No mapped fields found matching your search criteria"
                });
            }
            catch (ArgumentException argEx)
            {
                _logger.LogWarning(argEx, "Invalid search parameters: {Message}", argEx.Message);
                return Json(new
                {
                    success = false,
                    message = "Invalid search parameters: " + argEx.Message,
                    results = new List<object>(),
                    totalCount = 0,
                    hasMore = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching mapped fields with term: {SearchTerm}", request.SearchTerm);
                return Json(new
                {
                    success = false,
                    message = "An error occurred while searching mapped fields. Please try again.",
                    results = new List<object>(),
                    totalCount = 0,
                    hasMore = false
                });
            }
        }



        /// <summary>
        /// Get all anchor points for a template (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTemplateAnchors(int templateId)
        {
            try
            {
                _logger.LogInformation("Getting anchor points for template {TemplateId}", templateId);

                var TemplateAnchors = await _pdfProcessingService.GetTemplateAnchorsAsync(templateId);

                _logger.LogInformation("Retrieved {Count} anchor points for template {TemplateId}",
                    TemplateAnchors.Count, templateId);

                return Json(new
                {
                    success = true,
                    data = TemplateAnchors,
                    count = TemplateAnchors.Count,
                    message = $"Retrieved {TemplateAnchors.Count} anchor points"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting anchor points for template {TemplateId}", templateId);
                return Json(new
                {
                    success = false,
                    message = "Error retrieving anchor points",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Add new anchor point (AJAX)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddTemplateAnchor([FromBody] AddTemplateAnchorRequest request)
        {
            try
            {
                _logger.LogInformation("Adding anchor point {Name} to template {TemplateId}",
                    request.TemplateAnchor.Name, request.TemplateId);

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Json(new
                    {
                        success = false,
                        message = "Validation failed",
                        errors = errors
                    });
                }

                var TemplateAnchor = await _pdfProcessingService.AddTemplateAnchorAsync(request.TemplateId, request.TemplateAnchor);

                _logger.LogInformation("Anchor point {Id} created successfully for template {TemplateId}",
                    TemplateAnchor.Id, request.TemplateId);

                return Json(new
                {
                    success = true,
                    data = TemplateAnchor,
                    message = "Anchor point added successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding anchor point to template {TemplateId}", request.TemplateId);
                return Json(new
                {
                    success = false,
                    message = "Error adding anchor point",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Update existing anchor point (AJAX)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateTemplateAnchor([FromBody] UpdateTemplateAnchorRequest request)
        {
            try
            {
                _logger.LogInformation("Updating anchor point {Id}", request.TemplateAnchorId);

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Json(new
                    {
                        success = false,
                        message = "Validation failed",
                        errors = errors
                    });
                }

                var TemplateAnchor = await _pdfProcessingService.UpdateTemplateAnchorAsync(request.TemplateAnchorId, request.TemplateAnchor);

                if (TemplateAnchor == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Anchor point not found"
                    });
                }

                _logger.LogInformation("Anchor point {Id} updated successfully", request.TemplateAnchorId);

                return Json(new
                {
                    success = true,
                    data = TemplateAnchor,
                    message = "Anchor point updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating anchor point {Id}", request.TemplateAnchorId);
                return Json(new
                {
                    success = false,
                    message = "Error updating anchor point",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Remove anchor point (AJAX)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RemoveTemplateAnchor([FromBody] RemoveTemplateAnchorRequest request)
        {
            try
            {
                _logger.LogInformation("Removing anchor point {Id} from template {TemplateId}",
                    request.TemplateAnchorId, request.TemplateId);

                var result = await _pdfProcessingService.RemoveTemplateAnchorAsync(request.TemplateId, request.TemplateAnchorId);

                if (result)
                {
                    _logger.LogInformation("Anchor point {Id} removed successfully", request.TemplateAnchorId);
                    return Json(new
                    {
                        success = true,
                        message = "Anchor point removed successfully"
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Anchor point not found or could not be removed"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing anchor point {Id}", request.TemplateAnchorId);
                return Json(new
                {
                    success = false,
                    message = "Error removing anchor point",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Add a new field mapping via AJAX
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddFieldMapping([FromBody] AddFieldMappingRequest request)
        {
            try
            {
                if (request == null || request.TemplateId <= 0 || request.FieldMapping == null)
                {
                    return Json(new { success = false, message = "Invalid request data" });
                }

                if (string.IsNullOrWhiteSpace(request.FieldMapping.FieldName))
                {
                    return Json(new { success = false, message = "Field name is required" });
                }

                _logger.LogInformation("Adding field mapping {FieldName} to template {TemplateId}",
                    request.FieldMapping.FieldName, request.TemplateId);

                var result = await _pdfProcessingService.AddFieldMappingAsync(request.TemplateId, request.FieldMapping);

                return Json(new
                {
                    success = true,
                    message = "Field mapping added successfully",
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Validation error adding field mapping");
                return Json(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument adding field mapping");
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding field mapping to template {TemplateId}", request.TemplateId);
                return Json(new { success = false, message = "Failed to add field mapping" });
            }
        }

        /// <summary>
        /// Update an existing field mapping via AJAX
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateFieldMapping([FromBody] UpdateFieldMappingRequest request)
        {
            try
            {
                if (request == null || request.FieldMappingId <= 0 || request.FieldMapping == null)
                {
                    return Json(new { success = false, message = "Invalid request data" });
                }

                if (string.IsNullOrWhiteSpace(request.FieldMapping.FieldName))
                {
                    return Json(new { success = false, message = "Field name is required" });
                }

                _logger.LogInformation("Updating field mapping {Id}", request.FieldMappingId);

                var result = await _pdfProcessingService.UpdateFieldMappingAsync(request.FieldMappingId, request.FieldMapping);

                return Json(new
                {
                    success = true,
                    message = "Field mapping updated successfully",
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Validation error updating field mapping");
                return Json(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument updating field mapping");
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating field mapping {Id}", request.FieldMappingId);
                return Json(new { success = false, message = "Failed to update field mapping" });
            }
        }

        /// <summary>
        /// Remove a field mapping via AJAX with usage validation
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RemoveFieldMapping([FromBody] RemoveFieldMappingRequest request)
        {
            try
            {
                if (request == null || request.TemplateId <= 0 || request.FieldMappingId <= 0)
                {
                    return Json(new { success = false, message = "Invalid request data" });
                }

                _logger.LogInformation("Removing field mapping {Id} from template {TemplateId}",
                    request.FieldMappingId, request.TemplateId);

                var result = await _pdfProcessingService.RemoveFieldMappingWithUsageCheckAsync(
                    request.TemplateId, request.FieldMappingId);

                if (result.Success)
                {
                    return Json(new
                    {
                        success = true,
                        message = result.Message,
                        data = new { fieldMappingId = request.FieldMappingId }
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = result.Message,
                        errorCode = result.ErrorCode,
                        usageDetails = result.UsageDetails,
                        requiredActions = result.RequiredActions
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing field mapping {Id} from template {TemplateId}",
                    request.FieldMappingId, request.TemplateId);
                return Json(new { success = false, message = "An error occurred while removing the field mapping" });
            }
        }

        /// <summary>
        /// Get all field mappings for a template via AJAX
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFieldMappings(int templateId)
        {
            try
            {
                if (templateId <= 0)
                {
                    return Json(new { success = false, message = "Invalid template ID" });
                }

                var fieldMappings = await _pdfProcessingService.GetFieldMappingsAsync(templateId);

                return Json(new
                {
                    success = true,
                    data = fieldMappings,
                    count = fieldMappings.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting field mappings for template {TemplateId}", templateId);
                return Json(new { success = false, message = "Failed to get field mappings" });
            }
        }

        /// <summary>
        /// Get a specific field mapping via AJAX
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFieldMapping(int fieldMappingId)
        {
            try
            {
                if (fieldMappingId <= 0)
                {
                    return Json(new { success = false, message = "Invalid field mapping ID" });
                }

                var fieldMapping = await _pdfProcessingService.GetFieldMappingAsync(fieldMappingId);

                if (fieldMapping == null)
                {
                    return Json(new { success = false, message = "Field mapping not found" });
                }

                return Json(new
                {
                    success = true,
                    data = fieldMapping
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting field mapping {Id}", fieldMappingId);
                return Json(new { success = false, message = "Failed to get field mapping" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveStep2([FromBody] SaveStep2Request request)
        {
            try
            {
                var success = await _templateService.SaveStep2DataAsync(request.TemplateId, request.Data);
                return Json(new { success, message = success ? "Step 2 saved" : "Failed to save" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Step 2");
                return Json(new { success = false, message = "Error occurred" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveStep1([FromBody] SaveStep1Request request)
        {
            try
            {
                var success = await _templateService.SaveStep1DataAsync(request.TemplateId, request.Data);
                return Json(new { success, message = success ? "Step 1 saved" : "Failed to save" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Step 1");
                return Json(new { success = false, message = "Error occurred" });
            }
        }

        // POST: Save field mappings (Step 3)
        [HttpPost]
        public async Task<IActionResult> SaveFieldMappings([FromBody] SaveFieldMappingsRequest request)
        {
            try
            {
                if (request?.TemplateId == null || request.FieldMappings == null)
                {
                    return Json(new { success = false, message = "Invalid request data" });
                }

                // Implement field mapping save logic if needed
                // For now, just validate and return success
                return Json(new { success = true, message = "Field mappings validated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating field mappings");
                return Json(new { success = false, message = "Error validating field mappings: " + ex.Message });
            }
        }

        // POST: Finalize template (change status from Draft to Active) - Now happens in Step 3
        [HttpPost]
        public async Task<IActionResult> FinalizeTemplate([FromBody] FinalizeTemplateRequest request)
        {
            try
            {
                if (request?.TemplateId == null)
                {
                    return Json(new { success = false, message = "Invalid template ID" });
                }

                _logger.LogInformation("Finalizing template {TemplateId}", request.TemplateId);

                // Validate template completeness
                TemplateValidationResult validation = await _templateService.ValidateTemplateCompletenessAsync(request.TemplateId);
                if (!validation.IsValid)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Template validation failed",
                        errors = validation.Errors
                    });
                }

                // Change status from Draft to Active
                var result = await _templateService.FinalizeTemplateAsync(request.TemplateId);
                if (result)
                {
                    _logger.LogInformation("Template {TemplateId} finalized successfully", request.TemplateId);
                    return Json(new { success = true, message = "Template created successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to finalize template" });
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing template {TemplateId}", request?.TemplateId);
                return Json(new { success = false, message = "An error occurred while finalizing template" });
            }
        }

        public async Task<IActionResult> Index(TemplateFiltersViewModel filters)
        {
            try
            {
                // Ensure pagination is set
                filters ??= new TemplateFiltersViewModel();
                filters.Pagination ??= new PaginationViewModel { CurrentPage = 1, PageSize = 25 };

                var model = await _templateService.GetTemplateListAsync(filters);

                // Set user permissions
                model.UserRole = string.Join(",", Util.GetCurrentUser().Roles.ToList());
                model.CanCreate = User.IsAdmin();
                model.CanEdit = User.IsAdmin();
                model.CanDelete = User.IsAdmin();
                model.CanViewFinancialData = User.IsAdmin() || User.IsInRole("Auditor");

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load template list");
                TempData["Error"] = "Failed to load templates";
                return View(new TemplateListViewModel());
            }
        }

      

        // GET: Template details view
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var template = await _templateService.GetTemplateAsync(id);
                if (template == null)
                {
                    TempData["Error"] = "Template not found";
                    return RedirectToAction("Index");
                }

                return View(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load template details for ID {TemplateId}", id);
                TempData["Error"] = "Failed to load template details";
                return RedirectToAction("Index");
            }
        }

        // POST: Delete template
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _templateService.DeleteTemplateAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Template deleted successfully" });
                }
                return Json(new { success = false, message = "Template not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template {TemplateId}", id);
                return Json(new { success = false, message = "Error deleting template: " + ex.Message });
            }
        }


    }

    // Request classes using your existing DTOs
    public class SaveStep2Request
    {
        public int TemplateId { get; set; }
        public Step2DataDto Data { get; set; } = new();
    }

    public class SaveStep1Request
    {
        public int TemplateId { get; set; }
        public Step1DataDto Data { get; set; } = new();
    }

    public class SaveStep3Request
    {
        public int TemplateId { get; set; }
        public Step3DataDto Data { get; set; } = new();
    }

    public class AddFieldMappingRequest
    {
        public int TemplateId { get; set; }
        public FieldMappingDto FieldMapping { get; set; } = null!;
    }

    public class UpdateFieldMappingRequest
    {
        public int FieldMappingId { get; set; }
        public FieldMappingDto FieldMapping { get; set; } = null!;
    }

    public class RemoveFieldMappingRequest
    {
        public int TemplateId { get; set; }
        public int FieldMappingId { get; set; }
    }

    public class AddTemplateAnchorRequest
    {
        [Required]
        public int TemplateId { get; set; }

        [Required]
        public TemplateAnchorDto TemplateAnchor { get; set; } = new();
    }

    public class UpdateTemplateAnchorRequest
    {
        [Required]
        public int TemplateAnchorId { get; set; }

        [Required]
        public TemplateAnchorDto TemplateAnchor { get; set; } = new();
    }

    public class RemoveTemplateAnchorRequest
    {
        [Required]
        public int TemplateId { get; set; }

        [Required]
        public int TemplateAnchorId { get; set; }
    }

    public class SaveFieldMappingsRequest
    {
        public int TemplateId { get; set; }
        public List<FieldMappingDto> FieldMappings { get; set; } = new();
    }

    // Add this request class at the bottom of TemplateController.cs with other request classes
    public class UpdatePrimaryFileRequest
    {
        public int TemplateId { get; set; }
        public string PrimaryFileName { get; set; } = string.Empty;
    }


    public class GetMappedFieldsInfoRequest
    {
        public List<int> FieldIds { get; set; } = new List<int>();
    }
}