//// ✅ ReportController - Query Results Reporting with Advanced Filtering
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text.Json;
//using System.Threading.Tasks;
//using DT_PODSystem.Areas.Security.Helpers;
//using DT_PODSystem.Data;
//using DT_PODSystem.Models.DTOs;
//using DT_PODSystem.Models.Entities;
//using DT_PODSystem.Models.ViewModels;
//using DT_PODSystem.Services.Interfaces;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;

//namespace DT_PODSystem.Controllers
//{
//    [Authorize]
//    public class ReportController : Controller
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly IReportService _reportService;
//        private readonly ILogger<ReportController> _logger;

//        public ReportController(
//            ApplicationDbContext context,
//            IReportService reportService,
//            ILogger<ReportController> logger)
//        {
//            _context = context;
//            _reportService = reportService;
//            _logger = logger;
//        }

//        /// <summary>
//        /// Interactive Summary Dashboard
//        /// </summary>
//        public async Task<IActionResult> Summary()
//        {
//            try
//            {
//                var model = await _reportService.GetReportSummaryAsync();
//                return View(model);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error loading Report Summary");
//                TempData["Error"] = "Error loading summary data.";
//                return RedirectToAction("Index", "Dashboard");
//            }
//        }

//        [HttpPost]
//        public async Task<IActionResult> GetChartData(string viewType, ReportFiltersDto filters)
//        {
//            try
//            {
//                var chartData = await _reportService.GetMonthlyTrendsDataAsync(filters, viewType);
//                return Json(chartData);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting chart data for viewType: {ViewType}", viewType);
//                return Json(new { error = "Failed to load chart data" });
//            }
//        }

//        [HttpPost]
//        public async Task<IActionResult> GetFilteredData(ReportFiltersDto filters)
//        {
//            try
//            {
//                var data = new
//                {
//                    monthlyTrends = await _reportService.GetMonthlyTrendsDataAsync(filters),
//                    categoryDistribution = await _reportService.GetCategoryDistributionDataAsync(filters),
//                    vendorPerformance = await _reportService.GetVendorPerformanceDataAsync(filters),
//                    processingTime = await _reportService.GetProcessingTimeDataAsync(filters),
//                    departmentSuccess = await _reportService.GetDepartmentSuccessDataAsync(filters)
//                };

//                return Json(data);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting filtered data");
//                return Json(new { error = "Failed to load filtered data" });
//            }
//        }

//        [HttpPost]
//        public async Task<IActionResult> ExportSummary(string data)
//        {
//            try
//            {
//                var exportRequest = JsonSerializer.Deserialize<ExportRequestDto>(data);
//                var filters = exportRequest.Filters;

//                if (exportRequest.Format.ToLower() == "excel")
//                {
//                    var excelData = await _reportService.GenerateExcelReportAsync(filters);
//                    return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
//                               $"ProcessingSummary_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
//                }
//                else
//                {
//                    var pdfData = await _reportService.GeneratePdfReportAsync(filters);
//                    return File(pdfData, "application/pdf",
//                               $"ProcessingSummary_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error exporting summary");
//                TempData["Error"] = "Failed to export summary.";
//                return RedirectToAction("Summary");
//            }
//        }

//        /// <summary>
//        /// Main Query Results listing page with DataTables and advanced filtering
//        /// </summary>
//        public async Task<IActionResult> Index(QueryResultFiltersViewModel filters)
//        {
//            try
//            {
//                _logger.LogInformation("📊 Loading Query Results report with filters");

//                var viewModel = new QueryResultReportViewModel
//                {
//                    UserRole = GetUserRole(),
//                    CanViewFinancialData = User.IsAdmin() || User.IsInRole("Auditor"),
//                    CanAudit = User.IsAdmin() || User.IsInRole("Auditor"),
//                    CanExport = User.IsAdmin() || User.IsInRole("Auditor"),
//                    Filters = filters ?? new QueryResultFiltersViewModel()
//                };

//                // Load filter options
//                await LoadFilterOptionsAsync(viewModel);

