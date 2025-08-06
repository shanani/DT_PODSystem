using System;
using System.Linq;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Filters;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.Entities;
using DT_PODSystem.Models.ViewModels;
using DT_PODSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Controllers
{
    [Authorize]
    [RequireAdmin]
    public class LookupsController : Controller
    {
        private readonly ILookupsService _lookupsService;
        private readonly ILogger<LookupsController> _logger;

        public LookupsController(ILookupsService lookupsService, ILogger<LookupsController> logger)
        {
            _lookupsService = lookupsService;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDepartment(DepartmentDto departmentDto)
        {
            try
            {
                _logger.LogInformation("Creating department: {@Department}", departmentDto);

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { Field = x.Key, Error = x.Value.Errors.First().ErrorMessage })
                        .ToList();

                    _logger.LogWarning("ModelState validation failed: {@Errors}", errors);
                    var errorMessage = string.Join(", ", errors.Select(e => $"{e.Field}: {e.Error}"));
                    return Json(new { success = false, message = $"Validation failed: {errorMessage}" });
                }

                if (string.IsNullOrWhiteSpace(departmentDto.Name))
                {
                    return Json(new { success = false, message = "Department name is required" });
                }

                if (departmentDto.GeneralDirectorateId <= 0)
                {
                    return Json(new { success = false, message = "General Directorate selection is required" });
                }

                var department = new Department
                {
                    Name = departmentDto.Name.Trim(),
                    Description = departmentDto.Description?.Trim(),
                    GeneralDirectorateId = departmentDto.GeneralDirectorateId,
                    DisplayOrder = departmentDto.DisplayOrder > 0 ? departmentDto.DisplayOrder : 1,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name ?? "System"
                };

                var result = await _lookupsService.CreateDepartmentAsync(department);
                if (result.Success)
                {
                    return Json(new { success = true, message = "Department created successfully" });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating department");
                return Json(new { success = false, message = "An error occurred while creating the department" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDepartment(DepartmentDto departmentDto)
        {
            try
            {
                _logger.LogInformation("Updating department: {@Department}", departmentDto);

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { Field = x.Key, Error = x.Value.Errors.First().ErrorMessage })
                        .ToList();

                    _logger.LogWarning("ModelState validation failed: {@Errors}", errors);
                    var errorMessage = string.Join(", ", errors.Select(e => $"{e.Field}: {e.Error}"));
                    return Json(new { success = false, message = $"Validation failed: {errorMessage}" });
                }

                if (departmentDto.Id <= 0)
                {
                    return Json(new { success = false, message = "Invalid department ID" });
                }

                var result = await _lookupsService.UpdateDepartmentAsync(departmentDto.Id, departmentDto);
                if (result.Success)
                {
                    return Json(new { success = true, message = "Department updated successfully" });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating department {DepartmentId}", departmentDto.Id);
                return Json(new { success = false, message = "An error occurred while updating the department" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGeneralDirectorate(GeneralDirectorateDto gdDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { Field = x.Key, Error = x.Value.Errors.First().ErrorMessage })
                        .ToList();

                    var errorMessage = string.Join(", ", errors.Select(e => $"{e.Field}: {e.Error}"));
                    return Json(new { success = false, message = $"Validation failed: {errorMessage}" });
                }

                var gd = new GeneralDirectorate
                {
                    Name = gdDto.Name.Trim(),
                    Description = gdDto.Description?.Trim(),
                    DisplayOrder = gdDto.DisplayOrder > 0 ? gdDto.DisplayOrder : 1,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name ?? "System"
                };

                var result = await _lookupsService.CreateGeneralDirectorateAsync(gd);
                if (result.Success)
                {
                    return Json(new { success = true, message = "General Directorate created successfully" });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating general directorate");
                return Json(new { success = false, message = "An error occurred while creating the general directorate" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            try
            {
                var result = await _lookupsService.DeleteDepartmentAsync(id);
                if (result.Success)
                {
                    return Json(new { success = true, message = "Department deleted successfully" });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting department {DepartmentId}", id);
                return Json(new { success = false, message = "An error occurred while deleting the department" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string entityType, int id)
        {
            try
            {
                var result = await _lookupsService.ToggleStatusAsync(entityType, id);
                if (result.Success)
                {
                    return Json(new { success = true, message = "Status updated successfully" });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling status for {EntityType} {Id}", entityType, id);
                return Json(new { success = false, message = "An error occurred while updating the status" });
            }
        }

        #region Department CRUD Operations


        [HttpGet]
        public async Task<IActionResult> GetDepartment(int id)
        {
            try
            {
                var department = await _lookupsService.GetDepartmentByIdAsync(id);
                if (department != null)
                {
                    var data = new
                    {
                        id = department.Id,
                        name = department.Name,
                        description = department.Description,
                        generalDirectorateId = department.GeneralDirectorateId,
                        displayOrder = department.DisplayOrder,
                        isActive = department.IsActive
                    };
                    return Json(new { success = true, data });
                }

                return Json(new { success = false, message = "Department not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting department {DepartmentId}", id);
                return Json(new { success = false, message = "An error occurred while retrieving the department" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartmentsData()
        {
            try
            {
                var departments = await _lookupsService.GetDepartmentsAsync();
                var data = departments.Select(d => new
                {
                    id = d.Id,
                    name = d.Name,
                    description = d.Description,
                    generalDirectorateId = d.GeneralDirectorateId,
                    generalDirectorateName = d.GeneralDirectorate?.Name ?? "Unknown",
                    displayOrder = d.DisplayOrder,
                    isActive = d.IsActive,
                    templateCount = 0, // TODO: Get actual count from templates
                    createdDate = d.CreatedDate.ToString("yyyy-MM-dd")
                });

                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting departments data");
                return Json(new { data = new object[0] });
            }
        }

        #endregion

        #region General Directorate CRUD Operations


        [HttpGet]
        public async Task<IActionResult> GetGeneralDirectorate(int id)
        {
            try
            {
                var gd = await _lookupsService.GetGeneralDirectorateByIdAsync(id);
                if (gd != null)
                {
                    var data = new
                    {
                        id = gd.Id,
                        name = gd.Name,
                        description = gd.Description,
                        displayOrder = gd.DisplayOrder,
                        isActive = gd.IsActive
                    };
                    return Json(new { success = true, data });
                }

                return Json(new { success = false, message = "General Directorate not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting general directorate {GDId}", id);
                return Json(new { success = false, message = "An error occurred while retrieving the general directorate" });
            }
        }

        #endregion


        #region AJAX DataTable Methods

        [HttpGet]
        public async Task<IActionResult> GetCategoriesData()
        {
            try
            {
                var categories = await _lookupsService.GetCategoriesAsync();
                var data = categories.Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    description = c.Description,
                    displayOrder = c.DisplayOrder,
                    isActive = c.IsActive,
                    templateCount = 0, // TODO: Get actual count from templates
                    createdDate = c.CreatedDate.ToString("yyyy-MM-dd")
                });

                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories data");
                return Json(new { data = new object[0] });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetVendorsData()
        {
            try
            {
                var vendors = await _lookupsService.GetVendorsAsync();
                var data = vendors.Select(v => new
                {
                    id = v.Id,
                    name = v.Name,

                    companyName = v.CompanyName,
                    contactPerson = v.ContactPerson,
                    contactEmail = v.ContactEmail,
                    contactPhone = v.ContactPhone,
                    taxNumber = v.TaxNumber,
                    commercialRegister = v.CommercialRegister,
                    isApproved = v.IsApproved,
                    isActive = v.IsActive,
                    templateCount = 0, // TODO: Get actual count from templates
                    createdDate = v.CreatedDate.ToString("yyyy-MM-dd")
                });

                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vendors data");
                return Json(new { data = new object[0] });
            }
        }



        [HttpGet]
        public async Task<IActionResult> GetGeneralDirectoratesData()
        {
            try
            {
                var gds = await _lookupsService.GetGeneralDirectoratesAsync();
                var data = gds.Select(g => new
                {
                    id = g.Id,
                    name = g.Name,

                    description = g.Description,
                    managerName = g.ManagerName,
                    contactEmail = g.ContactEmail,
                    contactPhone = g.ContactPhone,
                    displayOrder = g.DisplayOrder,
                    isActive = g.IsActive,
                    departmentCount = g.Departments?.Count(d => d.IsActive) ?? 0,
                    createdDate = g.CreatedDate.ToString("yyyy-MM-dd")
                });

                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting general directorates data");
                return Json(new { data = new object[0] });
            }
        }

        #endregion

        #region Individual Get Methods for Edit Forms

        [HttpGet]
        public async Task<IActionResult> GetCategory(int id)
        {
            try
            {
                var category = await _lookupsService.GetCategoryByIdAsync(id);
                if (category != null)
                {
                    var data = new
                    {
                        id = category.Id,
                        name = category.Name,
                        description = category.Description,
                        displayOrder = category.DisplayOrder,
                        isActive = category.IsActive
                    };
                    return Json(new { success = true, data });
                }

                return Json(new { success = false, message = "Category not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category {CategoryId}", id);
                return Json(new { success = false, message = "An error occurred while retrieving the category" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetVendor(int id)
        {
            try
            {
                var vendor = await _lookupsService.GetVendorByIdAsync(id);
                if (vendor != null)
                {
                    var data = new
                    {
                        id = vendor.Id,
                        name = vendor.Name,

                        companyName = vendor.CompanyName,
                        contactPerson = vendor.ContactPerson,
                        contactEmail = vendor.ContactEmail,
                        contactPhone = vendor.ContactPhone,
                        taxNumber = vendor.TaxNumber,
                        commercialRegister = vendor.CommercialRegister,
                        isApproved = vendor.IsApproved,
                        isActive = vendor.IsActive
                    };
                    return Json(new { success = true, data });
                }

                return Json(new { success = false, message = "Vendor not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vendor {VendorId}", id);
                return Json(new { success = false, message = "An error occurred while retrieving the vendor" });
            }
        }

        [HttpGet]



        #endregion






        // GET: Lookups/Categories
        public async Task<IActionResult> Categories()
        {
            try
            {
                var model = new LookupsViewModel
                {
                    PageTitle = "Manage Categories",
                    EntityType = "Category",
                    Categories = await _lookupsService.GetCategoriesAsync()
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories page");
                TempData["Error"] = "Failed to load categories. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Lookups/Vendors
        public async Task<IActionResult> Vendors()
        {
            try
            {
                var model = new LookupsViewModel
                {
                    PageTitle = "Manage Vendors",
                    EntityType = "Vendor",
                    Vendors = await _lookupsService.GetVendorsAsync()
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading vendors page");
                TempData["Error"] = "Failed to load vendors. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Lookups/Departments
        public async Task<IActionResult> Departments()
        {
            try
            {
                var organizationalData = await _lookupsService.GetOrganizationalHierarchyAsync();
                var model = new LookupsViewModel
                {
                    PageTitle = "Manage Departments",
                    EntityType = "Department",
                    Departments = organizationalData.Departments,
                    GeneralDirectorates = organizationalData.GeneralDirectorates
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading departments page");
                TempData["Error"] = "Failed to load departments. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        #region Category CRUD Operations

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryDto categoryDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid data provided" });
                }

                var category = new Category
                {
                    Name = categoryDto.Name,
                    Description = categoryDto.Description,
                    DisplayOrder = categoryDto.DisplayOrder,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name ?? "System"
                };

                var result = await _lookupsService.CreateCategoryAsync(category);
                if (result.Success)
                {
                    return Json(new { success = true, message = "Category created successfully", data = result.Data });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return Json(new { success = false, message = "An error occurred while creating the category" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCategory([FromBody] CategoryDto categoryDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid data provided" });
                }

                var result = await _lookupsService.UpdateCategoryAsync(categoryDto.Id, categoryDto);
                if (result.Success)
                {
                    return Json(new { success = true, message = "Category updated successfully", data = result.Data });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category {CategoryId}", categoryDto.Id);
                return Json(new { success = false, message = "An error occurred while updating the category" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var result = await _lookupsService.DeleteCategoryAsync(id);
                if (result.Success)
                {
                    return Json(new { success = true, message = "Category deleted successfully" });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {CategoryId}", id);
                return Json(new { success = false, message = "An error occurred while deleting the category" });
            }
        }



        #endregion

        #region Vendor CRUD Operations

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVendor([FromBody] VendorDto vendorDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid data provided" });
                }

                var vendor = new Vendor
                {
                    Name = vendorDto.Name,

                    CompanyName = vendorDto.CompanyName,
                    ContactPerson = vendorDto.ContactPerson,
                    ContactEmail = vendorDto.ContactEmail,
                    ContactPhone = vendorDto.ContactPhone,
                    TaxNumber = vendorDto.TaxNumber,
                    CommercialRegister = vendorDto.CommercialRegister,
                    IsApproved = vendorDto.IsApproved,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name ?? "System"
                };

                var result = await _lookupsService.CreateVendorAsync(vendor);
                if (result.Success)
                {
                    return Json(new { success = true, message = "Vendor created successfully", data = result.Data });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vendor");
                return Json(new { success = false, message = "An error occurred while creating the vendor" });
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateVendor([FromBody] VendorDto vendorDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid data provided" });
                }

                var result = await _lookupsService.UpdateVendorAsync(vendorDto.Id, vendorDto);
                if (result.Success)
                {
                    return Json(new { success = true, message = "Vendor updated successfully", data = result.Data });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vendor {VendorId}", vendorDto.Id);
                return Json(new { success = false, message = "An error occurred while updating the vendor" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVendor(int id)
        {
            try
            {
                var result = await _lookupsService.DeleteVendorAsync(id);
                if (result.Success)
                {
                    return Json(new { success = true, message = "Vendor deleted successfully" });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vendor {VendorId}", id);
                return Json(new { success = false, message = "An error occurred while deleting the vendor" });
            }
        }



        #endregion

        #region Department CRUD Operations





        #endregion

        #region General Directorate CRUD Operations



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGeneralDirectorate([FromBody] GeneralDirectorateDto gdDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid data provided" });
                }

                var result = await _lookupsService.UpdateGeneralDirectorateAsync(gdDto.Id, gdDto);
                if (result.Success)
                {
                    return Json(new { success = true, message = "General Directorate updated successfully", data = result.Data });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating general directorate {GDId}", gdDto.Id);
                return Json(new { success = false, message = "An error occurred while updating the general directorate" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGeneralDirectorate(int id)
        {
            try
            {
                var result = await _lookupsService.DeleteGeneralDirectorateAsync(id);
                if (result.Success)
                {
                    return Json(new { success = true, message = "General Directorate deleted successfully" });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting general directorate {GDId}", id);
                return Json(new { success = false, message = "An error occurred while deleting the general directorate" });
            }
        }



        #endregion

        #region Helper Methods

        [HttpGet]
        public async Task<IActionResult> GetUsageDetails(string entityType, int id)
        {
            try
            {
                var usageDetails = await _lookupsService.GetUsageDetailsAsync(entityType, id);
                return Json(new { success = true, data = usageDetails });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage details for {EntityType} {Id}", entityType, id);
                return Json(new { success = false, message = "An error occurred while checking usage details" });
            }
        }




        [HttpGet]
        public async Task<IActionResult> ValidateName(string entityType, string name, int? excludeId = null)
        {
            try
            {
                var isUnique = await _lookupsService.IsNameUniqueAsync(entityType, name, excludeId);
                return Json(isUnique);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating name {Name} for {EntityType}", name, entityType);
                return Json(false);
            }
        }

        #endregion
    }
}
