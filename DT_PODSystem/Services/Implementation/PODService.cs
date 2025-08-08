using DT_PODSystem.Data;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.Entities;
using DT_PODSystem.Models.Enums;
using DT_PODSystem.Models.ViewModels;
using DT_PODSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DT_PODSystem.Services.Implementation
{

    public class PODService : IPODService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PODService> _logger;

        public PODService(ApplicationDbContext context, ILogger<PODService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Update POD - Handles your exact JavaScript data structure
        /// </summary>
        public async Task<bool> UpdatePODAsync(int id, PODUpdateDto podData)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Updating POD with ID: {PODId}", id);

                var pod = await _context.PODs
                    .Include(p => p.Entries)
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

                if (pod == null)
                {
                    _logger.LogWarning("POD not found for update with ID: {PODId}", id);
                    return false;
                }

                // ✅ Update POD basic fields (matching your form field names)
                pod.Name = podData.Name;
                pod.Description = podData.Description;
                pod.PONumber = podData.PoNumber;
                pod.ContractNumber = podData.ContractNumber;

                // Handle nullable integers properly
                if (podData.CategoryId.HasValue && podData.CategoryId.Value > 0)
                    pod.CategoryId = podData.CategoryId.Value;

                if (podData.DepartmentId.HasValue && podData.DepartmentId.Value > 0)
                    pod.DepartmentId = podData.DepartmentId.Value;

                pod.VendorId = podData.VendorId;

                // Handle enum conversions
                if (Enum.TryParse<AutomationStatus>(podData.AutomationStatus, out var autoStatus))
                    pod.AutomationStatus = autoStatus;

                if (Enum.TryParse<ProcessingFrequency>(podData.ProcessingFrequency, out var frequency))
                    pod.Frequency = frequency;

                if (podData.ProcessingPriority.HasValue)
                    pod.ProcessingPriority = podData.ProcessingPriority.Value;

                // SPOC fields
                pod.VendorSPOCUsername = podData.VendorSPOC;
                pod.GovernorSPOCUsername = podData.GovernorSPOC;
                pod.FinanceSPOCUsername = podData.FinanceSPOC;

                // Business rules
                pod.RequiresApproval = podData.RequiresApproval;
                pod.IsFinancialData = podData.ContainsFinancialData;

                pod.ModifiedDate = DateTime.UtcNow;

                // ✅ Handle POD Entries (your JavaScript format)
                await UpdatePODEntriesFromJavaScriptAsync(pod, podData.PodEntries);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("POD updated successfully with ID: {PODId}", id);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating POD with ID: {PODId}", id);
                throw;
            }
        }

        /// <summary>
        /// Handle POD Entries in your JavaScript format:
        /// - Strings for single values: "Test 1"
        /// - Objects for tables: { "Test table": { "Keys": [...], "Target": [...] } }
        /// </summary>
        private async Task UpdatePODEntriesFromJavaScriptAsync(POD pod, List<object> jsEntries)
        {
            try
            {
                _logger.LogInformation("Updating POD entries from JavaScript format for POD ID: {PODId}", pod.Id);

                // Clear existing entries
                var existingEntries = pod.Entries.ToList();
                foreach (var entry in existingEntries)
                {
                    _context.PODEntries.Remove(entry);
                }

                // Process JavaScript entries
                int order = 0;
                foreach (var jsEntry in jsEntries)
                {
                    order++;

                    if (jsEntry is string singleValue)
                    {
                        // Single value entry: "Test 1"
                        var singleEntry = new PODEntry
                        {
                            PODId = pod.Id,
                            EntryType = "single",
                            EntryOrder = order,
                            EntryData = JsonSerializer.Serialize(new { key = singleValue, value = "(input)" }),
                            DisplayName = singleValue,
                            IsActive = true,
                            CreatedBy = "system", // Should come from user context
                            CreatedDate = DateTime.UtcNow
                        };

                        _context.PODEntries.Add(singleEntry);
                        _logger.LogInformation("Created single entry: {Name}", singleValue);
                    }
                    else if (jsEntry is JObject tableObject)
                    {
                        // Table entry: { "Test table": { "Keys": [...], "Target": [...] } }
                        foreach (var tableProperty in tableObject.Properties())
                        {
                            var tableName = tableProperty.Name;
                            var tableData = tableProperty.Value;

                            var tableEntry = new PODEntry
                            {
                                PODId = pod.Id,
                                EntryType = "table",
                                EntryOrder = order,
                                EntryData = tableData.ToString(),
                                DisplayName = tableName,
                                Category = "table",
                                IsActive = true,
                                CreatedBy = "system", // Should come from user context
                                CreatedDate = DateTime.UtcNow
                            };

                            _context.PODEntries.Add(tableEntry);
                            _logger.LogInformation("Created table entry: {Name}", tableName);
                        }
                    }
                    else if (jsEntry != null)
                    {
                        // Handle other object types
                        var jsonString = JsonSerializer.Serialize(jsEntry);
                        var jObject = JObject.Parse(jsonString);

                        foreach (var property in jObject.Properties())
                        {
                            var tableName = property.Name;
                            var tableData = property.Value;

                            var tableEntry = new PODEntry
                            {
                                PODId = pod.Id,
                                EntryType = "table",
                                EntryOrder = order,
                                EntryData = tableData.ToString(),
                                DisplayName = tableName,
                                Category = "table",
                                IsActive = true,
                                CreatedBy = "system",
                                CreatedDate = DateTime.UtcNow
                            };

                            _context.PODEntries.Add(tableEntry);
                            _logger.LogInformation("Created object entry: {Name}", tableName);
                        }
                    }
                }

                _logger.LogInformation("Successfully processed {Count} POD entries", jsEntries.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating POD entries from JavaScript format for POD ID: {PODId}", pod.Id);
                throw;
            }
        }

        /// <summary>
        /// Get POD with entries formatted for your JavaScript
        /// </summary>
        public async Task<object> GetPODForJavaScriptAsync(int id)
        {
            try
            {
                _logger.LogInformation("Getting POD for JavaScript with ID: {PODId}", id);

                var pod = await _context.PODs
                    .Include(p => p.Category)
                    .Include(p => p.Department)
                        .ThenInclude(d => d.GeneralDirectorate)
                    .Include(p => p.Vendor)
                    .Include(p => p.Entries.Where(e => e.IsActive))
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

                if (pod == null)
                {
                    _logger.LogWarning("POD not found with ID: {PODId}", id);
                    return null;
                }

                // Format entries for your JavaScript loadFromJson function
                var jsEntries = new List<object>();
                var orderedEntries = pod.Entries.OrderBy(e => e.EntryOrder).ToList();

                foreach (var entry in orderedEntries)
                {
                    if (entry.EntryType == "single")
                    {
                        // Extract single value name
                        try
                        {
                            var entryData = JsonSerializer.Deserialize<Dictionary<string, object>>(entry.EntryData);
                            if (entryData.ContainsKey("key"))
                            {
                                jsEntries.Add(entryData["key"].ToString());
                            }
                            else
                            {
                                jsEntries.Add(entry.DisplayName ?? "Unknown");
                            }
                        }
                        catch
                        {
                            jsEntries.Add(entry.DisplayName ?? "Unknown");
                        }
                    }
                    else if (entry.EntryType == "table")
                    {
                        // Parse table data
                        try
                        {
                            var tableData = JObject.Parse(entry.EntryData);
                            var tableObject = new Dictionary<string, object>
                            {
                                [entry.DisplayName ?? "Unknown"] = tableData
                            };
                            jsEntries.Add(tableObject);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse table data for entry: {EntryId}", entry.Id);
                        }
                    }
                }

                return new
                {
                    success = true,
                    pod = new
                    {
                        // Basic fields matching your form names
                        name = pod.Name,
                        podCode = pod.PODCode,
                        description = pod.Description,
                        poNumber = pod.PONumber,
                        contractNumber = pod.ContractNumber,
                        categoryId = pod.CategoryId,
                        departmentId = pod.DepartmentId,
                        vendorId = pod.VendorId,
                        automationStatus = pod.AutomationStatus.ToString(),
                        processingFrequency = pod.Frequency.ToString(),
                        processingPriority = pod.ProcessingPriority,
                        vendorSPOC = pod.VendorSPOCUsername,
                        governorSPOC = pod.GovernorSPOCUsername,
                        financeSPOC = pod.FinanceSPOCUsername,
                        requiresApproval = pod.RequiresApproval,
                        containsFinancialData = pod.IsFinancialData,

                        // Additional display data
                        categoryName = pod.Category?.Name,
                        departmentName = pod.Department?.Name,
                        vendorName = pod.Vendor?.Name,

                        // Entries in JavaScript format
                        entries = jsEntries
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting POD for JavaScript with ID: {PODId}", id);
                throw;
            }
        }



        /// <summary>
        /// Update POD Entries - handles create, update, delete operations
        /// </summary>
        private async Task UpdatePODEntriesAsync(POD pod, List<PODEntryUpdateDto> entryDtos)
        {
            try
            {
                _logger.LogInformation("Updating entries for POD ID: {PODId}", pod.Id);

                // Get existing entries
                var existingEntries = pod.Entries.ToList();
                var submittedEntryIds = entryDtos.Where(e => e.Id.HasValue).Select(e => e.Id.Value).ToList();

                // 1. DELETE removed entries
                var entriesToDelete = existingEntries.Where(e => !submittedEntryIds.Contains(e.Id)).ToList();
                foreach (var entryToDelete in entriesToDelete)
                {
                    _context.PODEntries.Remove(entryToDelete);
                    _logger.LogInformation("Deleting POD entry ID: {EntryId}", entryToDelete.Id);
                }

                // 2. UPDATE existing entries
                foreach (var entryDto in entryDtos.Where(e => e.Id.HasValue))
                {
                    var existingEntry = existingEntries.FirstOrDefault(e => e.Id == entryDto.Id.Value);
                    if (existingEntry != null)
                    {
                        existingEntry.EntryType = entryDto.EntryType;
                        existingEntry.EntryOrder = entryDto.EntryOrder;
                        existingEntry.EntryData = entryDto.EntryData;
                        existingEntry.DisplayName = entryDto.DisplayName;
                        existingEntry.Description = entryDto.Description;
                        existingEntry.Category = entryDto.Category;
                        existingEntry.IsRequired = entryDto.IsRequired;
                        existingEntry.IsActive = entryDto.IsActive;
                        existingEntry.ModifiedDate = DateTime.UtcNow;

                        _logger.LogInformation("Updating POD entry ID: {EntryId}", existingEntry.Id);
                    }
                }

                // 3. CREATE new entries
                foreach (var entryDto in entryDtos.Where(e => !e.Id.HasValue))
                {
                    var newEntry = new PODEntry
                    {
                        PODId = pod.Id,
                        EntryType = entryDto.EntryType,
                        EntryOrder = entryDto.EntryOrder,
                        EntryData = entryDto.EntryData,
                        DisplayName = entryDto.DisplayName,
                        Description = entryDto.Description,
                        Category = entryDto.Category,
                        IsRequired = entryDto.IsRequired,
                        IsActive = entryDto.IsActive,
                        CreatedBy = "system", // Should get from user context
                        CreatedDate = DateTime.UtcNow
                    };

                    _context.PODEntries.Add(newEntry);
                    _logger.LogInformation("Creating new POD entry for POD ID: {PODId}", pod.Id);
                }

                _logger.LogInformation("POD entries updated successfully for POD ID: {PODId}", pod.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating POD entries for POD ID: {PODId}", pod.Id);
                throw;
            }
        }

        /// <summary>
        /// Get POD with Entries for editing
        /// </summary>
        public async Task<PODDto?> GetPODWithEntriesAsync(int id)
        {
            try
            {
                _logger.LogInformation("Getting POD with entries for ID: {PODId}", id);

                var pod = await _context.PODs
                    .Include(p => p.Category)
                    .Include(p => p.Department)
                        .ThenInclude(d => d.GeneralDirectorate)
                    .Include(p => p.Vendor)
                    .Include(p => p.Entries.Where(e => e.IsActive))
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

                if (pod == null)
                {
                    _logger.LogWarning("POD not found with ID: {PODId}", id);
                    return null;
                }

                return new PODDto
                {
                    Id = pod.Id,
                    Name = pod.Name,
                    PODCode = pod.PODCode,
                    Description = pod.Description,
                    PONumber = pod.PONumber,
                    ContractNumber = pod.ContractNumber,
                    CategoryId = pod.CategoryId,
                    DepartmentId = pod.DepartmentId,
                    VendorId = pod.VendorId,
                    AutomationStatus = pod.AutomationStatus,
                    Frequency = pod.Frequency,
                    VendorSPOCUsername = pod.VendorSPOCUsername,
                    GovernorSPOCUsername = pod.GovernorSPOCUsername,
                    FinanceSPOCUsername = pod.FinanceSPOCUsername,
                    Status = pod.Status,
                    Version = pod.Version,
                    RequiresApproval = pod.RequiresApproval,
                    IsFinancialData = pod.IsFinancialData,
                    ProcessingPriority = pod.ProcessingPriority,
                    CategoryName = pod.Category?.Name ?? string.Empty,
                    DepartmentName = pod.Department?.Name ?? string.Empty,
                    GeneralDirectorateName = pod.Department?.GeneralDirectorate?.Name ?? string.Empty,
                    VendorName = pod.Vendor?.Name ?? string.Empty,
                    ProcessedCount = pod.ProcessedCount,
                    LastProcessedDate = pod.LastProcessedDate,
                    CreatedDate = pod.CreatedDate,
                    ModifiedDate = pod.ModifiedDate,
                    // ✅ NEW: Include Entries
                    Entries = pod.Entries.Select(e => new PODEntryDto
                    {
                        Id = e.Id,
                        EntryType = e.EntryType,
                        EntryOrder = e.EntryOrder,
                        EntryData = e.EntryData,
                        DisplayName = e.DisplayName,
                        Description = e.Description,
                        Category = e.Category,
                        IsRequired = e.IsRequired,
                        IsActive = e.IsActive
                    }).OrderBy(e => e.EntryOrder).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting POD with entries for ID: {PODId}", id);
                throw;
            }
        }




        #region Basic CRUD Operations

        /// <summary>
        /// Create a new POD with business information
        /// </summary>
        public async Task<POD> CreatePODAsync(PODCreationDto podData)
        {
            try
            {
                _logger.LogInformation("Creating new POD: {Name}", podData.Name);

                // Validate the data
                var validationResult = await ValidatePODAsync(podData);
                if (!validationResult.IsValid)
                {
                    throw new ArgumentException($"POD validation failed: {string.Join(", ", validationResult.Errors)}");
                }

                // Generate unique POD code
                var podCode = await GeneratePODCodeAsync(podData.Name);

                // Create the POD entity
                var pod = new POD
                {
                    Name = podData.Name,
                    Description = podData.Description,
                    PODCode = podCode,
                    PONumber = podData.PONumber,
                    ContractNumber = podData.ContractNumber,
                    CategoryId = podData.CategoryId,
                    DepartmentId = podData.DepartmentId,
                    VendorId = podData.VendorId,
                    AutomationStatus = podData.AutomationStatus,
                    Frequency = podData.Frequency,
                    VendorSPOCUsername = podData.VendorSPOCUsername,
                    GovernorSPOCUsername = podData.GovernorSPOCUsername,
                    FinanceSPOCUsername = podData.FinanceSPOCUsername,
                    RequiresApproval = podData.RequiresApproval,
                    IsFinancialData = podData.IsFinancialData,
                    ProcessingPriority = podData.ProcessingPriority,
                    Status = PODStatus.Draft,
                    Version = "1.0",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System", // TODO: Get from current user context
                    IsActive = true
                };

                _context.PODs.Add(pod);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created POD with ID {PODId} and code {PODCode}", pod.Id, pod.PODCode);
                return pod;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating POD: {Name}", podData.Name);
                throw;
            }
        }

        /// <summary>
        /// Get POD by ID with all related data
        /// </summary>
        public async Task<POD?> GetPODAsync(int id)
        {
            try
            {
                _logger.LogDebug("Getting POD with ID: {PODId}", id);

                var pod = await _context.PODs
                    .Include(p => p.Category)
                    .Include(p => p.Department)
                        .ThenInclude(d => d.GeneralDirectorate)
                    .Include(p => p.Vendor)
                    .Include(p => p.Templates)
                        .ThenInclude(t => t.FieldMappings)
                    .Include(p => p.Attachments)
                        .ThenInclude(a => a.UploadedFile)
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

                if (pod == null)
                {
                    _logger.LogWarning("POD not found with ID: {PODId}", id);
                }

                return pod;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting POD with ID: {PODId}", id);
                throw;
            }
        }



        /// <summary>
        /// Delete POD (soft delete - sets IsActive = false)
        /// </summary>
        public async Task<bool> DeletePODAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting POD with ID: {PODId}", id);

                var pod = await _context.PODs.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
                if (pod == null)
                {
                    _logger.LogWarning("POD not found for deletion with ID: {PODId}", id);
                    return false;
                }

                // Check if POD has active templates
                var hasActiveTemplates = await _context.PdfTemplates
                    .AnyAsync(t => t.PODId == id && t.IsActive && t.Status == TemplateStatus.Active);

                if (hasActiveTemplates)
                {
                    throw new InvalidOperationException("Cannot delete POD with active templates. Please archive or delete templates first.");
                }

                // Soft delete - set IsActive to false
                pod.IsActive = false;
                pod.ModifiedDate = DateTime.UtcNow;
                pod.ModifiedBy = "System"; // TODO: Get from current user context

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted POD with ID: {PODId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting POD with ID: {PODId}", id);
                throw;
            }
        }

        /// <summary>
        /// Get paginated list of PODs with filtering
        /// </summary>
        public async Task<PODListViewModel> GetPODListAsync(PODFiltersViewModel filters)
        {
            try
            {
                _logger.LogDebug("Getting POD list with filters");

                var query = _context.PODs
                    .Include(p => p.Category)
                    .Include(p => p.Department)
                        .ThenInclude(d => d.GeneralDirectorate)
                    .Include(p => p.Vendor)
                    .Where(p => p.IsActive);

                // Apply filters
                if (!string.IsNullOrEmpty(filters.SearchTerm))
                {
                    var searchTerm = filters.SearchTerm.ToLower();
                    query = query.Where(p =>
                        p.Name.ToLower().Contains(searchTerm) ||
                        p.PODCode.ToLower().Contains(searchTerm) ||
                        (p.Description != null && p.Description.ToLower().Contains(searchTerm)) ||
                        (p.PONumber != null && p.PONumber.ToLower().Contains(searchTerm)) ||
                        (p.ContractNumber != null && p.ContractNumber.ToLower().Contains(searchTerm)));
                }

                if (filters.Status.HasValue)
                {
                    query = query.Where(p => p.Status == filters.Status.Value);
                }

                if (filters.AutomationStatus.HasValue)
                {
                    query = query.Where(p => p.AutomationStatus == filters.AutomationStatus.Value);
                }

                if (filters.Frequency.HasValue)
                {
                    query = query.Where(p => p.Frequency == filters.Frequency.Value);
                }

                if (filters.CategoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == filters.CategoryId.Value);
                }

                if (filters.DepartmentId.HasValue)
                {
                    query = query.Where(p => p.DepartmentId == filters.DepartmentId.Value);
                }

                if (filters.VendorId.HasValue)
                {
                    query = query.Where(p => p.VendorId == filters.VendorId.Value);
                }

                if (filters.RequiresApproval.HasValue)
                {
                    query = query.Where(p => p.RequiresApproval == filters.RequiresApproval.Value);
                }

                if (filters.IsFinancialData.HasValue)
                {
                    query = query.Where(p => p.IsFinancialData == filters.IsFinancialData.Value);
                }

                if (filters.CreatedFromDate.HasValue)
                {
                    query = query.Where(p => p.CreatedDate >= filters.CreatedFromDate.Value);
                }

                if (filters.CreatedToDate.HasValue)
                {
                    query = query.Where(p => p.CreatedDate <= filters.CreatedToDate.Value.AddDays(1));
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination and ordering
                var pods = await query
                    .OrderByDescending(p => p.CreatedDate)
                    .Skip((filters.Pagination.CurrentPage - 1) * filters.Pagination.PageSize)
                    .Take(filters.Pagination.PageSize)
                    .Select(p => new PODListItemDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        PODCode = p.PODCode,
                        Description = p.Description,
                        Status = p.Status,
                        AutomationStatus = p.AutomationStatus,
                        Frequency = p.Frequency,
                        CategoryName = p.Category.Name,
                        DepartmentName = p.Department.Name,
                        GeneralDirectorateName = p.Department.GeneralDirectorate.Name,
                        VendorName = p.Vendor != null ? p.Vendor.Name : "No Vendor",
                        TemplateCount = p.Templates.Count(t => t.IsActive),
                        ProcessedCount = p.ProcessedCount,
                        LastProcessedDate = p.LastProcessedDate,
                        CreatedDate = p.CreatedDate,
                        ModifiedDate = p.ModifiedDate,
                        CreatedBy = p.CreatedBy ?? "System",
                        RequiresApproval = p.RequiresApproval,
                        IsFinancialData = p.IsFinancialData,
                        ProcessingPriority = p.ProcessingPriority
                    })
                    .ToListAsync();

                // Load filter options
                await LoadFilterOptionsAsync(filters);

                var viewModel = new PODListViewModel
                {
                    PODs = pods,
                    Filters = filters,
                    Pagination = new PaginationViewModel
                    {
                        CurrentPage = filters.Pagination.CurrentPage,
                        PageSize = filters.Pagination.PageSize,
                        TotalItems = totalCount
                    }
                };

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting POD list");
                throw;
            }
        }

        /// <summary>
        /// Get all PODs for dropdown/selection lists
        /// </summary>
        public async Task<List<PODSelectionDto>> GetPODsForSelectionAsync(bool activeOnly = true)
        {
            try
            {
                _logger.LogDebug("Getting PODs for selection, activeOnly: {ActiveOnly}", activeOnly);

                var query = _context.PODs.AsQueryable();

                if (activeOnly)
                {
                    query = query.Where(p => p.IsActive && p.Status == PODStatus.Active);
                }
                else
                {
                    query = query.Where(p => p.IsActive);
                }

                var pods = await query
                    .OrderBy(p => p.Name)
                    .Select(p => new PODSelectionDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        PODCode = p.PODCode,
                        Status = p.Status,
                        IsActive = p.IsActive
                    })
                    .ToListAsync();

                return pods;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PODs for selection");
                throw;
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate POD data for creation/update
        /// </summary>
        public async Task<PODValidationResult> ValidatePODAsync(PODCreationDto podData, int? existingPodId = null)
        {
            var result = new PODValidationResult { IsValid = true };

            try
            {
                // Check if name is unique
                var nameExists = await _context.PODs
                    .AnyAsync(p => p.Name.ToLower() == podData.Name.ToLower() &&
                                   p.IsActive &&
                                   (existingPodId == null || p.Id != existingPodId));

                if (nameExists)
                {
                    result.Errors.Add("POD name already exists. Please choose a different name.");
                    result.IsValid = false;
                }

                // Check if PO Number is unique (if provided)
                if (!string.IsNullOrEmpty(podData.PONumber))
                {
                    var poNumberExists = await _context.PODs
                        .AnyAsync(p => p.PONumber == podData.PONumber &&
                                       p.IsActive &&
                                       (existingPodId == null || p.Id != existingPodId));

                    if (poNumberExists)
                    {
                        result.Errors.Add("PO Number already exists. Please choose a different PO Number.");
                        result.IsValid = false;
                    }
                }

                // Check if Contract Number is unique (if provided)
                if (!string.IsNullOrEmpty(podData.ContractNumber))
                {
                    var contractNumberExists = await _context.PODs
                        .AnyAsync(p => p.ContractNumber == podData.ContractNumber &&
                                       p.IsActive &&
                                       (existingPodId == null || p.Id != existingPodId));

                    if (contractNumberExists)
                    {
                        result.Errors.Add("Contract Number already exists. Please choose a different Contract Number.");
                        result.IsValid = false;
                    }
                }

                // Validate Category exists
                var categoryExists = await _context.Categories.AnyAsync(c => c.Id == podData.CategoryId && c.IsActive);
                if (!categoryExists)
                {
                    result.Errors.Add("Selected category does not exist or is inactive.");
                    result.IsValid = false;
                }

                // Validate Department exists
                var departmentExists = await _context.Departments.AnyAsync(d => d.Id == podData.DepartmentId && d.IsActive);
                if (!departmentExists)
                {
                    result.Errors.Add("Selected department does not exist or is inactive.");
                    result.IsValid = false;
                }

                // Validate Vendor exists (if provided)
                if (podData.VendorId.HasValue)
                {
                    var vendorExists = await _context.Vendors.AnyAsync(v => v.Id == podData.VendorId.Value && v.IsActive);
                    if (!vendorExists)
                    {
                        result.Errors.Add("Selected vendor does not exist or is inactive.");
                        result.IsValid = false;
                    }
                }

                // Validate processing priority range
                if (podData.ProcessingPriority < 1 || podData.ProcessingPriority > 10)
                {
                    result.Errors.Add("Processing priority must be between 1 and 10.");
                    result.IsValid = false;
                }

                // Warnings for business logic
                if (podData.IsFinancialData && !podData.RequiresApproval)
                {
                    result.Warnings.Add("Financial data PODs typically require approval. Consider enabling approval workflow.");
                }

                if (podData.AutomationStatus == AutomationStatus.FullyAutomated && podData.RequiresApproval)
                {
                    result.Warnings.Add("Fully automated PODs with approval requirements may create processing bottlenecks.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating POD data");
                result.Errors.Add("Validation failed due to system error.");
                result.IsValid = false;
            }

            return result;
        }

        /// <summary>
        /// Generate unique POD code
        /// </summary>
        public async Task<string> GeneratePODCodeAsync(string baseName)
        {
            try
            {
                // Create base code from name (first 3 letters + timestamp)
                var namePrefix = string.Concat(baseName.Take(3)).ToUpper();
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var baseCode = $"{namePrefix}_{timestamp}";

                // Check if code exists (unlikely but possible)
                var counter = 1;
                var podCode = baseCode;

                while (await _context.PODs.AnyAsync(p => p.PODCode == podCode))
                {
                    podCode = $"{baseCode}_{counter:D2}";
                    counter++;
                }

                _logger.LogDebug("Generated POD code: {PODCode}", podCode);
                return podCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating POD code for name: {BaseName}", baseName);
                // Fallback to GUID if generation fails
                return Guid.NewGuid().ToString("N")[..8].ToUpper();
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get POD display name with code
        /// </summary>
        public string GetPODDisplayName(POD pod)
        {
            return $"{pod.Name} ({pod.PODCode})";
        }

        /// <summary>
        /// Load filter options for dropdowns
        /// </summary>
        private async Task LoadFilterOptionsAsync(PODFiltersViewModel filters)
        {
            try
            {
                // Status options
                filters.StatusOptions = Enum.GetValues<PODStatus>()
                    .Select(s => new SelectListItem
                    {
                        Value = ((int)s).ToString(),
                        Text = s.ToString()
                    })
                    .ToList();

                // Automation status options
                filters.AutomationStatusOptions = Enum.GetValues<AutomationStatus>()
                    .Select(s => new SelectListItem
                    {
                        Value = ((int)s).ToString(),
                        Text = s switch
                        {
                            AutomationStatus.PDF => "PDF Processing",
                            AutomationStatus.ManualEntryWorkflow => "Manual Entry + Workflow",
                            AutomationStatus.FullyAutomated => "Fully Automated",
                            _ => s.ToString()
                        }
                    })
                    .ToList();

                // Frequency options
                filters.FrequencyOptions = Enum.GetValues<ProcessingFrequency>()
                    .Select(f => new SelectListItem
                    {
                        Value = ((int)f).ToString(),
                        Text = f switch
                        {
                            ProcessingFrequency.Monthly => "Monthly",
                            ProcessingFrequency.Quarterly => "Quarterly",
                            ProcessingFrequency.HalfYearly => "Half Yearly",
                            ProcessingFrequency.Yearly => "Yearly",
                            _ => f.ToString()
                        }
                    })
                    .ToList();

                // Category options
                filters.CategoryOptions = await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    })
                    .ToListAsync();

                // Department options
                var departments = await _context.Departments
                    .Include(d => d.GeneralDirectorate)
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.GeneralDirectorate.Name)
                    .ThenBy(d => d.Name)
                    .ToListAsync();

                filters.DepartmentOptions = departments
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = $"{d.GeneralDirectorate.Name} - {d.Name}"
                    })
                    .ToList();

                // Vendor options
                filters.VendorOptions = await _context.Vendors
                    .Where(v => v.IsActive)
                    .OrderBy(v => v.Name)
                    .Select(v => new SelectListItem
                    {
                        Value = v.Id.ToString(),
                        Text = v.Name
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading filter options");
                // Don't throw - just leave options empty
            }
        }

        #endregion
    }
}