//                // Load summary statistics using ReportService
//                var summaryFilters = MapToReportFilters(filters);
//                viewModel.Summary = await GetQueryResultSummaryAsync(filters);

//                // Set up bulk actions based on user role
//                viewModel.BulkActions = GetBulkActions();

//                // Set up export options
//                viewModel.ExportOptions = GetExportOptions();

//                return View(viewModel);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "💥 Error loading Query Results report");
//                TempData["Error"] = "Failed to load report. Please try again.";
//                return RedirectToAction("Index", "Home");
//            }
//        }

//        // Fix the GetQueryResultsData method in ReportController

//        [HttpPost]
//        public async Task<IActionResult> GetQueryResultsData([FromForm] QueryResultDataTableRequest request)
//        {
//            try
//            {
//                _logger.LogDebug("📋 DataTables request: Page {Page}, Size {Size}, Search: {Search}",
//                    request.Start / request.Length + 1, request.Length, request.Search?.Value);

//                // Build query with DataTables parameters
//                var query = _context.QueryResults
//                    .Include(qr => qr.Query)
//                    .Include(qr => qr.QueryOutput)
//                    .Include(qr => qr.ProcessedFile)
//                        .ThenInclude(pf => pf.Template)
//                        .ThenInclude(t => t.POD)
//                    .AsQueryable();

//                // Apply filters
//                query = ApplyDataTableFilters(query, request);

//                // Get total count before pagination
//                var totalRecords = await query.CountAsync();

//                // Apply search
//                if (!string.IsNullOrEmpty(request.Search?.Value))
//                {
//                    var searchTerm = request.Search.Value.ToLower();
//                    query = query.Where(qr =>
//                        qr.OutputName.ToLower().Contains(searchTerm) ||
//                        qr.CalculatedValue.ToLower().Contains(searchTerm) ||
//                        qr.Query.Name.ToLower().Contains(searchTerm) ||
//                        qr.PeriodId.Contains(searchTerm));
//                }

//                var filteredRecords = await query.CountAsync();

//                // Apply sorting
//                query = ApplyDataTableSorting(query, request);

//                // Apply pagination and get data with proper column names
//                var rawData = await query
//                    .Skip(request.Start)
//                    .Take(request.Length)
//                    .Select(qr => new
//                    {
//                        id = qr.Id,
//                        periodId = qr.PeriodId,
//                        queryName = qr.Query.Name,
//                        outputName = qr.OutputName,
//                        calculatedValue = qr.CalculatedValue,
//                        dataType = qr.OutputDataType,
//                        vendorName = qr.ProcessedFile.Template.POD.Vendor != null ? qr.ProcessedFile.Template.POD.Vendor.Name : "N/A",
//                        executedDate = qr.ExecutedDate,
//                        isValid = qr.IsValid,
//                        needApproval = qr.NeedApproval,
//                        isApproved = qr.IsApproved
//                    })
//                    .ToListAsync();

//                // Transform data to match your existing DataTable column expectations
//                var data = rawData.Select(item => new
//                {
//                    id = item.id,
//                    periodDisplay = $"{item.periodId.Substring(0, 4)}-{item.periodId.Substring(4, 2)}", // Format: 2025-01
//                    queryName = item.queryName,
//                    outputName = item.outputName,
//                    calculatedValue = item.calculatedValue,
//                    outputDataType = item.dataType,
//                    originalFileName = "N/A", // QueryResult doesn't have this directly
//                    categoryName = item.queryName, // Using query name as category
//                    vendorName = item.vendorName,
//                    executedDate = item.executedDate,
//                    isValid = item.isValid
//                }).ToList();

//                var response = new
//                {
//                    draw = request.Draw,
//                    recordsTotal = totalRecords,
//                    recordsFiltered = filteredRecords,
//                    data = data
//                };

//                return Json(response);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "💥 Error loading QueryResults DataTable data");
//                return Json(new
//                {
//                    draw = request.Draw,
//                    recordsTotal = 0,
//                    recordsFiltered = 0,
//                    data = Array.Empty<object>(),
//                    error = "Failed to load data"
//                });
//            }
//        }

