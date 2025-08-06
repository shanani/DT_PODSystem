using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Helpers;
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
    public class QueryController : Controller
    {
        private readonly IQueryService _queryService;
        private readonly ILogger<QueryController> _logger;

        public QueryController(
            IQueryService queryService,
            ILogger<QueryController> logger)
        {
            _queryService = queryService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SaveQuery([FromBody] SaveQueryRequest request)
        {
            try
            {
                _logger.LogInformation("SaveQuery called - checking request");

                if (request == null)
                {
                    _logger.LogError("❌ Request object is NULL");

                    if (!ModelState.IsValid)
                    {
                        foreach (var error in ModelState)
                        {
                            _logger.LogError("ModelState Error: {Key} = {Error}",
                                error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                        }
                    }

                    return BadRequest(new { success = false, message = "Request is null" });
                }

                _logger.LogInformation("✅ Request object bound successfully");
                _logger.LogInformation("QueryId: {QueryId}", request.QueryId);
                _logger.LogInformation("Data object: {HasData}", request.Data != null);

                if (request.Data != null)
                {
                    _logger.LogInformation("Name: {Name}", request.Data.Name ?? "Not provided");
                    _logger.LogInformation("Description: {Description}", request.Data.Description ?? "Not provided");
                    _logger.LogInformation("Status: {Status}", request.Data.Status?.ToString() ?? "Not provided");
                    _logger.LogInformation("Constants count: {Count}", request.Data.Constants?.Count ?? 0);
                    _logger.LogInformation("OutputFields count: {Count}", request.Data.Outputs?.Count ?? 0);
                    _logger.LogInformation("CanvasState provided: {HasCanvas}", !string.IsNullOrEmpty(request.Data.CanvasState));
                }

                // ✅ Validate query name from Data object
                if (!string.IsNullOrWhiteSpace(request.Data?.Name))
                {
                    if (request.Data.Name.Length < 3 || request.Data.Name.Length > 200)
                    {
                        return Json(new { success = false, message = "Query name must be between 3-200 characters" });
                    }
                }

                // ✅ Simple call with just QueryId and Data
                var success = await _queryService.SaveQueryDataAsync(
                    request.QueryId,
                    request.Data
                );

                if (success)
                {
                    _logger.LogInformation("✅ Query {QueryId} saved successfully", request.QueryId);
                    return Ok(new { success = true, message = "Query saved successfully" });
                }
                else
                {
                    _logger.LogWarning("❌ Failed to save query {QueryId}", request.QueryId);
                    return Ok(new { success = false, message = "Failed to save query" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SaveQuery for QueryId: {QueryId}", request?.QueryId);
                return StatusCode(500, new { success = false, message = "An error occurred while saving the query" });
            }
        }



        // GET: Edit existing query (full query builder)
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                _logger.LogInformation("Loading query for editing: {QueryId}", id);

                // Get the complete query data for editing
                var model = await _queryService.GetQueryBuilderStateAsync(id);

                if (model == null)
                {
                    TempData["Error"] = "Query not found";
                    return RedirectToAction("Index");
                }

                model.IsEditMode = true;

                _logger.LogInformation("Query loaded for editing: {QueryName} (Status: {Status})",
                    model.QueryName, model.Status);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading query for editing: {QueryId}", id);
                TempData["Error"] = "Failed to load query for editing";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveConstant([FromBody] SaveQueryConstantRequest request)
        {
            try
            {
                if (request?.Constant == null)
                {
                    return Json(new { success = false, message = "Invalid request" });
                }

                var result = await _queryService.SaveConstantAsync(request.QueryId, request.Constant);
                return Json(new
                {
                    success = result.Success,
                    id = result.Id,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving constant for query {QueryId}", request?.QueryId);
                return Json(new { success = false, message = "Error saving constant" });
            }
        }



        [HttpGet]
        public async Task<IActionResult> GetConstant(int id)
        {
            try
            {
                var constant = await _queryService.GetConstantAsync(id);
                if (constant == null)
                {
                    return Json(new { success = false, message = "Constant not found" });
                }

                return Json(new { success = true, constant });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting constant {ConstantId}", id);
                return Json(new { success = false, message = "Error loading constant" });
            }
        }


        [HttpPost]
        public async Task<IActionResult> SaveOutput([FromBody] SaveQueryOutputRequest request)
        {
            try
            {
                if (request?.Output == null)
                {
                    return Json(new { success = false, message = "Invalid request" });
                }

                var result = await _queryService.SaveOutputAsync(request.QueryId, request.Output);
                return Json(new
                {
                    success = result.Success,
                    id = result.Id,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving output for query {QueryId}", request?.QueryId);
                return Json(new { success = false, message = "Error saving output" });
            }
        }



        [HttpGet]
        public async Task<IActionResult> GetOutput(int id)
        {
            try
            {
                var output = await _queryService.GetOutputAsync(id);
                if (output == null)
                {
                    return Json(new { success = false, message = "Output not found" });
                }

                return Json(new { success = true, output });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting output {OutputId}", id);
                return Json(new { success = false, message = "Error loading output" });
            }
        }



        // GET: Create new query (shows the form)
        public async Task<IActionResult> Create()
        {
            try
            {
                // Create a minimal model for the create form
                var model = new QueryBuilderViewModel
                {
                    QueryId = 0, // New query
                    QueryName = string.Empty,
                    Description = string.Empty,
                    Status = QueryStatus.Draft,
                    IsEditMode = false,

                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create query page");
                TempData["Error"] = "Failed to load create query page";
                return RedirectToAction("Index");
            }
        }

        // POST: Create new query with basic information
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateQueryRequest request)
        {
            try
            {
                _logger.LogInformation("Creating new query with name: {QueryName}", request.Name);

                // Validate request
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Query name is required"
                    });
                }

                if (request.Name.Length < 3)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Query name must be at least 3 characters long"
                    });
                }

                if (request.Name.Length > 200)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Query name cannot exceed 200 characters"
                    });
                }

                // Check for name pattern (optional - if you want to enforce naming convention)
                if (!System.Text.RegularExpressions.Regex.IsMatch(request.Name, @"^[a-zA-Z_][a-zA-Z0-9_\s]*$"))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Query name must start with a letter and contain only letters, numbers, spaces, and underscores"
                    });
                }

                // Create basic query DTO
                var queryDto = new QueryDto
                {
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim() ?? string.Empty,
                    Status = QueryStatus.Draft
                };

                // Create the query using the service
                var createdQuery = await _queryService.CreateQueryAsync(queryDto);

                if (createdQuery != null)
                {
                    _logger.LogInformation("Query created successfully with ID: {QueryId}", createdQuery.Id);

                    return Json(new
                    {
                        success = true,
                        message = "Query created successfully!",
                        queryId = createdQuery.Id,
                        queryName = createdQuery.Name
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Failed to create query. Please try again."
                    });
                }
            }
            catch (ArgumentException argEx)
            {
                _logger.LogWarning(argEx, "Invalid query data: {Message}", argEx.Message);
                return Json(new
                {
                    success = false,
                    message = argEx.Message
                });
            }
            catch (InvalidOperationException opEx)
            {
                _logger.LogWarning(opEx, "Query creation failed: {Message}", opEx.Message);
                return Json(new
                {
                    success = false,
                    message = "A query with this name may already exist. Please choose a different name."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating query with name: {QueryName}", request.Name);
                return Json(new
                {
                    success = false,
                    message = "An unexpected error occurred while creating the query. Please try again."
                });
            }
        }


        /// <summary>
        /// Request model for creating a new query
        /// </summary>
        public class CreateQueryRequest
        {
            [Required]
            [StringLength(200, MinimumLength = 3)]
            public string Name { get; set; } = string.Empty;

            [StringLength(1000)]
            public string? Description { get; set; }

            public string Status { get; set; } = "Draft";
            public int ExecutionPriority { get; internal set; }
        }

        /// <summary>
        /// Request model for updating query basic information
        /// </summary>
        public class UpdateQueryInfoRequest
        {
            [Required]
            public int QueryId { get; set; }

            [Required]
            [StringLength(200, MinimumLength = 3)]
            public string Name { get; set; } = string.Empty;

            [StringLength(1000)]
            public string? Description { get; set; }
        }


        // GET: Query Management Index
        public async Task<IActionResult> Index(QueryFiltersViewModel filters)
        {
            try
            {
                // Ensure pagination is set
                filters ??= new QueryFiltersViewModel();
                filters.Pagination ??= new PaginationViewModel { CurrentPage = 1, PageSize = 25 };

                var model = await _queryService.GetQueryListAsync(filters);

                // Set user permissions
                model.UserRole = GetUserRole();
                model.CanCreate = User.IsAdmin();
                model.CanEdit = User.IsAdmin();
                model.CanDelete = User.IsAdmin();
                model.CanTest = User.IsAdmin() || User.IsInRole("Auditor");

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load query list");
                TempData["Error"] = "Failed to load queries";
                return View(new QueryListViewModel());
            }
        }


        // GET: Query details view
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var query = await _queryService.GetQueryAsync(id);
                if (query == null)
                {
                    TempData["Error"] = "Query not found";
                    return RedirectToAction("Index");
                }

                return View(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load query details for ID {QueryId}", id);
                TempData["Error"] = "Failed to load query details";
                return RedirectToAction("Index");
            }
        }

        #region Query Constants Management


        /// <summary>
        /// Delete query constant with usage validation (AJAX)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DeleteConstant([FromBody] DeleteQueryConstantRequest request)
        {
            try
            {
                var result = await _queryService.DeleteConstantAsync(request.QueryId, request.ConstantId);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    errorCode = result.ErrorCode,
                    usageDetails = result.UsageDetails,
                    requiredActions = result.RequiredActions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting constant {ConstantId} for query {QueryId}",
                    request.ConstantId, request.QueryId);

                return Json(new
                {
                    success = false,
                    message = "An unexpected error occurred while deleting the constant",
                    errorCode = "SYSTEM_ERROR"
                });
            }
        }



        /// <summary>
        /// Get all constants for a query (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetConstants(int queryId)
        {
            try
            {
                var constants = await _queryService.GetQueryConstantsAsync(queryId);
                return Json(new
                {
                    success = true,
                    globalConstants = constants.Where(c => c.IsGlobal).ToList(),
                    localConstants = constants.Where(c => !c.IsGlobal).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting constants for query {QueryId}", queryId);
                return Json(new { success = false, message = "Error loading constants" });
            }
        }

        #endregion

        #region Query Outputs Management



        /// <summary>
        /// Delete query output with usage validation (AJAX)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DeleteOutput([FromBody] DeleteQueryOutputRequest request)
        {
            try
            {
                var result = await _queryService.DeleteOutputAsync(request.QueryId, request.OutputId);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    errorCode = result.ErrorCode,
                    usageDetails = result.UsageDetails,
                    requiredActions = result.RequiredActions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting output {OutputId} for query {QueryId}",
                    request.OutputId, request.QueryId);

                return Json(new
                {
                    success = false,
                    message = "An unexpected error occurred while deleting the output field",
                    errorCode = "SYSTEM_ERROR"
                });
            }
        }


        /// <summary>
        /// Get all outputs for a query (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOutputs(int queryId)
        {
            try
            {
                var outputs = await _queryService.GetQueryOutputsAsync(queryId);
                return Json(new
                {
                    success = true,
                    data = outputs,
                    count = outputs.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outputs for query {QueryId}", queryId);
                return Json(new { success = false, message = "Error loading outputs" });
            }
        }

        #endregion



        #region Query Lifecycle Management

        /// <summary>
        /// Create new query (POST)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateQuery([FromBody] CreateQueryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Json(new { success = false, message = "Query name is required" });
                }

                var query = await _queryService.CreateQueryAsync(new QueryDto
                {
                    Name = request.Name,
                    Description = request.Description,
                    Status = QueryStatus.Draft,
                    ExecutionPriority = request.ExecutionPriority
                });

                return Json(new
                {
                    success = true,
                    queryId = query.Id,
                    message = "Query created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating query");
                return Json(new { success = false, message = "Error creating query" });
            }
        }

        /// <summary>
        /// Update query basic information (POST)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateQuery([FromBody] UpdateQueryRequest request)
        {
            try
            {
                var result = await _queryService.UpdateQueryAsync(request.QueryId, new QueryDto
                {
                    Id = request.QueryId,
                    Name = request.Name,
                    Description = request.Description,
                    Status = request.Status,
                    ExecutionPriority = request.ExecutionPriority
                });

                return Json(new
                {
                    success = result,
                    message = result ? "Query updated successfully" : "Failed to update query"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating query {QueryId}", request.QueryId);
                return Json(new { success = false, message = "Error updating query" });
            }
        }

        /// <summary>
        /// Delete query (POST)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _queryService.DeleteQueryAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Query deleted successfully" });
                }
                return Json(new { success = false, message = "Query not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting query {QueryId}", id);
                return Json(new { success = false, message = "Error deleting query: " + ex.Message });
            }
        }

        /// <summary>
        /// Bulk actions for queries (POST)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> BulkAction(string action, List<int> queryIds)
        {
            try
            {
                if (queryIds == null || !queryIds.Any())
                {
                    return Json(new { success = false, message = "No queries selected" });
                }

                var result = await _queryService.BulkActionAsync(action, queryIds);
                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    processedCount = result.ProcessedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk action {Action}", action);
                return Json(new { success = false, message = "Error performing bulk action" });
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get user role for permissions
        /// </summary>
        private string GetUserRole()
        {
            if (User.IsAdmin()) return "Admin";
            if (User.IsInRole("Auditor")) return "Auditor";
            if (User.IsInRole("Viewer")) return "Viewer";
            return "Unknown";
        }

        #endregion
    }

    #region Request Classes


    public class CreateQueryRequest
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int ExecutionPriority { get; set; } = 5;
    }

    public class UpdateQueryRequest
    {
        public int QueryId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public QueryStatus Status { get; set; }
        public int ExecutionPriority { get; set; }
    }

    // ========================================================================================
    // Request Models (Aligned with Template Pattern)
    // ========================================================================================

    // ✅ UPGRADED: Enhanced SaveQueryRequest to include basic information
    public class SaveQueryRequest
    {
        public int QueryId { get; set; }
        public QueryDataDto? Data { get; set; }

    }



    public class DeleteConstantRequest
    {
        public int QueryId { get; set; }
        public int ConstantId { get; set; }
    }


    public class DeleteOutputRequest
    {
        public int QueryId { get; set; }
        public int OutputId { get; set; }
    }


    public class FinalizeQueryRequest
    {
        public int QueryId { get; set; }
        public string? Notes { get; set; }
    }


    #endregion
}