using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DT_PODSystem.Data;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.Entities;
using DT_PODSystem.Models.Enums;
using DT_PODSystem.Models.ViewModels;
using DT_PODSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Services.Implementation
{
    /// <summary>
    /// POD Service Implementation - Basic CRUD operations for POD management
    /// </summary>
    public class PODService : IPODService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PODService> _logger;

        public PODService(ApplicationDbContext context, ILogger<PODService> logger)
        {
            _context = context;
            _logger = logger;
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
        /// Update existing POD
        /// </summary>
        public async Task<bool> UpdatePODAsync(int id, PODUpdateDto podData)
        {
            try
            {
                _logger.LogInformation("Updating POD with ID: {PODId}", id);

                var pod = await _context.PODs.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
                if (pod == null)
                {
                    _logger.LogWarning("POD not found for update with ID: {PODId}", id);
                    return false;
                }

                // Validate the data
                var validationResult = await ValidatePODAsync(new PODCreationDto
                {
                    Name = podData.Name,
                    Description = podData.Description,
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
                    ProcessingPriority = podData.ProcessingPriority
                }, id);

                if (!validationResult.IsValid)
                {
                    throw new ArgumentException($"POD validation failed: {string.Join(", ", validationResult.Errors)}");
                }

                // Update the POD properties
                pod.Name = podData.Name;
                pod.Description = podData.Description;
                pod.PONumber = podData.PONumber;
                pod.ContractNumber = podData.ContractNumber;
                pod.CategoryId = podData.CategoryId;
                pod.DepartmentId = podData.DepartmentId;
                pod.VendorId = podData.VendorId;
                pod.AutomationStatus = podData.AutomationStatus;
                pod.Frequency = podData.Frequency;
                pod.VendorSPOCUsername = podData.VendorSPOCUsername;
                pod.GovernorSPOCUsername = podData.GovernorSPOCUsername;
                pod.FinanceSPOCUsername = podData.FinanceSPOCUsername;
                pod.RequiresApproval = podData.RequiresApproval;
                pod.IsFinancialData = podData.IsFinancialData;
                pod.ProcessingPriority = podData.ProcessingPriority;
                pod.ModifiedDate = DateTime.UtcNow;
                pod.ModifiedBy = "System"; // TODO: Get from current user context

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated POD with ID: {PODId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating POD with ID: {PODId}", id);
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