//        /// <summary>
//        /// Query Result details modal
//        /// </summary>
//        public async Task<IActionResult> Details(int id)
//        {
//            try
//            {
//                var result = await _context.QueryResults
//                    .Include(qr => qr.Query)
//                    .Include(qr => qr.QueryOutput)
//                    .Include(qr => qr.ProcessedFile)
//                        .ThenInclude(pf => pf.Template)
//                        .ThenInclude(t => t.POD)
//                    .Include(qr => qr.ProcessedFile)
//                        .ThenInclude(pf => pf.Template)                         
//                    .FirstOrDefaultAsync(qr => qr.Id == id);

//                if (result == null)
//                {
//                    return NotFound();
//                }

//                return PartialView("_DetailsModal", result);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "💥 Error loading QueryResult details for ID: {Id}", id);
//                return Json(new { success = false, message = "Failed to load details" });
//            }
//        }

//        /// <summary>
//        /// Export Query Results to Excel/CSV
//        /// </summary>
//        [HttpPost]
//        [Authorize(Roles = "Admin,Auditor")]
//        public async Task<IActionResult> Export(QueryResultFiltersViewModel filters, string format = "excel")
//        {
//            try
//            {
//                _logger.LogInformation("📤 Exporting Query Results: Format={Format}, User={User}", format, User.Identity?.Name);

//                var reportFilters = MapToReportFilters(filters);
//                var exportData = format == "excel"
//                    ? await _reportService.GenerateExcelReportAsync(reportFilters)
//                    : await _reportService.GeneratePdfReportAsync(reportFilters);

//                var fileName = $"QueryResults_{DateTime.Now:yyyyMMdd_HHmmss}.{(format == "excel" ? "xlsx" : "csv")}";
//                var contentType = format == "excel"
//                    ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
//                    : "text/csv";

//                return File(exportData, contentType, fileName);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "💥 Error exporting Query Results");
//                TempData["Error"] = "Export failed. Please try again.";
//                return RedirectToAction("Index");
//            }
//        }

//        /// <summary>
//        /// Bulk operations on selected Query Results
//        /// </summary>
//        [HttpPost]
//        [Authorize(Roles = "Admin,Auditor")]
//        public async Task<IActionResult> BulkAction(string action, int[] selectedIds, string? comment = null)
//        {
//            try
//            {
//                _logger.LogInformation("🔄 Bulk action: {Action} on {Count} items by {User}",
//                    action, selectedIds?.Length ?? 0, User.Identity?.Name);

//                if (selectedIds == null || selectedIds.Length == 0)
//                {
//                    return Json(new { success = false, message = "No items selected" });
//                }

//                var results = await _context.QueryResults
//                    .Where(qr => selectedIds.Contains(qr.Id))
//                    .ToListAsync();

//                foreach (var result in results)
//                {
//                    switch (action.ToLower())
//                    {
//                        case "approve":
//                            result.IsApproved = true;
//                            result.ApprovedBy = User.Identity?.Name ?? "Unknown";
//                            result.ApprovalDate = DateTime.UtcNow;
//                            break;
//                        case "reject":
//                            result.IsApproved = false;
//                            result.ValidationErrors = comment ?? "Rejected by user";
//                            break;
//                        case "markvalid":
//                            result.IsValid = true;
//                            result.ValidationErrors = null;
//                            break;
//                    }

//                    result.ModifiedDate = DateTime.UtcNow;
//                    result.ModifiedBy = User.Identity?.Name ?? "Unknown";
//                }

//                await _context.SaveChangesAsync();

//                var message = $"Successfully {action} {results.Count} query results";
//                TempData["Success"] = message;

//                return Json(new { success = true, message = message });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "💥 Error executing bulk action: {Action}", action);
//                return Json(new { success = false, message = "Bulk action failed" });
//            }
//        }
//        private async Task<QueryResultSummaryViewModel> GetQueryResultSummaryAsync(QueryResultFiltersViewModel filters)
//        {
//            var reportFilters = MapToReportFilters(filters);
//            var summaryDto = await _reportService.GetSummaryStatsAsync(reportFilters);

