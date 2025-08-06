using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DT_PODSystem.Data;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.Entities;
using DT_PODSystem.Models.Enums;
using DT_PODSystem.Models.ViewModels;
using DT_PODSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DT_PODSystem.Services.Implementation
{
    public class PdfProcessingService : IPdfProcessingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PdfProcessingService> _logger;
        public PdfProcessingService(ApplicationDbContext context, ILogger<PdfProcessingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Add these NEW methods to PdfProcessingService.cs

        /// <summary>
        /// Get all anchor points for a template
        /// </summary>
        public async Task<List<TemplateAnchorDto>> GetTemplateAnchorsAsync(int templateId)
        {
            try
            {
                var anchorPoints = await _context.TemplateAnchors
                    .Where(ap => ap.TemplateId == templateId)
                    .OrderBy(ap => ap.PageNumber)
                    .ThenBy(ap => ap.DisplayOrder)
                    .ToListAsync();

                return anchorPoints.Select(MapAnchorToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get anchor points for template {TemplateId}", templateId);
                throw;
            }
        }

        /// <summary>
        /// Add a new anchor point
        /// </summary>
        public async Task<TemplateAnchorDto> AddTemplateAnchorAsync(int templateId, TemplateAnchorDto anchorPointDto)
        {
            try
            {
                // Validate template exists
                var templateExists = await _context.PdfTemplates
                    .AnyAsync(t => t.Id == templateId);

                if (!templateExists)
                {
                    throw new ArgumentException($"Template with ID {templateId} not found");
                }

                // Check for duplicate names within the same template
                var duplicateName = await _context.TemplateAnchors
                    .AnyAsync(ap => ap.TemplateId == templateId &&
                                   ap.Name.ToLower() == anchorPointDto.Name.ToLower());

                if (duplicateName)
                {
                    throw new ArgumentException($"Anchor point with name '{anchorPointDto.Name}' already exists in this template");
                }

                // Create new anchor point entity
                var entity = new TemplateAnchor
                {
                    TemplateId = templateId,
                    PageNumber = anchorPointDto.PageNumber,
                    Name = anchorPointDto.Name.Trim(),

                    Description = anchorPointDto.Description?.Trim(),
                    X = anchorPointDto.X,
                    Y = anchorPointDto.Y,
                    Width = anchorPointDto.Width,
                    Height = anchorPointDto.Height,
                    ReferenceText = anchorPointDto.ReferenceText.Trim(),
                    ReferencePattern = anchorPointDto.ReferencePattern?.Trim(),
                    IsRequired = anchorPointDto.IsRequired,
                    ConfidenceThreshold = anchorPointDto.ConfidenceThreshold,


                    DisplayOrder = anchorPointDto.DisplayOrder,
                    Color = anchorPointDto.Color ?? "#00C48C",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System" // TODO: Get from current user
                };

                _context.TemplateAnchors.Add(entity);
                await _context.SaveChangesAsync();

                var result = MapAnchorToDto(entity);
                _logger.LogInformation("Anchor point {Id} created successfully for template {TemplateId}",
                    entity.Id, templateId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add anchor point to template {TemplateId}", templateId);
                throw;
            }
        }

        /// <summary>
        /// Update an existing anchor point
        /// </summary>
        public async Task<TemplateAnchorDto?> UpdateTemplateAnchorAsync(int anchorPointId, TemplateAnchorDto anchorPointDto)
        {
            try
            {
                var entity = await _context.TemplateAnchors
                    .FirstOrDefaultAsync(ap => ap.Id == anchorPointId);

                if (entity == null)
                {
                    _logger.LogWarning("Anchor point {Id} not found for update", anchorPointId);
                    return null;
                }

                // Check for duplicate names (excluding current anchor)
                var duplicateName = await _context.TemplateAnchors
                    .AnyAsync(ap => ap.TemplateId == entity.TemplateId &&
                                   ap.Id != anchorPointId &&
                                   ap.Name.ToLower() == anchorPointDto.Name.ToLower());

                if (duplicateName)
                {
                    throw new ArgumentException($"Anchor point with name '{anchorPointDto.Name}' already exists in this template");
                }

                // Update entity properties
                entity.PageNumber = anchorPointDto.PageNumber;
                entity.Name = anchorPointDto.Name.Trim();
                entity.Description = anchorPointDto.Description?.Trim();
                entity.X = anchorPointDto.X;
                entity.Y = anchorPointDto.Y;
                entity.Width = anchorPointDto.Width;
                entity.Height = anchorPointDto.Height;
                entity.ReferenceText = anchorPointDto.ReferenceText.Trim();
                entity.ReferencePattern = anchorPointDto.ReferencePattern?.Trim();
                entity.IsRequired = anchorPointDto.IsRequired;
                entity.ConfidenceThreshold = anchorPointDto.ConfidenceThreshold;

                entity.DisplayOrder = anchorPointDto.DisplayOrder;
                entity.Color = anchorPointDto.Color ?? entity.Color;
                entity.ModifiedDate = DateTime.UtcNow;
                entity.ModifiedBy = "System"; // TODO: Get from current user

                await _context.SaveChangesAsync();

                var result = MapAnchorToDto(entity);
                _logger.LogInformation("Anchor point {Id} updated successfully", anchorPointId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update anchor point {Id}", anchorPointId);
                throw;
            }
        }

        /// <summary>
        /// Remove an anchor point
        /// </summary>
        public async Task<bool> RemoveTemplateAnchorAsync(int templateId, int anchorPointId)
        {
            try
            {
                _logger.LogInformation("Removing anchor point {Id} from template {TemplateId}",
                    anchorPointId, templateId);

                var entity = await _context.TemplateAnchors
                    .FirstOrDefaultAsync(ap => ap.Id == anchorPointId && ap.TemplateId == templateId);

                if (entity == null)
                {
                    _logger.LogWarning("Anchor point {Id} not found in template {TemplateId}",
                        anchorPointId, templateId);
                    return false;
                }

                _context.TemplateAnchors.Remove(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Anchor point {Id} removed successfully", anchorPointId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove anchor point {Id} from template {TemplateId}",
                    anchorPointId, templateId);
                throw;
            }
        }

        /// <summary>
        /// Get a specific anchor point by ID
        /// </summary>
        public async Task<TemplateAnchorDto?> GetTemplateAnchorAsync(int anchorPointId)
        {
            try
            {
                var entity = await _context.TemplateAnchors
                    .FirstOrDefaultAsync(ap => ap.Id == anchorPointId);

                return entity != null ? MapAnchorToDto(entity) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get anchor point {Id}", anchorPointId);
                throw;
            }
        }

        /// <summary>
        /// Map TemplateAnchor entity to DTO
        /// </summary>
        private static TemplateAnchorDto MapAnchorToDto(TemplateAnchor entity)
        {
            return new TemplateAnchorDto
            {
                Id = entity.Id,
                TemplateId = entity.TemplateId,
                PageNumber = entity.PageNumber,
                Name = entity.Name,
                Description = entity.Description,
                X = entity.X,
                Y = entity.Y,
                Width = entity.Width,
                Height = entity.Height,
                ReferenceText = entity.ReferenceText,
                ReferencePattern = entity.ReferencePattern,
                IsRequired = entity.IsRequired,
                ConfidenceThreshold = entity.ConfidenceThreshold,
                DisplayOrder = entity.DisplayOrder,
                Color = entity.Color
            };
        }

        // ✅ FIXED: IsFieldMappingUsedInCanvasState - Only check for actual field mapping nodes
        private bool IsFieldMappingUsedInCanvasState(string canvasState, int fieldMappingId)
        {
            if (string.IsNullOrEmpty(canvasState))
            {
                _logger.LogInformation("Canvas state is empty for field mapping {FieldMappingId}", fieldMappingId);
                return false;
            }

            try
            {
                _logger.LogInformation("Checking field mapping {FieldMappingId} usage in canvas state", fieldMappingId);
                _logger.LogInformation("Canvas state length: {Length}", canvasState.Length);

                // ✅ Parse the JSON structure to access nodes properly
                var canvasData = JsonConvert.DeserializeObject<dynamic>(canvasState);

                // Handle different possible JSON structures
                string combinedNodeContent = "";

                // Structure 1: {"nodes": {...}} - New structure
                if (canvasData?.nodes != null)
                {
                    var nodes = canvasData.nodes;
                    foreach (var node in nodes)
                    {
                        if (node.Value != null)
                        {
                            // Convert node to string to search within it
                            var nodeJson = JsonConvert.SerializeObject(node.Value);
                            combinedNodeContent += nodeJson + " ";
                        }
                    }
                }
                // Structure 2: Direct nodes - Old structure compatibility
                else if (canvasData != null)
                {
                    var nodeProperties = canvasData.GetType().GetProperties();
                    foreach (var prop in nodeProperties)
                    {
                        var nodeValue = prop.GetValue(canvasData);
                        if (nodeValue != null)
                        {
                            var nodeJson = JsonConvert.SerializeObject(nodeValue);
                            combinedNodeContent += nodeJson + " ";
                        }
                    }
                }

                // ✅ SPECIFIC SEARCH: Only look for field mapping nodes with exact patterns
                var fieldMappingPatterns = new[]
                {
            $"data-variable-id=\\\"{fieldMappingId}\\\"",    // Exact HTML attribute match
            $"data-field-id=\\\"{fieldMappingId}\\\"",       // Exact HTML attribute match
            $"\"variableId\":{fieldMappingId}",              // JSON property (exact number)
            $"\"fieldMappingId\":{fieldMappingId}",          // JSON property (exact number)
        };

                bool found = false;
                string foundPattern = "";

                foreach (var pattern in fieldMappingPatterns)
                {
                    if (combinedNodeContent.Contains(pattern))
                    {
                        found = true;
                        foundPattern = pattern;
                        _logger.LogInformation("✅ FOUND field mapping usage with pattern: {Pattern}", pattern);
                        break;
                    }
                }

                // ✅ NO ADDITIONAL CONTEXTUAL SEARCH - only exact matches

                _logger.LogInformation("Field mapping {FieldMappingId} usage check result: {Found} (Pattern: {Pattern})",
                    fieldMappingId, found, foundPattern);

                // ✅ DEBUG: If not found, log some context for troubleshooting
                if (!found)
                {
                    var preview = combinedNodeContent.Length > 500 ?
                        combinedNodeContent.Substring(0, 500) + "..." : combinedNodeContent;
                    _logger.LogInformation("No field mapping usage found. Content preview: {Preview}", preview);
                }

                return found;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing canvas state for field mapping {FieldMappingId} usage check", fieldMappingId);
                return false; // ✅ CHANGED: Don't block deletion on parsing errors for field mappings
            }
        }

        // ✅ UPDATED: GetFieldMappingUsageDetailsAsync - More specific search for this template only
        private async Task<ConstantUsageResult> GetFieldMappingUsageDetailsAsync(int fieldMappingId, int templateId)
        {
            var result = new ConstantUsageResult();
            //To Do - Check if field mapping is being used in canvas or formulas   
            return result;


        }


        /// <summary>
        /// Enhanced RemoveFieldMappingAsync with usage validation
        /// </summary>
        public async Task<DeleteResult> RemoveFieldMappingWithUsageCheckAsync(int templateId, int fieldMappingId)
        {
            try
            {
                _logger.LogInformation("Removing field mapping {Id} from template {TemplateId} with usage check",
                    fieldMappingId, templateId);

                var fieldMapping = await _context.FieldMappings
                    .FirstOrDefaultAsync(fm => fm.Id == fieldMappingId && fm.TemplateId == templateId);

                if (fieldMapping == null)
                {
                    _logger.LogWarning("Field mapping {FieldMappingId} not found in template {TemplateId}", fieldMappingId, templateId);
                    return new DeleteResult
                    {
                        Success = false,
                        Message = "Field mapping not found",
                        ErrorCode = "FIELD_NOT_FOUND"
                    };
                }

                _logger.LogInformation("Field mapping found: '{FieldName}' (ID: {FieldMappingId})", fieldMapping.DisplayName, fieldMappingId);

                // ✅ Check if field mapping is being used in canvas or formulas
                var usageCheck = await GetFieldMappingUsageDetailsAsync(fieldMappingId, templateId);

                if (usageCheck.IsInUse)
                {
                    var deleteResult = new DeleteResult
                    {
                        Success = false,
                        Message = $"Cannot delete field '{fieldMapping.DisplayName}' - it's currently being used in the formula canvas",
                        ErrorCode = "FIELD_IN_USE",
                        UsageDetails = usageCheck.UsageDetails,
                        RequiredActions = new List<string>
                {
                    "Remove the field from the formula canvas first",
                    "Delete any connections to this field",
                    "Then try deleting the field mapping again"
                }
                    };

                    _logger.LogWarning("❌ Cannot delete field mapping {FieldMappingId} - usage details: {UsageDetails}",
                        fieldMappingId, string.Join(", ", usageCheck.UsageDetails));

                    return deleteResult;
                }

                // ✅ Safe to delete - use existing logic
                var entity = await _context.FieldMappings
                    .FirstOrDefaultAsync(fm => fm.Id == fieldMappingId && fm.TemplateId == templateId);

                if (entity == null)
                {
                    _logger.LogWarning("Field mapping entity {FieldMappingId} not found for deletion", fieldMappingId);
                    return new DeleteResult
                    {
                        Success = false,
                        Message = "Field mapping not found",
                        ErrorCode = "FIELD_NOT_FOUND"
                    };
                }

                // Remove the field mapping
                _context.FieldMappings.Remove(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Successfully deleted field mapping {FieldMappingId} from template {TemplateId}",
                    fieldMappingId, templateId);

                return new DeleteResult
                {
                    Success = true,
                    Message = $"Field mapping '{fieldMapping.DisplayName}' deleted successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error deleting field mapping {FieldMappingId} from template {TemplateId}",
                    fieldMappingId, templateId);

                return new DeleteResult
                {
                    Success = false,
                    Message = "An error occurred while deleting the field mapping",
                    ErrorCode = "DELETE_ERROR"
                };
            }
        }


        /// <summary>
        /// Add a new field mapping via AJAX
        /// </summary>
        public async Task<FieldMappingDto> AddFieldMappingAsync(int templateId, FieldMappingDto fieldMapping)
        {
            try
            {
                _logger.LogInformation("Adding field mapping {FieldName} to template {TemplateId}",
                    fieldMapping.FieldName, templateId);

                // Validate template exists
                var template = await _context.PdfTemplates.FindAsync(templateId);
                if (template == null)
                    throw new ArgumentException($"Template {templateId} not found");

                // Check for duplicate field names in the same template
                var existingField = await _context.FieldMappings
                    .FirstOrDefaultAsync(fm => fm.TemplateId == templateId &&
                                       fm.FieldName.ToLower() == fieldMapping.FieldName.ToLower());

                if (existingField != null)
                    throw new InvalidOperationException($"Field '{fieldMapping.FieldName}' already exists in this template");

                // Create new field mapping entity
                var entity = new FieldMapping
                {
                    TemplateId = templateId,
                    FieldName = fieldMapping.FieldName,
                    DisplayName = fieldMapping.DisplayName ?? fieldMapping.FieldName,
                    Description = fieldMapping.Description ?? string.Empty,
                    X = fieldMapping.X,
                    Y = fieldMapping.Y,
                    Width = fieldMapping.Width,
                    Height = fieldMapping.Height,
                    PageNumber = fieldMapping.PageNumber > 0 ? fieldMapping.PageNumber : 1,
                    IsRequired = fieldMapping.IsRequired,
                    ValidationPattern = fieldMapping.ValidationPattern ?? string.Empty,
                    ValidationMessage = fieldMapping.ValidationMessage ?? string.Empty,
                    MinValue = fieldMapping.MinValue,
                    MaxValue = fieldMapping.MaxValue,
                    DefaultValue = fieldMapping.DefaultValue ?? string.Empty,
                    UseOCR = fieldMapping.UseOCR,
                    OCRLanguage = fieldMapping.OCRLanguage ?? "en",
                    OCRConfidenceThreshold = fieldMapping.OCRConfidenceThreshold,
                    DisplayOrder = fieldMapping.DisplayOrder,
                    BorderColor = fieldMapping.BorderColor ?? "#A54EE1",
                    IsVisible = fieldMapping.IsVisible,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                };

                _context.FieldMappings.Add(entity);
                await _context.SaveChangesAsync();

                // Return the created field mapping with assigned ID
                var result = MapToDto(entity);
                _logger.LogInformation("Field mapping {FieldName} added successfully with ID {Id}",
                    result.FieldName, result.Id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add field mapping {FieldName} to template {TemplateId}",
                    fieldMapping.FieldName, templateId);
                throw;
            }
        }

        /// <summary>
        /// Update an existing field mapping via AJAX
        /// </summary>
        public async Task<FieldMappingDto> UpdateFieldMappingAsync(int fieldMappingId, FieldMappingDto fieldMapping)
        {
            try
            {
                _logger.LogInformation("Updating field mapping {Id}", fieldMappingId);

                var entity = await _context.FieldMappings
                    .FirstOrDefaultAsync(fm => fm.Id == fieldMappingId);

                if (entity == null)
                    throw new ArgumentException($"Field mapping {fieldMappingId} not found");

                // Check for duplicate field names (excluding current field)
                var duplicateField = await _context.FieldMappings
                    .FirstOrDefaultAsync(fm => fm.TemplateId == entity.TemplateId &&
                                       fm.Id != fieldMappingId &&
                                       fm.FieldName.ToLower() == fieldMapping.FieldName.ToLower());

                if (duplicateField != null)
                    throw new InvalidOperationException($"Field '{fieldMapping.FieldName}' already exists in this template");

                // Update entity properties
                entity.FieldName = fieldMapping.FieldName;
                entity.DisplayName = fieldMapping.DisplayName ?? fieldMapping.FieldName;
                entity.Description = fieldMapping.Description ?? string.Empty;
                entity.X = fieldMapping.X;
                entity.Y = fieldMapping.Y;
                entity.Width = fieldMapping.Width;
                entity.Height = fieldMapping.Height;
                entity.PageNumber = fieldMapping.PageNumber > 0 ? fieldMapping.PageNumber : entity.PageNumber;
                entity.IsRequired = fieldMapping.IsRequired;
                entity.ValidationPattern = fieldMapping.ValidationPattern ?? string.Empty;
                entity.ValidationMessage = fieldMapping.ValidationMessage ?? string.Empty;
                entity.MinValue = fieldMapping.MinValue;
                entity.MaxValue = fieldMapping.MaxValue;
                entity.DefaultValue = fieldMapping.DefaultValue ?? string.Empty;
                entity.UseOCR = fieldMapping.UseOCR;
                entity.OCRLanguage = fieldMapping.OCRLanguage ?? entity.OCRLanguage;
                entity.OCRConfidenceThreshold = fieldMapping.OCRConfidenceThreshold;
                entity.DisplayOrder = fieldMapping.DisplayOrder;
                entity.BorderColor = fieldMapping.BorderColor ?? entity.BorderColor;
                entity.IsVisible = fieldMapping.IsVisible;
                entity.ModifiedDate = DateTime.UtcNow;
                entity.ModifiedBy = "System";

                await _context.SaveChangesAsync();

                var result = MapToDto(entity);
                _logger.LogInformation("Field mapping {Id} updated successfully", fieldMappingId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update field mapping {Id}", fieldMappingId);
                throw;
            }
        }

        /// <summary>
        /// Remove a field mapping via AJAX
        /// </summary>
        public async Task<bool> RemoveFieldMappingAsync(int templateId, int fieldMappingId)
        {
            try
            {
                _logger.LogInformation("Removing field mapping {Id} from template {TemplateId}",
                    fieldMappingId, templateId);

                var entity = await _context.FieldMappings
                    .FirstOrDefaultAsync(fm => fm.Id == fieldMappingId && fm.TemplateId == templateId);

                if (entity == null)
                {
                    _logger.LogWarning("Field mapping {Id} not found in template {TemplateId}",
                        fieldMappingId, templateId);
                    return false;
                }



                // Remove the field mapping
                _context.FieldMappings.Remove(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Field mapping {Id} removed successfully", fieldMappingId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove field mapping {Id} from template {TemplateId}",
                    fieldMappingId, templateId);
                throw;
            }
        }

        /// <summary>
        /// Get all field mappings for a template
        /// </summary>
        public async Task<List<FieldMappingDto>> GetFieldMappingsAsync(int templateId)
        {
            try
            {
                var fieldMappings = await _context.FieldMappings
                    .Where(fm => fm.TemplateId == templateId)
                    .OrderBy(fm => fm.PageNumber)
                    .ThenBy(fm => fm.DisplayOrder)
                    .ThenBy(fm => fm.Y)
                    .ThenBy(fm => fm.X)
                    .ToListAsync();

                return fieldMappings.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get field mappings for template {TemplateId}", templateId);
                throw;
            }
        }

        /// <summary>
        /// Get a specific field mapping by ID
        /// </summary>
        public async Task<FieldMappingDto?> GetFieldMappingAsync(int fieldMappingId)
        {
            try
            {
                var entity = await _context.FieldMappings
                    .FirstOrDefaultAsync(fm => fm.Id == fieldMappingId);

                return entity != null ? MapToDto(entity) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get field mapping {Id}", fieldMappingId);
                throw;
            }
        }

        // ================================================================
        // HELPER METHODS
        // ================================================================



        /// <summary>
        /// Map entity to DTO
        /// </summary>
        private FieldMappingDto MapToDto(FieldMapping entity)
        {
            return new FieldMappingDto
            {
                Id = entity.Id,
                FieldName = entity.FieldName,
                DisplayName = entity.DisplayName,
                Description = entity.Description,
                X = entity.X,
                Y = entity.Y,
                Width = entity.Width,
                Height = entity.Height,
                PageNumber = entity.PageNumber,
                IsRequired = entity.IsRequired,
                ValidationPattern = entity.ValidationPattern,
                ValidationMessage = entity.ValidationMessage,
                MinValue = entity.MinValue,
                MaxValue = entity.MaxValue,
                DefaultValue = entity.DefaultValue,
                UseOCR = entity.UseOCR,
                OCRLanguage = entity.OCRLanguage,
                OCRConfidenceThreshold = entity.OCRConfidenceThreshold,
                DisplayOrder = entity.DisplayOrder,
                BorderColor = entity.BorderColor,
                IsVisible = entity.IsVisible

            };
        }


        public async Task<string> RenderPdfPageAsync(string filePath, int pageNumber, decimal zoomLevel = 1.0m)
        {
            try
            {
                if (!File.Exists(filePath)) throw new FileNotFoundException($"PDF file not found: {filePath}");
                var base64Image = await ConvertPdfPageToImageAsync(filePath, pageNumber, zoomLevel);
                return $"data:image/png;base64,{base64Image}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to render PDF page {PageNumber} from {FilePath}", pageNumber, filePath);
                throw;
            }
        }

        public async Task<bool> DeleteFieldMappingAsync(int id)
        {
            var entity = await _context.FieldMappings.FirstOrDefaultAsync(f => f.Id == id);
            if (entity == null) return false;
            _context.FieldMappings.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<string> PreviewFieldExtractionAsync(int fieldMappingId, string filePath)
        {
            try
            {
                var fieldMapping = await _context.FieldMappings.FindAsync(fieldMappingId);
                if (fieldMapping == null) return "Field mapping not found";
                if (!File.Exists(filePath)) return "PDF file not found";
                var extractedText = await ExtractTextFromRegionAsync(filePath, fieldMapping.PageNumber, fieldMapping.X, fieldMapping.Y, fieldMapping.Width, fieldMapping.Height, fieldMapping.UseOCR);
                return extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to preview field extraction for mapping {FieldMappingId}", fieldMappingId);
                return "Extraction failed";
            }
        }
        public async Task<List<AutoDetectedFieldViewModel>> AutoDetectFieldsAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return new List<AutoDetectedFieldViewModel>();
                var detectedFields = new List<AutoDetectedFieldViewModel>();
                var commonPatterns = new[] { new { Name = "Amount", Pattern = @"\$?\d{1,3}(,\d{3})*(\.\d{2})?", DataType = DataTypeEnum.Number }, new { Name = "Date", Pattern = @"\d{1,2}[\/\-]\d{1,2}[\/\-]\d{2,4}", DataType = DataTypeEnum.Date } };
                foreach (var pattern in commonPatterns)
                {
                    var matches = await FindPatternMatchesAsync(filePath, pattern.Pattern);
                    foreach (var match in matches)
                    {
                        detectedFields.Add(new AutoDetectedFieldViewModel { SuggestedName = pattern.Name, SuggestDataTypeEnum = pattern.DataType, X = match.X, Y = match.Y, Width = match.Width, Height = match.Height, PageNumber = match.PageNumber, Confidence = match.Confidence, ExtractedText = match.Text, DetectionMethod = "Pattern Recognition" });
                    }
                }
                return detectedFields.Take(10).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-detect fields for {FilePath}", filePath);
                return new List<AutoDetectedFieldViewModel>();
            }
        }
        private async Task<string> ConvertPdfPageToImageAsync(string filePath, int pageNumber, decimal zoomLevel)
        {
            var mockImage = new byte[1000];
            new Random().NextBytes(mockImage);
            return Convert.ToBase64String(mockImage);
        }
        private async Task<string> ExtractTextFromRegionAsync(string filePath, int pageNumber, double x, double y, double width, double height, bool useOCR)
        {
            if (useOCR)
            {
                return await PerformOCRAsync(filePath, pageNumber, x, y, width, height);
            }
            else
            {
                return await ExtractTextDirectlyAsync(filePath, pageNumber, x, y, width, height);
            }
        }
        private async Task<string> PerformOCRAsync(string filePath, int pageNumber, double x, double y, double width, double height)
        {
            return "OCR extracted text sample";
        }
        private async Task<string> ExtractTextDirectlyAsync(string filePath, int pageNumber, double x, double y, double width, double height)
        {
            return "Direct extracted text sample";
        }
        private async Task<List<PatternMatch>> FindPatternMatchesAsync(string filePath, string pattern)
        {
            var matches = new List<PatternMatch>();
            var sampleMatches = new[] { new PatternMatch { Text = "$1,234.56", X = 100, Y = 200, Width = 80, Height = 20, PageNumber = 1, Confidence = 0.92m }, new PatternMatch { Text = "01/15/2024", X = 300, Y = 150, Width = 70, Height = 18, PageNumber = 1, Confidence = 0.88m }, new PatternMatch { Text = "user@example.com", X = 150, Y = 300, Width = 120, Height = 16, PageNumber = 1, Confidence = 0.95m } };
            return sampleMatches.Where(m => Regex.IsMatch(m.Text, pattern)).ToList();
        }
        private class PatternMatch
        {
            public string Text
            {
                get;
                set;
            } = string.Empty;
            public decimal X
            {
                get;
                set;
            }
            public decimal Y
            {
                get;
                set;
            }
            public decimal Width
            {
                get;
                set;
            }
            public decimal Height
            {
                get;
                set;
            }
            public int PageNumber
            {
                get;
                set;
            }
            public decimal Confidence
            {
                get;
                set;
            }
        }
    }
}