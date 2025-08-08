using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.Entities;
using DT_PODSystem.Models.Enums;
using DT_PODSystem.Models.ViewModels;
using DT_PODSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DT_PODSystem.Controllers
{
    [Authorize]
    public class PODController : Controller
    {
        private readonly IPODService _podService;
        private readonly ILookupsService _lookupsService;
        private readonly ILogger<PODController> _logger;

        public PODController(
            IPODService podService,
            ILookupsService lookupsService,
            ILogger<PODController> logger)
        {
            _podService = podService;
            _lookupsService = lookupsService;
            _logger = logger;
        }
         
        /// <summary>
        /// AJAX endpoint to save POD entries only
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePODEntries(int id, [FromBody] dynamic entriesData)
        {
            try
            {
                _logger.LogInformation("Saving POD entries for ID: {PODId}", id);

                var success = await _podService.SavePODEntriesFromJsonAsync(id, entriesData);

                if (success)
                {
                    return Json(new { success = true, message = "POD entries saved successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "POD not found or could not be updated." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving POD entries for ID: {PODId}", id);
                return Json(new { success = false, message = "An error occurred while saving POD entries." });
            }
        }

        /// <summary>
        /// Get POD data with entries for editing (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPODWithEntries(int id)
        {
            try
            {
                var pod = await _podService.GetPODWithEntriesAsync(id);

                if (pod == null)
                {
                    return Json(new { success = false, message = "POD not found." });
                }

                return Json(new
                {
                    success = true,
                    pod = pod,
                    message = "POD loaded successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting POD with entries for ID: {PODId}", id);
                return Json(new { success = false, message = "Error loading POD data." });
            }
        }    


        #region Index and List

        // GET: POD
        public async Task<IActionResult> Index(PODFiltersViewModel filters = null)
        {
            try
            {
                filters ??= new PODFiltersViewModel();
                filters.Pagination ??= new PaginationViewModel { CurrentPage = 1, PageSize = 25 };

                var model = await _podService.GetPODListAsync(filters);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading POD list");
                TempData["Error"] = "Failed to load PODs. Please try again.";
                return View(new PODListViewModel());
            }
        }

        // GET: POD/GetData (DataTables AJAX)
        [HttpGet]
        public async Task<IActionResult> GetPodsData()
        {
            try
            {
                var filters = new PODFiltersViewModel { Pagination = new PaginationViewModel { PageSize = int.MaxValue } };
                var podList = await _podService.GetPODListAsync(filters);

                var data = podList.PODs.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    podCode = p.PODCode,
                    description = p.Description ?? "No description",
                    category = p.CategoryName ?? "No Category",
                    department = p.DepartmentName ?? "No Department",
                    vendor = p.VendorName ?? "No Vendor",
                    status = p.Status.ToString(),
                    automationStatus = p.AutomationStatus.ToString(),
                    frequency = p.Frequency.ToString(),
                    templateCount = p.TemplateCount,
                    processedCount = p.ProcessedCount,
                    lastProcessedDate = p.LastProcessedDate?.ToString("yyyy-MM-dd") ?? "Never",
                    createdDate = p.CreatedDate.ToString("yyyy-MM-dd"),
                    createdBy = p.CreatedBy ?? "System",
                    requiresApproval = p.RequiresApproval,
                    isFinancialData = p.IsFinancialData,
                    processingPriority = p.ProcessingPriority
                });

                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PODs data");
                return Json(new { data = new object[0] });
            }
        }

        #endregion

        #region Create

        // GET: POD/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var model = new PODCreateEditViewModel
                {
                    POD = new PODCreationDto
                    {
                        AutomationStatus = AutomationStatus.PDF,
                        Frequency = ProcessingFrequency.Monthly,
                        ProcessingPriority = 5,
                        RequiresApproval = false,
                        IsFinancialData = false
                    }
                };

                // Load lookup data
                await LoadLookupDataAsync(model);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create POD page");
                TempData["Error"] = "Failed to load page. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: POD/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PODCreateEditViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadLookupDataAsync(model);
                    return View(model);
                }

                var pod = await _podService.CreatePODAsync(model.POD);

                TempData["Success"] = $"POD '{pod.Name}' created successfully!";
                return RedirectToAction(nameof(Details), new { id = pod.Id });
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
                await LoadLookupDataAsync(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating POD");
                ModelState.AddModelError("", "An error occurred while creating the POD. Please try again.");
                await LoadLookupDataAsync(model);
                return View(model);
            }
        }

        #endregion

        #region Edit

        // GET: POD/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var pod = await _podService.GetPODAsync(id);
                if (pod == null)
                {
                    TempData["Error"] = "POD not found.";
                    return RedirectToAction(nameof(Index));
                }

                var model = new PODCreateEditViewModel
                {
                    POD = new PODCreationDto
                    {
                        Name = pod.Name,
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
                        RequiresApproval = pod.RequiresApproval,
                        IsFinancialData = pod.IsFinancialData,
                        ProcessingPriority = pod.ProcessingPriority
                    },
                    IsEditing = true,
                    EditingPODId = id
                };

                await LoadLookupDataAsync(model);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit POD page for ID: {PODId}", id);
                TempData["Error"] = "Failed to load POD. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: POD/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PODCreateEditViewModel model)
        {
            try
            {
                if (id != model.EditingPODId)
                {
                    TempData["Error"] = "Invalid POD ID.";
                    return RedirectToAction(nameof(Index));
                }

                if (!ModelState.IsValid)
                {
                    model.IsEditing = true;
                    model.EditingPODId = id;
                    await LoadLookupDataAsync(model);
                    return View(model);
                }

                var updateDto = new PODUpdateDto
                {
                    Name = model.POD.Name,
                    Description = model.POD.Description,
                    PONumber = model.POD.PONumber,
                    ContractNumber = model.POD.ContractNumber,
                    CategoryId = model.POD.CategoryId,
                    DepartmentId = model.POD.DepartmentId,
                    VendorId = model.POD.VendorId,
                    AutomationStatus = model.POD.AutomationStatus,
                    Frequency = model.POD.Frequency,
                    VendorSPOCUsername = model.POD.VendorSPOCUsername,
                    GovernorSPOCUsername = model.POD.GovernorSPOCUsername,
                    FinanceSPOCUsername = model.POD.FinanceSPOCUsername,
                    RequiresApproval = model.POD.RequiresApproval,
                    IsFinancialData = model.POD.IsFinancialData,
                    ProcessingPriority = model.POD.ProcessingPriority
                };

                var success = await _podService.UpdatePODAsync(id, updateDto);

                if (success)
                {
                    TempData["Success"] = "POD updated successfully!";
                    return RedirectToAction(nameof(Details), new { id });
                }
                else
                {
                    ModelState.AddModelError("", "Failed to update POD. It may have been deleted.");
                    model.IsEditing = true;
                    model.EditingPODId = id;
                    await LoadLookupDataAsync(model);
                    return View(model);
                }
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
                model.IsEditing = true;
                model.EditingPODId = id;
                await LoadLookupDataAsync(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating POD with ID: {PODId}", id);
                ModelState.AddModelError("", "An error occurred while updating the POD. Please try again.");
                model.IsEditing = true;
                model.EditingPODId = id;
                await LoadLookupDataAsync(model);
                return View(model);
            }
        }

        #endregion

        #region Details

        // GET: POD/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var pod = await _podService.GetPODAsync(id);
                if (pod == null)
                {
                    TempData["Error"] = "POD not found.";
                    return RedirectToAction(nameof(Index));
                }

                var model = new PODDetailsViewModel
                {
                    POD = pod
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading POD details for ID: {PODId}", id);
                TempData["Error"] = "Failed to load POD details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region AJAX Operations

        // POST: POD/Delete (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _podService.DeletePODAsync(id);

                if (success)
                {
                    return Json(new { success = true, message = "POD deleted successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "POD not found or could not be deleted." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting POD with ID: {PODId}", id);
                return Json(new { success = false, message = "An error occurred while deleting the POD." });
            }
        }

        // GET: POD/Get (AJAX)
        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var pod = await _podService.GetPODAsync(id);
                if (pod != null)
                {
                    var data = new
                    {
                        id = pod.Id,
                        name = pod.Name,
                        podCode = pod.PODCode,
                        description = pod.Description,
                        poNumber = pod.PONumber,
                        contractNumber = pod.ContractNumber,
                        categoryId = pod.CategoryId,
                        departmentId = pod.DepartmentId,
                        vendorId = pod.VendorId,
                        status = pod.Status.ToString(),
                        automationStatus = pod.AutomationStatus.ToString(),
                        frequency = pod.Frequency.ToString(),
                        vendorSPOCUsername = pod.VendorSPOCUsername,
                        governorSPOCUsername = pod.GovernorSPOCUsername,
                        financeSPOCUsername = pod.FinanceSPOCUsername,
                        requiresApproval = pod.RequiresApproval,
                        isFinancialData = pod.IsFinancialData,
                        processingPriority = pod.ProcessingPriority,
                        version = pod.Version,
                        processedCount = pod.ProcessedCount,
                        lastProcessedDate = pod.LastProcessedDate?.ToString("yyyy-MM-dd HH:mm"),
                        createdDate = pod.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
                        createdBy = pod.CreatedBy
                    };
                    return Json(new { success = true, data });
                }

                return Json(new { success = false, message = "POD not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting POD {PODId}", id);
                return Json(new { success = false, message = "An error occurred while retrieving the POD" });
            }
        }

        #endregion

        #region Helper Methods

        private async Task LoadLookupDataAsync(PODCreateEditViewModel model)
        {
            try
            {
                // Load categories
                var categories = await _lookupsService.GetCategoriesAsync();
                model.Categories = categories.Where(c => c.IsActive)
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    }).ToList();

                // Load departments with general directorates
                var organizationalData = await _lookupsService.GetOrganizationalHierarchyAsync();
                model.Departments = organizationalData.Departments.Where(d => d.IsActive)
                    .Select(d => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = $"{d.Name} ({d.GeneralDirectorateName ?? "No GD"})"
                    }).ToList();

                // Load vendors
                var vendors = await _lookupsService.GetVendorsAsync();
                model.Vendors = vendors.Where(v => v.IsActive)
                    .Select(v => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = v.Id.ToString(),
                        Text = v.Name
                    }).ToList();

                model.Vendors.Insert(0, new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = "",
                    Text = "Select Vendor (Optional)"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading lookup data");
                model.Categories = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
                model.Departments = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
                model.Vendors = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
            }
        }

        #endregion
    }
}