//            return new QueryResultSummaryViewModel
//            {
//                TotalResults = summaryDto.TotalProcessed,
//                ValidResults = summaryDto.SuccessCount,
//                InvalidResults = summaryDto.TotalProcessed - summaryDto.SuccessCount,
//                AverageExecutionTime = double.Parse(summaryDto.AverageProcessingTime.Replace(" min", "")),
//                UniqueQueries = summaryDto.TotalCategories, // Categories represent queries in our service
//                UniqueVendors = summaryDto.TotalVendors
//            };
//        }

//        private async Task<QueryResultTrendsChartViewModel> GetQueryResultTrendsAsync(QueryResultFiltersViewModel filters)
//        {
//            var reportFilters = MapToReportFilters(filters);
//            var trendsDto = await _reportService.GetMonthlyTrendsDataAsync(reportFilters);

//            return new QueryResultTrendsChartViewModel
//            {
//                Periods = trendsDto.Categories.ToList(),
//                TotalResults = trendsDto.Series.FirstOrDefault(s => s.Name == "Total Processed")?.Data.Cast<int>().ToList() ?? new List<int>(),
//                ValidResults = trendsDto.Series.FirstOrDefault(s => s.Name == "Successful")?.Data.Cast<int>().ToList() ?? new List<int>(),
//                ApprovedResults = new List<int>(), // Not available in current DTO
//                AverageConfidence = new List<decimal>() // Not available in current DTO
//            };
//        }

//        private async Task<VendorAnalyticsViewModel> GetVendorAnalyticsAsync(QueryResultFiltersViewModel filters)
//        {
//            var reportFilters = MapToReportFilters(filters);
//            var vendorDto = await _reportService.GetVendorPerformanceDataAsync(reportFilters);

//            return new VendorAnalyticsViewModel
//            {
//                VendorNames = vendorDto.VendorNames.ToList(),
//                ResultCounts = vendorDto.TotalDocuments.ToList(),
//                SuccessRates = vendorDto.SuccessRates.ToList(),
//                AverageValues = new List<decimal>() // Not available in current DTO - you can calculate this if needed
//            };
//        }
//        public async Task<IActionResult> Analytics(QueryResultFiltersViewModel filters)
//        {
//            try
//            {
//                _logger.LogInformation("📈 Loading Query Results analytics");

//                var viewModel = new QueryResultAnalyticsViewModel
//                {
//                    UserRole = GetUserRole(),
//                    Filters = filters ?? new QueryResultFiltersViewModel(),
//                    TrendsChart = await GetQueryResultTrendsAsync(filters),
//                    VendorAnalytics = await GetVendorAnalyticsAsync(filters)
//                };

//                // Load filter options
//                await LoadAnalyticsFilterOptionsAsync(viewModel);

//                return View(viewModel);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "💥 Error loading Query Results analytics");
//                TempData["Error"] = "Failed to load analytics. Please try again.";
//                return RedirectToAction("Index");
//            }
//        }

//        #region Private Helper Methods

//        private ReportFiltersDto MapToReportFilters(QueryResultFiltersViewModel filters)
//        {
//            return new ReportFiltersDto
//            {
//                Categories = filters.QueryId.HasValue ? new List<int> { filters.QueryId.Value } : new List<int>(),
//                Vendors = filters.VendorId.HasValue ? new List<int> { filters.VendorId.Value } : new List<int>(),
//                Departments = filters.DepartmentId.HasValue ? new List<int> { filters.DepartmentId.Value } : new List<int>(),
//                Status = !string.IsNullOrEmpty(filters?.Status) ? new List<string> { filters.Status } : new List<string>(),
//                StartDate = filters?.ExecutedFromDate,
//                EndDate = filters?.ExecutedToDate
//            };
//        }

