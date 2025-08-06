using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DT_PODSystem.Data;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.Entities;
using DT_PODSystem.Models.ViewModels;
using DT_PODSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Services.Implementation
{
    /// <summary>
    /// LookupsService - Updated for POD Architecture
    /// Now checks POD usage instead of direct template relationships
    /// </summary>
    public class LookupsService : ILookupsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<LookupsService> _logger;

        // Cache keys
        private const string CACHE_KEY_CATEGORIES = "lookups_categories";
        private const string CACHE_KEY_VENDORS = "lookups_vendors";
        private const string CACHE_KEY_DEPARTMENTS = "lookups_departments";
        private const string CACHE_KEY_GENERAL_DIRECTORATES = "lookups_general_directorates";
        private const string CACHE_KEY_ORGANIZATIONAL_HIERARCHY = "lookups_organizational_hierarchy";
        private const int CACHE_EXPIRY_MINUTES = 30;

        public LookupsService(ApplicationDbContext context, IMemoryCache cache, ILogger<LookupsService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        #region Category Operations

        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _cache.GetOrCreateAsync(CACHE_KEY_CATEGORIES, async entry =>
            {
                entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));
                _logger.LogDebug("Loading categories from database");
                return await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
            }) ?? new List<Category>();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _context.Categories.FindAsync(id);
        }

        public async Task<ServiceResult<Category>> CreateCategoryAsync(Category category)
        {
            try
            {
                // Validate uniqueness
                var existingByName = await _context.Categories
                    .AnyAsync(c => c.Name == category.Name && c.IsActive);
                if (existingByName)
                {
                    return ServiceResult<Category>.ErrorResult("Category name already exists");
                }

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                // Clear cache
                _cache.Remove(CACHE_KEY_CATEGORIES);
                _cache.Remove(CACHE_KEY_ORGANIZATIONAL_HIERARCHY);

                _logger.LogInformation("Category created: {CategoryId} - {CategoryName}", category.Id, category.Name);
                return ServiceResult<Category>.SuccessResult(category, "Category created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category: {CategoryName}", category.Name);
                return ServiceResult<Category>.ErrorResult("Failed to create category");
            }
        }

        public async Task<ServiceResult<Category>> UpdateCategoryAsync(int id, CategoryDto categoryDto)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return ServiceResult<Category>.ErrorResult("Category not found");
                }

                // Validate uniqueness (excluding current record)
                var existingByName = await _context.Categories
                    .AnyAsync(c => c.Name == categoryDto.Name && c.Id != id && c.IsActive);
                if (existingByName)
                {
                    return ServiceResult<Category>.ErrorResult("Category name already exists");
                }

                // Update properties
                category.Name = categoryDto.Name;
                category.Description = categoryDto.Description;
                category.DisplayOrder = categoryDto.DisplayOrder;
                category.ModifiedDate = DateTime.UtcNow;
                category.ModifiedBy = "System"; // Should be from current user context

                await _context.SaveChangesAsync();

                // Clear cache
                _cache.Remove(CACHE_KEY_CATEGORIES);
                _cache.Remove(CACHE_KEY_ORGANIZATIONAL_HIERARCHY);

                _logger.LogInformation("Category updated: {CategoryId} - {CategoryName}", id, category.Name);
                return ServiceResult<Category>.SuccessResult(category, "Category updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category: {CategoryId}", id);
                return ServiceResult<Category>.ErrorResult("Failed to update category");
            }
        }

        public async Task<ServiceResult<bool>> DeleteCategoryAsync(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return ServiceResult<bool>.ErrorResult("Category not found");
                }

                // ✅ UPDATED: Check if category is used in PODs (not templates directly)
                var podsCount = await _context.PODs
                    .CountAsync(p => p.CategoryId == id && p.IsActive);

                if (podsCount > 0)
                {
                    return ServiceResult<bool>.ErrorResult($"Cannot delete category. It is used in {podsCount} POD(s)");
                }

                // Soft delete
                category.IsActive = false;
                category.ModifiedDate = DateTime.UtcNow;
                category.ModifiedBy = "System";

                await _context.SaveChangesAsync();

                // Clear cache
                _cache.Remove(CACHE_KEY_CATEGORIES);
                _cache.Remove(CACHE_KEY_ORGANIZATIONAL_HIERARCHY);

                _logger.LogInformation("Category deleted: {CategoryId} - {CategoryName}", id, category.Name);
                return ServiceResult<bool>.SuccessResult(true, "Category deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category: {CategoryId}", id);
                return ServiceResult<bool>.ErrorResult("Failed to delete category");
            }
        }

        #endregion

        #region Vendor Operations

        public async Task<List<Vendor>> GetVendorsAsync()
        {
            return await _cache.GetOrCreateAsync(CACHE_KEY_VENDORS, async entry =>
            {
                entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));
                _logger.LogDebug("Loading vendors from database");
                return await _context.Vendors
                    .Where(v => v.IsActive)
                    .OrderBy(v => v.Name)
                    .ToListAsync();
            }) ?? new List<Vendor>();
        }

        public async Task<Vendor?> GetVendorByIdAsync(int id)
        {
            return await _context.Vendors.FindAsync(id);
        }

        public async Task<ServiceResult<Vendor>> CreateVendorAsync(Vendor vendor)
        {
            try
            {
                // Validate uniqueness
                var existingByName = await _context.Vendors
                    .AnyAsync(v => v.Name == vendor.Name && v.IsActive);
                if (existingByName)
                {
                    return ServiceResult<Vendor>.ErrorResult("Vendor name already exists");
                }

                _context.Vendors.Add(vendor);
                await _context.SaveChangesAsync();

                // Clear cache
                _cache.Remove(CACHE_KEY_VENDORS);
                _cache.Remove(CACHE_KEY_ORGANIZATIONAL_HIERARCHY);

                _logger.LogInformation("Vendor created: {VendorId} - {VendorName}", vendor.Id, vendor.Name);
                return ServiceResult<Vendor>.SuccessResult(vendor, "Vendor created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vendor: {VendorName}", vendor.Name);
                return ServiceResult<Vendor>.ErrorResult("Failed to create vendor");
            }
        }

        public async Task<ServiceResult<Vendor>> UpdateVendorAsync(int id, VendorDto vendorDto)
        {
            try
            {
                var vendor = await _context.Vendors.FindAsync(id);
                if (vendor == null)
                {
                    return ServiceResult<Vendor>.ErrorResult("Vendor not found");
                }

                var existingByName = await _context.Vendors
                    .AnyAsync(v => v.Name == vendorDto.Name && v.Id != id && v.IsActive);
                if (existingByName)
                {
                    return ServiceResult<Vendor>.ErrorResult("Vendor name already exists");
                }

                // Update properties
                vendor.Name = vendorDto.Name;
                vendor.CompanyName = vendorDto.CompanyName;
                vendor.ContactPerson = vendorDto.ContactPerson;
                vendor.ContactEmail = vendorDto.ContactEmail;
                vendor.ContactPhone = vendorDto.ContactPhone;
                vendor.TaxNumber = vendorDto.TaxNumber;
                vendor.CommercialRegister = vendorDto.CommercialRegister;
                vendor.IsApproved = vendorDto.IsApproved;
                vendor.ModifiedDate = DateTime.UtcNow;
                vendor.ModifiedBy = "System";

                await _context.SaveChangesAsync();

                // Clear cache
                _cache.Remove(CACHE_KEY_VENDORS);
                _cache.Remove(CACHE_KEY_ORGANIZATIONAL_HIERARCHY);

                _logger.LogInformation("Vendor updated: {VendorId} - {VendorName}", id, vendor.Name);
                return ServiceResult<Vendor>.SuccessResult(vendor, "Vendor updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vendor: {VendorId}", id);
                return ServiceResult<Vendor>.ErrorResult("Failed to update vendor");
            }
        }

        public async Task<ServiceResult<bool>> DeleteVendorAsync(int id)
        {
            try
            {
                var vendor = await _context.Vendors.FindAsync(id);
                if (vendor == null)
                {
                    return ServiceResult<bool>.ErrorResult("Vendor not found");
                }

                // ✅ UPDATED: Check if vendor is used in PODs (not templates directly)
                var podsCount = await _context.PODs
                    .CountAsync(p => p.VendorId == id && p.IsActive);

                if (podsCount > 0)
                {
                    return ServiceResult<bool>.ErrorResult($"Cannot delete vendor. It is used in {podsCount} POD(s)");
                }

                // Soft delete
                vendor.IsActive = false;
                vendor.ModifiedDate = DateTime.UtcNow;
                vendor.ModifiedBy = "System";

                await _context.SaveChangesAsync();

                // Clear cache
                _cache.Remove(CACHE_KEY_VENDORS);
                _cache.Remove(CACHE_KEY_ORGANIZATIONAL_HIERARCHY);

                _logger.LogInformation("Vendor deleted: {VendorId} - {VendorName}", id, vendor.Name);
                return ServiceResult<bool>.SuccessResult(true, "Vendor deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vendor: {VendorId}", id);
                return ServiceResult<bool>.ErrorResult("Failed to delete vendor");
            }
        }

        #endregion

        #region Department Operations

        public async Task<List<Department>> GetDepartmentsAsync()
        {
            return await _cache.GetOrCreateAsync(CACHE_KEY_DEPARTMENTS, async entry =>
            {
                entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));
                _logger.LogDebug("Loading departments from database");
                return await _context.Departments
                    .Include(d => d.GeneralDirectorate)
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.GeneralDirectorate.Name)
                    .ThenBy(d => d.DisplayOrder)
                    .ThenBy(d => d.Name)
                    .ToListAsync();
            }) ?? new List<Department>();
        }

        public async Task<Department?> GetDepartmentByIdAsync(int id)
        {
            return await _context.Departments
                .Include(d => d.GeneralDirectorate)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<ServiceResult<Department>> CreateDepartmentAsync(Department department)
        {
            try
            {
                // Validate General Directorate exists
                var gdExists = await _context.GeneralDirectorates
                    .AnyAsync(g => g.Id == department.GeneralDirectorateId && g.IsActive);
                if (!gdExists)
                {
                    return ServiceResult<Department>.ErrorResult("General Directorate not found");
                }

                var existingByName = await _context.Departments
                    .AnyAsync(d => d.Name == department.Name &&
                                  d.GeneralDirectorateId == department.GeneralDirectorateId &&
                                  d.IsActive);
                if (existingByName)
                {
                    return ServiceResult<Department>.ErrorResult("Department name already exists in this General Directorate");
                }

                _context.Departments.Add(department);
                await _context.SaveChangesAsync();

                // Clear caches
                _cache.Remove(CACHE_KEY_DEPARTMENTS);
                _cache.Remove(CACHE_KEY_ORGANIZATIONAL_HIERARCHY);

                // Load with navigation property for return
                var createdDepartment = await _context.Departments
                    .Include(d => d.GeneralDirectorate)
                    .FirstAsync(d => d.Id == department.Id);

                _logger.LogInformation("Department created: {DepartmentId} - {DepartmentName}", department.Id, department.Name);
                return ServiceResult<Department>.SuccessResult(createdDepartment, "Department created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating department: {DepartmentName}", department.Name);
                return ServiceResult<Department>.ErrorResult("Failed to create department");
            }
        }

        public async Task<ServiceResult<Department>> UpdateDepartmentAsync(int id, DepartmentDto departmentDto)
        {
            try
            {
                var department = await _context.Departments
                    .Include(d => d.GeneralDirectorate)
                    .FirstOrDefaultAsync(d => d.Id == id);
                if (department == null)
                {
                    return ServiceResult<Department>.ErrorResult("Department not found");
                }

                // Validate General Directorate exists
                var gdExists = await _context.GeneralDirectorates
                    .AnyAsync(g => g.Id == departmentDto.GeneralDirectorateId && g.IsActive);
                if (!gdExists)
                {
                    return ServiceResult<Department>.ErrorResult("General Directorate not found");
                }

                var existingByName = await _context.Departments
                    .AnyAsync(d => d.Name == departmentDto.Name &&
                                  d.GeneralDirectorateId == departmentDto.GeneralDirectorateId &&
                                  d.Id != id && d.IsActive);
                if (existingByName)
                {
                    return ServiceResult<Department>.ErrorResult("Department name already exists in this General Directorate");
                }

                // Update properties
                department.Name = departmentDto.Name;
                department.Description = departmentDto.Description;
                department.GeneralDirectorateId = departmentDto.GeneralDirectorateId;
                department.ManagerName = departmentDto.ManagerName;
                department.ContactEmail = departmentDto.ContactEmail;
                department.ContactPhone = departmentDto.ContactPhone;
                department.DisplayOrder = departmentDto.DisplayOrder;
                department.ModifiedDate = DateTime.UtcNow;
                department.ModifiedBy = "System";

                await _context.SaveChangesAsync();

                // Clear caches
                _cache.Remove(CACHE_KEY_DEPARTMENTS);
                _cache.Remove(CACHE_KEY_ORGANIZATIONAL_HIERARCHY);

                // Reload with navigation property
                await _context.Entry(department).Reference(d => d.GeneralDirectorate).LoadAsync();

                _logger.LogInformation("Department updated: {DepartmentId} - {DepartmentName}", id, department.Name);
                return ServiceResult<Department>.SuccessResult(department, "Department updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating department: {DepartmentId}", id);
                return ServiceResult<Department>.ErrorResult("Failed to update department");
            }
        }

        public async Task<ServiceResult<bool>> DeleteDepartmentAsync(int id)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);
                if (department == null)
                {
                    return ServiceResult<bool>.ErrorResult("Department not found");
                }

                // ✅ UPDATED: Check if department is used in PODs (not templates directly)
                var podsCount = await _context.PODs
                    .CountAsync(p => p.DepartmentId == id && p.IsActive);

                if (podsCount > 0)
                {
                    return ServiceResult<bool>.ErrorResult($"Cannot delete department. It is used in {podsCount} POD(s)");
                }

                // Soft delete
                department.IsActive = false;
                department.ModifiedDate = DateTime.UtcNow;
                department.ModifiedBy = "System";

                await _context.SaveChangesAsync();

                // Clear caches
                _cache.Remove(CACHE_KEY_DEPARTMENTS);
                _cache.Remove(CACHE_KEY_ORGANIZATIONAL_HIERARCHY);

                _logger.LogInformation("Department deleted: {DepartmentId} - {DepartmentName}", id, department.Name);
                return ServiceResult<bool>.SuccessResult(true, "Department deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting department: {DepartmentId}", id);
                return ServiceResult<bool>.ErrorResult("Failed to delete department");
            }
        }

        #endregion

        #region General Directorate Operations

        public async Task<List<GeneralDirectorate>> GetGeneralDirectoratesAsync()
        {
            return await _cache.GetOrCreateAsync(CACHE_KEY_GENERAL_DIRECTORATES, async entry =>
            {
                entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));
                _logger.LogDebug("Loading general directorates from database");
                return await _context.GeneralDirectorates
                    .Where(g => g.IsActive)
                    .OrderBy(g => g.DisplayOrder)
                    .ThenBy(g => g.Name)
                    .ToListAsync();
            }) ?? new List<GeneralDirectorate>();
        }

        public async Task<GeneralDirectorate?> GetGeneralDirectorateByIdAsync(int id)
        {
            return await _context.GeneralDirectorates.FindAsync(id);
        }

        public async Task<ServiceResult<GeneralDirectorate>> CreateGeneralDirectorateAsync(GeneralDirectorate gd)
        {
            try
            {
                var existingByName = await _context.GeneralDirectorates
                    .AnyAsync(g => g.Name == gd.Name && g.IsActive);
                if (existingByName)
                {
                    return ServiceResult<GeneralDirectorate>.ErrorResult("General Directorate name already exists");
                }

                _context.GeneralDirectorates.Add(gd);
                await _context.SaveChangesAsync();

                // Clear caches
                _cache.Remove(CACHE_KEY_GENERAL_DIRECTORATES);
                _cache.Remove(CACHE_KEY_ORGANIZATIONAL_HIERARCHY);

                _logger.LogInformation("General Directorate created: {GDId} - {GDName}", gd.Id, gd.Name);
                return ServiceResult<GeneralDirectorate>.SuccessResult(gd, "General Directorate created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating general directorate: {GDName}", gd.Name);
                return ServiceResult<GeneralDirectorate>.ErrorResult("Failed to create general directorate");
            }
        }

        public async Task<ServiceResult<GeneralDirectorate>> UpdateGeneralDirectorateAsync(int id, GeneralDirectorateDto gdDto)
        {
            try
            {
                var gd = await _context.GeneralDirectorates.FindAsync(id);
                if (gd == null)
                {
                    return ServiceResult<GeneralDirectorate>.ErrorResult("General Directorate not found");
                }

                var existingByName = await _context.GeneralDirectorates
                    .AnyAsync(g => g.Name == gdDto.Name && g.Id != id && g.IsActive);
                if (existingByName)
                {
                    return ServiceResult<GeneralDirectorate>.ErrorResult("General Directorate name already exists");
                }

                // Update properties
                gd.Name = gdDto.Name;
                gd.Description = gdDto.Description;
                gd.ManagerName = gdDto.ManagerName;
                gd.ContactEmail = gdDto.ContactEmail;
                gd.ContactPhone = gdDto.ContactPhone;
                gd.DisplayOrder = gdDto.DisplayOrder;
                gd.ModifiedDate = DateTime.UtcNow;
                gd.ModifiedBy = "System";

                await _context.SaveChangesAsync();

                // Clear caches
                _cache.Remove(CACHE_KEY_GENERAL_DIRECTORATES);
                _cache.Remove(CACHE_KEY_ORGANIZATIONAL_HIERARCHY);

                _logger.LogInformation("General Directorate updated: {GDId} - {GDName}", id, gd.Name);
                return ServiceResult<GeneralDirectorate>.SuccessResult(gd, "General Directorate updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating general directorate: {GDId}", id);
                return ServiceResult<GeneralDirectorate>.ErrorResult("Failed to update general directorate");
            }
        }

        public async Task<ServiceResult<bool>> DeleteGeneralDirectorateAsync(int id)
        {
            try
            {
                var gd = await _context.GeneralDirectorates.FindAsync(id);
                if (gd == null)
                {
                    return ServiceResult<bool>.ErrorResult("General Directorate not found");
                }

                // Check if GD has departments
                var departmentsCount = await _context.Departments
                    .CountAsync(d => d.GeneralDirectorateId == id && d.IsActive);

                if (departmentsCount > 0)
                {
                    return ServiceResult<bool>.ErrorResult($"Cannot delete General Directorate. It has {departmentsCount} department(s)");
                }

                // Soft delete
                gd.IsActive = false;
                gd.ModifiedDate = DateTime.UtcNow;
                gd.ModifiedBy = "System";

                await _context.SaveChangesAsync();

                // Clear caches
                _cache.Remove(CACHE_KEY_GENERAL_DIRECTORATES);
                _cache.Remove(CACHE_KEY_ORGANIZATIONAL_HIERARCHY);

                _logger.LogInformation("General Directorate deleted: {GDId} - {GDName}", id, gd.Name);
                return ServiceResult<bool>.SuccessResult(true, "General Directorate deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting general directorate: {GDId}", id);
                return ServiceResult<bool>.ErrorResult("Failed to delete general directorate");
            }
        }

        #endregion

        #region Combined Operations

        public async Task<OrganizationalHierarchyDto> GetOrganizationalHierarchyAsync()
        {
            return await _cache.GetOrCreateAsync(CACHE_KEY_ORGANIZATIONAL_HIERARCHY, async entry =>
            {
                entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));
                _logger.LogDebug("Loading organizational hierarchy from database");

                var categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();

                var generalDirectorates = await _context.GeneralDirectorates
                    .Where(g => g.IsActive)
                    .OrderBy(g => g.DisplayOrder)
                    .ThenBy(g => g.Name)
                    .ToListAsync();

                var departments = await _context.Departments
                    .Include(d => d.GeneralDirectorate)
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.GeneralDirectorate.Name)
                    .ThenBy(d => d.DisplayOrder)
                    .ThenBy(d => d.Name)
                    .ToListAsync();

                var vendors = await _context.Vendors
                    .Where(v => v.IsActive)
                    .OrderBy(v => v.Name)
                    .ToListAsync();

                return new OrganizationalHierarchyDto
                {
                    Categories = categories.Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        DisplayOrder = c.DisplayOrder,
                        IsActive = c.IsActive
                    }).ToList(),
                    GeneralDirectorates = generalDirectorates.Select(g => new GeneralDirectorateDto
                    {
                        Id = g.Id,
                        Name = g.Name,
                        Description = g.Description,
                        ManagerName = g.ManagerName,
                        ContactEmail = g.ContactEmail,
                        ContactPhone = g.ContactPhone,
                        DisplayOrder = g.DisplayOrder,
                        IsActive = g.IsActive
                    }).ToList(),
                    Departments = departments.Select(d => new DepartmentDto
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Description = d.Description,
                        GeneralDirectorateId = d.GeneralDirectorateId,
                        GeneralDirectorateName = d.GeneralDirectorate.Name,
                        ManagerName = d.ManagerName,
                        ContactEmail = d.ContactEmail,
                        ContactPhone = d.ContactPhone,
                        DisplayOrder = d.DisplayOrder,
                        IsActive = d.IsActive
                    }).ToList(),
                    Vendors = vendors.Select(v => new VendorDto
                    {
                        Id = v.Id,
                        Name = v.Name,
                        CompanyName = v.CompanyName,
                        ContactPerson = v.ContactPerson,
                        ContactEmail = v.ContactEmail,
                        ContactPhone = v.ContactPhone,
                        TaxNumber = v.TaxNumber,
                        CommercialRegister = v.CommercialRegister,
                        IsApproved = v.IsApproved,
                        IsActive = v.IsActive
                    }).ToList()
                };
            }) ?? new OrganizationalHierarchyDto();
        }

        #endregion

        #region Utility Operations

        public async Task<bool> IsNameUniqueAsync(string entityType, string name, int? excludeId = null)
        {
            try
            {
                switch (entityType.ToLower())
                {
                    case "category":
                        return !await _context.Categories
                            .AnyAsync(c => c.Name == name && c.Id != (excludeId ?? 0) && c.IsActive);

                    case "vendor":
                        return !await _context.Vendors
                            .AnyAsync(v => v.Name == name && v.Id != (excludeId ?? 0) && v.IsActive);

                    case "department":
                        return !await _context.Departments
                            .AnyAsync(d => d.Name == name && d.Id != (excludeId ?? 0) && d.IsActive);

                    case "generaldirectorate":
                        return !await _context.GeneralDirectorates
                            .AnyAsync(g => g.Name == name && g.Id != (excludeId ?? 0) && g.IsActive);

                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating name uniqueness for {EntityType}", entityType);
                return false;
            }
        }

        // ✅ UPDATED: GetUsageDetailsAsync now checks POD usage instead of direct template usage
        public async Task<LookupUsageDetailsDto> GetUsageDetailsAsync(string entityType, int id)
        {
            try
            {
                _logger.LogDebug("Getting usage details for {EntityType} {Id}", entityType, id);

                var usageDetails = new LookupUsageDetailsDto
                {
                    LookupType = entityType,
                    LookupId = id,
                    IsInUse = false,
                    CanBeDeleted = true,
                    TotalUsageCount = 0,
                    Dependencies = new List<string>(),
                    UsageBreakdown = new Dictionary<string, int>()
                };

                switch (entityType.ToLower())
                {
                    case "category":
                        var category = await _context.Categories.FindAsync(id);
                        if (category != null)
                        {
                            usageDetails.LookupName = category.Name;

                            // ✅ UPDATED: Check PODs instead of templates directly
                            var podsCount = await _context.PODs.CountAsync(p => p.CategoryId == id && p.IsActive);
                            var templatesCount = await _context.PODs
                                .Where(p => p.CategoryId == id && p.IsActive)
                                .SelectMany(p => p.Templates)
                                .CountAsync(t => t.IsActive);

                            usageDetails.TotalUsageCount = podsCount;
                            usageDetails.UsageBreakdown["PODs"] = podsCount;
                            usageDetails.UsageBreakdown["Templates"] = templatesCount;

                            if (podsCount > 0)
                            {
                                usageDetails.IsInUse = true;
                                usageDetails.CanBeDeleted = false;
                                usageDetails.Dependencies.Add($"{podsCount} POD(s) with {templatesCount} template(s)");
                            }
                        }
                        break;

                    case "vendor":
                        var vendor = await _context.Vendors.FindAsync(id);
                        if (vendor != null)
                        {
                            usageDetails.LookupName = vendor.Name;

                            // ✅ UPDATED: Check PODs instead of templates directly
                            var podsCount = await _context.PODs.CountAsync(p => p.VendorId == id && p.IsActive);
                            var templatesCount = await _context.PODs
                                .Where(p => p.VendorId == id && p.IsActive)
                                .SelectMany(p => p.Templates)
                                .CountAsync(t => t.IsActive);

                            usageDetails.TotalUsageCount = podsCount;
                            usageDetails.UsageBreakdown["PODs"] = podsCount;
                            usageDetails.UsageBreakdown["Templates"] = templatesCount;

                            if (podsCount > 0)
                            {
                                usageDetails.IsInUse = true;
                                usageDetails.CanBeDeleted = false;
                                usageDetails.Dependencies.Add($"{podsCount} POD(s) with {templatesCount} template(s)");
                            }
                        }
                        break;

                    case "department":
                        var department = await _context.Departments.FindAsync(id);
                        if (department != null)
                        {
                            usageDetails.LookupName = department.Name;

                            // ✅ UPDATED: Check PODs instead of templates directly
                            var podsCount = await _context.PODs.CountAsync(p => p.DepartmentId == id && p.IsActive);
                            var templatesCount = await _context.PODs
                                .Where(p => p.DepartmentId == id && p.IsActive)
                                .SelectMany(p => p.Templates)
                                .CountAsync(t => t.IsActive);

                            usageDetails.TotalUsageCount = podsCount;
                            usageDetails.UsageBreakdown["PODs"] = podsCount;
                            usageDetails.UsageBreakdown["Templates"] = templatesCount;

                            if (podsCount > 0)
                            {
                                usageDetails.IsInUse = true;
                                usageDetails.CanBeDeleted = false;
                                usageDetails.Dependencies.Add($"{podsCount} POD(s) with {templatesCount} template(s)");
                            }
                        }
                        break;

                    case "generaldirectorate":
                        var gd = await _context.GeneralDirectorates.FindAsync(id);
                        if (gd != null)
                        {
                            usageDetails.LookupName = gd.Name;

                            // Check departments first (direct relationship)
                            var departmentCount = await _context.Departments.CountAsync(d => d.GeneralDirectorateId == id && d.IsActive);

                            // ✅ UPDATED: Also check PODs through departments
                            var podsCount = await _context.PODs
                                .Include(p => p.Department)
                                .CountAsync(p => p.Department.GeneralDirectorateId == id && p.IsActive);

                            usageDetails.TotalUsageCount = departmentCount + podsCount;
                            usageDetails.UsageBreakdown["Departments"] = departmentCount;
                            usageDetails.UsageBreakdown["PODs"] = podsCount;

                            if (departmentCount > 0 || podsCount > 0)
                            {
                                usageDetails.IsInUse = true;
                                usageDetails.CanBeDeleted = false;
                                var dependencies = new List<string>();
                                if (departmentCount > 0) dependencies.Add($"{departmentCount} department(s)");
                                if (podsCount > 0) dependencies.Add($"{podsCount} POD(s)");
                                usageDetails.Dependencies.AddRange(dependencies);
                            }
                        }
                        break;
                }

                return usageDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage details for {EntityType} {Id}", entityType, id);
                return new LookupUsageDetailsDto
                {
                    LookupType = entityType,
                    LookupId = id,
                    IsInUse = false,
                    CanBeDeleted = false,
                    TotalUsageCount = 0,
                    Dependencies = new List<string> { "Error checking usage" },
                    UsageBreakdown = new Dictionary<string, int>()
                };
            }
        }

        public async Task<ServiceResult<bool>> ToggleStatusAsync(string entityType, int id)
        {
            try
            {
                switch (entityType.ToLower())
                {
                    case "category":
                        var category = await _context.Categories.FindAsync(id);
                        if (category == null) return ServiceResult<bool>.ErrorResult("Category not found");
                        category.IsActive = !category.IsActive;
                        category.ModifiedDate = DateTime.UtcNow;
                        category.ModifiedBy = "System";
                        _cache.Remove(CACHE_KEY_CATEGORIES);
                        _cache.Remove(CACHE_KEY_ORGANIZATIONAL_HIERARCHY);
                        break;

                    case "vendor":
                        var vendor = await _context.Vendors.FindAsync(id);
                        if (vendor == null) return ServiceResult<bool>.ErrorResult("Vendor not found");
                        vendor.IsActive = !vendor.IsActive;
                        vendor.ModifiedDate = DateTime.UtcNow;
                        vendor.ModifiedBy = "System";
                        _cache.Remove(CACHE_KEY_VENDORS);
                        _cache.Remove(CACHE_KEY_ORGANIZATIONAL_HIERARCHY);
                        break;

                    case "department":
                        var department = await _context.Departments.FindAsync(id);
                        if (department == null) return ServiceResult<bool>.ErrorResult("Department not found");
                        department.IsActive = !department.IsActive;
                        department.ModifiedDate = DateTime.UtcNow;
                        department.ModifiedBy = "System";
                        _cache.Remove(CACHE_KEY_DEPARTMENTS);
                        _cache.Remove(CACHE_KEY_ORGANIZATIONAL_HIERARCHY);
                        break;

                    case "generaldirectorate":
                        var gd = await _context.GeneralDirectorates.FindAsync(id);
                        if (gd == null) return ServiceResult<bool>.ErrorResult("General Directorate not found");
                        gd.IsActive = !gd.IsActive;
                        gd.ModifiedDate = DateTime.UtcNow;
                        gd.ModifiedBy = "System";
                        _cache.Remove(CACHE_KEY_GENERAL_DIRECTORATES);
                        _cache.Remove(CACHE_KEY_ORGANIZATIONAL_HIERARCHY);
                        break;

                    default:
                        return ServiceResult<bool>.ErrorResult("Invalid entity type");
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Status toggled for {EntityType} {Id}", entityType, id);
                return ServiceResult<bool>.SuccessResult(true, "Status updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling status for {EntityType} {Id}", entityType, id);
                return ServiceResult<bool>.ErrorResult("Failed to update status");
            }
        }

        #endregion
    }
}