//        private IQueryable<QueryResult> ApplyDataTableFilters(IQueryable<QueryResult> query, QueryResultDataTableRequest request)
//        {
//            // Apply any additional filters from request
//            // This is where you'd add custom filter logic based on request parameters
//            return query;
//        }

//        private IQueryable<QueryResult> ApplyDataTableSorting(IQueryable<QueryResult> query, QueryResultDataTableRequest request)
//        {
//            if (request.Order != null && request.Order.Any())
//            {
//                var order = request.Order.First();
//                var columnIndex = order.Column;
//                var sortDirection = order.Dir;

//                // Map column index to property name
//                var sortExpression = columnIndex switch
//                {
//                    0 => sortDirection == "asc" ? query.OrderBy(qr => qr.PeriodId) : query.OrderByDescending(qr => qr.PeriodId),
//                    1 => sortDirection == "asc" ? query.OrderBy(qr => qr.Query.Name) : query.OrderByDescending(qr => qr.Query.Name),
//                    2 => sortDirection == "asc" ? query.OrderBy(qr => qr.OutputName) : query.OrderByDescending(qr => qr.OutputName),
//                    3 => sortDirection == "asc" ? query.OrderBy(qr => qr.CalculatedValue) : query.OrderByDescending(qr => qr.CalculatedValue),
//                    7 => sortDirection == "asc" ? query.OrderBy(qr => qr.ExecutedDate) : query.OrderByDescending(qr => qr.ExecutedDate),
//                    _ => query.OrderByDescending(qr => qr.ExecutedDate)
//                };

//                return sortExpression;
//            }

//            return query.OrderByDescending(qr => qr.ExecutedDate);
//        }

//        private async Task LoadFilterOptionsAsync(QueryResultReportViewModel viewModel)
//        {
//            // Load unique periods from QueryResults
//            var periods = await _context.QueryResults
//                .Select(qr => qr.PeriodId)
//                .Distinct()
//                .OrderByDescending(p => p)
//                .ToListAsync();

//            viewModel.Filters.PeriodOptions = periods.Select(p => new SelectListItem
//            {
//                Value = p,
//                Text = $"{p.Substring(0, 4)}-{p.Substring(4, 2)}" // Format: 2025-01
//            }).ToList();

//            // Load queries (instead of categories for QueryResults)
//            viewModel.Filters.CategoryOptions = await _context.Queries
//                .Where(q => q.IsActive)
//                .OrderBy(q => q.Name)
//                .Select(q => new SelectListItem { Value = q.Id.ToString(), Text = q.Name })
//                .ToListAsync();

//            // Load vendors through ProcessedFile -> Template -> Vendor
//            var vendorData = await _context.QueryResults
//                .Include(qr => qr.ProcessedFile)
//                .ThenInclude(pf => pf.Template)
//                .ThenInclude(t => t.POD)
//                .Where(qr => qr.ProcessedFile.Template.POD.Vendor != null)
//                .Select(qr => new
//                {
//                    Id = qr.ProcessedFile.Template.POD.Vendor!.Id,
//                    Name = qr.ProcessedFile.Template.POD.Vendor.Name
//                })
//                .Distinct()
//                .OrderBy(v => v.Name)
//                .ToListAsync();

//            viewModel.Filters.VendorOptions = vendorData.Select(v => new SelectListItem
//            {
//                Value = v.Id.ToString(),
//                Text = v.Name
//            }).ToList();

//            // Load departments through ProcessedFile -> Template -> Department
//            // Fix: Use ToListAsync() first, then do string concatenation in memory
//            var departmentData = await _context.QueryResults
//                .Include(qr => qr.ProcessedFile)
//                .ThenInclude(pf => pf.Template)
//                .ThenInclude(t => t.POD)                
//                .Select(qr => new
//                {
//                    Id = qr.ProcessedFile.Template.POD.Department.Id,
//                    DepartmentName = qr.ProcessedFile.Template.POD.Department.Name,
//                    GeneralDirectorateName = qr.ProcessedFile.Template.POD.Department.GeneralDirectorate.Name
//                })
//                .Distinct()
//                .ToListAsync(); // Execute query first

//            // Then do string concatenation in memory
//            viewModel.Filters.DepartmentOptions = departmentData
//                .Select(d => new SelectListItem
//                {
//                    Value = d.Id.ToString(),
//                    Text = $"{d.GeneralDirectorateName} - {d.DepartmentName}" // String concatenation in memory
//                })
//                .OrderBy(d => d.Text)
//                .ToList();

//            // Status options
//            viewModel.Filters.StatusOptions = new List<SelectListItem>
//    {
//        new SelectListItem { Value = "success", Text = "Valid" },
//        new SelectListItem { Value = "failed", Text = "Invalid" },
//        new SelectListItem { Value = "pending", Text = "Pending Review" }
//    };
//        }

//        private async Task LoadAnalyticsFilterOptionsAsync(QueryResultAnalyticsViewModel viewModel)
//        {
//            // Same as above but for analytics view
//            var periods = await _context.QueryResults
//                .Select(qr => qr.PeriodId)
//                .Distinct()
//                .OrderByDescending(p => p)
//                .Take(12) // Last 12 months for analytics
//                .ToListAsync();

//            viewModel.Filters.PeriodOptions = periods.Select(p => new SelectListItem
//            {
//                Value = p,
//                Text = $"{p.Substring(0, 4)}-{p.Substring(4, 2)}"
//            }).ToList();
//        }

//        private List<BulkActionViewModel> GetBulkActions()
//        {
//            var actions = new List<BulkActionViewModel>();

//            if (User.IsAdmin() || User.IsInRole("Auditor"))
//            {
//                actions.AddRange(new[]
//                {
//                    new BulkActionViewModel
//                    {
//                        Action = "approve",
//                        DisplayName = "Approve",
//                        IconClass = "fa fa-check",
//                        CssClass = "btn btn-success",
//                        RequiresConfirmation = true,
//                        ConfirmationMessage = "Approve selected query results?"
//                    },
//                    new BulkActionViewModel
//                    {
//                        Action = "reject",
//                        DisplayName = "Reject",
//                        IconClass = "fa fa-times",
//                        CssClass = "btn btn-danger",
//                        RequiresConfirmation = true,
//                        ConfirmationMessage = "Reject selected query results?"
//                    },
//                    new BulkActionViewModel
//                    {
//                        Action = "markvalid",
//                        DisplayName = "Mark Valid",
//                        IconClass = "fa fa-check-circle",
//                        CssClass = "btn btn-info",
//                        RequiresConfirmation = true,
//                        ConfirmationMessage = "Mark selected results as valid?"
//                    }
//                });
//            }

//            return actions;
//        }

//        private List<ExportOptionViewModel> GetExportOptions()
//        {
//            return new List<ExportOptionViewModel>
//            {
//                new ExportOptionViewModel
//                {
//                    Format = "excel",
//                    DisplayName = "Export to Excel",
//                    IconClass = "fa fa-file-excel",
//                    Description = "Export to Excel with full formatting"
//                },
//                new ExportOptionViewModel
//                {
//                    Format = "csv",
//                    DisplayName = "Export to CSV",
//                    IconClass = "fa fa-file-csv",
//                    Description = "Export to CSV for data analysis"
//                }
//            };
//        }

//        private string GetUserRole()
//        {
//            if (User.IsAdmin()) return "Admin";
//            if (User.IsInRole("Auditor")) return "Auditor";
//            if (User.IsInRole("Viewer")) return "Viewer";
//            return "Unknown";
//        }

//        #endregion
//    }
//}



//// DataTable request model
//public class QueryResultDataTableRequest
//{
//    public int Draw { get; set; }
//    public int Start { get; set; }
//    public int Length { get; set; }
//    public DataTableSearch? Search { get; set; }
//    public List<DataTableOrder>? Order { get; set; }
//}

//public class DataTableSearch
//{
//    public string Value { get; set; } = string.Empty;
//}

//public class DataTableOrder
//{
//    public int Column { get; set; }
//    public string Dir { get; set; } = "asc";
//}