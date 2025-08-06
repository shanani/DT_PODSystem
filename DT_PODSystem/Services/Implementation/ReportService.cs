// Services/Implementation/ReportService.cs
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
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Services.Implementation
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportService> _logger;

        public ReportService(ApplicationDbContext context, ILogger<ReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ReportSummaryViewModel> GetReportSummaryAsync(ReportFiltersDto? filters = null)
        {
            filters ??= new ReportFiltersDto();

            var model = new ReportSummaryViewModel
            {
                DateRange = GetDateRangeString(filters),
                Summary = await GetSummaryStatsAsync(filters),
                MonthlyTrends = await GetMonthlyTrendsAsync(filters),
                CategoryDistribution = await GetCategoryDistributionDataAsync(filters),
                VendorPerformance = await GetVendorPerformanceDataAsync(filters),
                ProcessingTime = await GetProcessingTimeDataAsync(filters),
                DepartmentSuccess = await GetDepartmentSuccessDataAsync(filters),
                AvailableCategories = await GetAvailableCategoriesAsync(),
                AvailableVendors = await GetAvailableVendorsAsync(),
                AvailableDepartments = await GetAvailableDepartmentsAsync()
            };

            return model;
        }

        public async Task<ChartDataDto> GetMonthlyTrendsDataAsync(ReportFiltersDto filters, string viewType = "monthly")
        {
            var query = _context.QueryResults.AsQueryable();
            query = ApplyFilters(query, filters);

            if (viewType == "yearly")
            {
                var yearlyData = await query
                    .GroupBy(q => q.ExecutedDate.Year)
                    .Select(g => new
                    {
                        Year = g.Key,
                        Total = g.Count(),
                        Success = g.Count(x => x.IsValid == true),
                        Failed = g.Count(x => x.IsValid == false)
                    })
                    .OrderBy(x => x.Year)
                    .ToListAsync();

                return new ChartDataDto
                {
                    Series = new[]
                    {
                        new ChartSeriesDto { Name = "Total Processed", Data = yearlyData.Select(x => (object)x.Total).ToArray() },
                        new ChartSeriesDto { Name = "Successful", Data = yearlyData.Select(x => (object)x.Success).ToArray() },
                        new ChartSeriesDto { Name = "Failed", Data = yearlyData.Select(x => (object)x.Failed).ToArray() }
                    },
                    Categories = yearlyData.Select(x => x.Year.ToString()).ToArray()
                };
            }

            // Monthly data (default)
            var monthlyData = await query
                .GroupBy(q => new { q.ExecutedDate.Year, q.ExecutedDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Total = g.Count(),
                    Success = g.Count(x => x.IsValid == true),
                    Failed = g.Count(x => x.IsValid == false)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .Take(12)
                .ToListAsync();

            return new ChartDataDto
            {
                Series = new[]
                {
                    new ChartSeriesDto { Name = "Total Processed", Data = monthlyData.Select(x => (object)x.Total).ToArray() },
                    new ChartSeriesDto { Name = "Successful", Data = monthlyData.Select(x => (object)x.Success).ToArray() },
                    new ChartSeriesDto { Name = "Failed", Data = monthlyData.Select(x => (object)x.Failed).ToArray() }
                },
                Categories = monthlyData.Select(x => $"{GetMonthName(x.Month)} {x.Year}").ToArray()
            };
        }

        public async Task<CategoryDistributionDto> GetCategoryDistributionDataAsync(ReportFiltersDto filters)
        {
            var query = _context.QueryResults
                .Include(q => q.Query)
                .AsQueryable();

            query = ApplyFilters(query, filters);

            var categoryData = await query
                .GroupBy(q => q.Query.Name)
                .Select(g => new
                {
                    QueryName = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            return new CategoryDistributionDto
            {
                Labels = categoryData.Select(x => x.QueryName).ToArray(),
                Values = categoryData.Select(x => x.Count).ToArray()
            };
        }

        public async Task<VendorPerformanceDto> GetVendorPerformanceDataAsync(ReportFiltersDto filters)
        {
            var query = _context.QueryResults
                .Include(q => q.ProcessedFile)
                .ThenInclude(pf => pf.Template)
                .ThenInclude(t => t.Vendor)
                .AsQueryable();

            query = ApplyFilters(query, filters);

            var vendorData = await query
                .Where(q => q.ProcessedFile.Template.Vendor != null)
                .GroupBy(q => q.ProcessedFile.Template.Vendor!.Name)
                .Select(g => new
                {
                    VendorName = g.Key,
                    TotalDocuments = g.Count(),
                    SuccessfulDocuments = g.Count(x => x.IsValid == true),
                    SuccessRate = g.Count() > 0 ? (decimal)g.Count(x => x.IsValid == true) / g.Count() * 100 : 0
                })
                .OrderByDescending(x => x.TotalDocuments)
                .Take(10)
                .ToListAsync();

            return new VendorPerformanceDto
            {
                VendorNames = vendorData.Select(x => x.VendorName).ToArray(),
                SuccessRates = vendorData.Select(x => x.SuccessRate).ToArray(),
                TotalDocuments = vendorData.Select(x => x.TotalDocuments).ToArray()
            };
        }

        public async Task<ProcessingTimeDto> GetProcessingTimeDataAsync(ReportFiltersDto filters)
        {
            var query = _context.QueryResults.AsQueryable();
            query = ApplyFilters(query, filters);

            var processingData = await query
                .GroupBy(q => q.ExecutedDate.Hour / 4) // Group by 4-hour intervals
                .Select(g => new
                {
                    TimeSlot = g.Key,
                    AverageTime = g.Average(x => x.ExecutionTimeMs) / 1000.0, // Convert to seconds
                    DocumentCount = g.Count()
                })
                .OrderBy(x => x.TimeSlot)
                .ToListAsync();

            var timeLabels = new[] { "00:00", "04:00", "08:00", "12:00", "16:00", "20:00" };

            return new ProcessingTimeDto
            {
                TimeLabels = timeLabels,
                AverageTime = processingData.Select(x => (decimal)(x.AverageTime / 60.0)).ToArray(), // Convert to minutes
                DocumentVolume = processingData.Select(x => x.DocumentCount).ToArray()
            };
        }

        public async Task<DepartmentSuccessDto> GetDepartmentSuccessDataAsync(ReportFiltersDto filters)
        {
            var query = _context.QueryResults
                .Include(q => q.ProcessedFile)
                .ThenInclude(pf => pf.Template)
                .ThenInclude(t => t.Department)
                .AsQueryable();

            query = ApplyFilters(query, filters);

            var departmentData = await query
                .GroupBy(q => q.ProcessedFile.Template.Department.Name)
                .Select(g => new
                {
                    DepartmentName = g.Key,
                    SuccessRate = g.Count() > 0 ? (decimal)g.Count(x => x.IsValid == true) / g.Count() * 100 : 0
                })
                .OrderByDescending(x => x.SuccessRate)
                .ToListAsync();

            return new DepartmentSuccessDto
            {
                DepartmentNames = departmentData.Select(x => x.DepartmentName).ToArray(),
                SuccessRates = departmentData.Select(x => x.SuccessRate).ToArray()
            };
        }

        public async Task<SummaryStatsDto> GetSummaryStatsAsync(ReportFiltersDto filters)
        {
            var query = _context.QueryResults.AsQueryable();
            query = ApplyFilters(query, filters);

            var stats = await query
                .GroupBy(q => 1)
                .Select(g => new
                {
                    TotalProcessed = g.Count(),
                    SuccessCount = g.Count(x => x.IsValid == true),
                    AverageProcessingTime = g.Average(x => x.ExecutionTimeMs) / 1000.0 // Convert to seconds
                })
                .FirstOrDefaultAsync();

            var totalQueries = await _context.Queries.CountAsync();
            var totalVendors = await _context.Vendors.CountAsync();

            return new SummaryStatsDto
            {
                TotalProcessed = stats?.TotalProcessed ?? 0,
                SuccessCount = stats?.SuccessCount ?? 0,
                SuccessRate = stats?.TotalProcessed > 0 ? (decimal)stats.SuccessCount / stats.TotalProcessed * 100 : 0,
                AverageProcessingTime = $"{(stats?.AverageProcessingTime ?? 0) / 60.0:F1} min",
                TotalCategories = totalQueries,
                TotalVendors = totalVendors
            };
        }

        public async Task<List<LookupDto>> GetAvailableCategoriesAsync()
        {
            // Get categories through Queries since QueryResult doesn't have direct Category relation
            return await _context.Queries
                .Select(q => new LookupDto { Id = q.Id, Name = q.Name })
                .OrderBy(q => q.Name)
                .ToListAsync();
        }

        public async Task<List<LookupDto>> GetAvailableVendorsAsync()
        {
            // Get vendors through ProcessedFile -> Template -> Vendor relationship
            return await _context.QueryResults
                .Include(q => q.ProcessedFile)
                .ThenInclude(pf => pf.Template)
                .ThenInclude(t => t.Vendor)
                .Where(q => q.ProcessedFile.Template.Vendor != null)
                .Select(q => new LookupDto
                {
                    Id = q.ProcessedFile.Template.Vendor!.Id,
                    Name = q.ProcessedFile.Template.Vendor.Name
                })
                .Distinct()
                .OrderBy(v => v.Name)
                .ToListAsync();
        }

        public async Task<List<LookupDto>> GetAvailableDepartmentsAsync()
        {
            // Get departments through ProcessedFile -> Template -> Department relationship
            return await _context.QueryResults
                .Include(q => q.ProcessedFile)
                .ThenInclude(pf => pf.Template)
                .ThenInclude(t => t.Department)
                .Select(q => new LookupDto
                {
                    Id = q.ProcessedFile.Template.Department.Id,
                    Name = q.ProcessedFile.Template.Department.Name
                })
                .Distinct()
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<byte[]> GenerateExcelReportAsync(ReportFiltersDto filters)
        {
            // TODO: Implement Excel generation using EPPlus or similar
            // This is a placeholder implementation
            var data = await GetReportSummaryAsync(filters);

            // Create Excel file with summary data
            // Include charts, tables, and filtered results

            return new byte[0]; // Placeholder
        }

        public async Task<byte[]> GeneratePdfReportAsync(ReportFiltersDto filters)
        {
            // TODO: Implement PDF generation using iTextSharp or similar
            // This is a placeholder implementation
            var data = await GetReportSummaryAsync(filters);

            // Create PDF report with charts and data

            return new byte[0]; // Placeholder
        }

        private async Task<MonthlyTrendsDto> GetMonthlyTrendsAsync(ReportFiltersDto filters)
        {
            var chartData = await GetMonthlyTrendsDataAsync(filters, "monthly");

            return new MonthlyTrendsDto
            {
                Labels = chartData.Categories,
                TotalProcessed = chartData.Series.FirstOrDefault(s => s.Name == "Total Processed")?.Data.Cast<int>().ToArray() ?? Array.Empty<int>(),
                Successful = chartData.Series.FirstOrDefault(s => s.Name == "Successful")?.Data.Cast<int>().ToArray() ?? Array.Empty<int>(),
                Failed = chartData.Series.FirstOrDefault(s => s.Name == "Failed")?.Data.Cast<int>().ToArray() ?? Array.Empty<int>()
            };
        }

        private IQueryable<QueryResult> ApplyFilters(IQueryable<QueryResult> query, ReportFiltersDto filters)
        {
            // Apply date range filter
            if (filters.StartDate.HasValue)
            {
                query = query.Where(q => q.ExecutedDate >= filters.StartDate.Value);
            }
            if (filters.EndDate.HasValue)
            {
                query = query.Where(q => q.ExecutedDate <= filters.EndDate.Value);
            }

            // Apply query filter (instead of category)
            if (filters.Categories.Any())
            {
                query = query.Where(q => filters.Categories.Contains(q.QueryId));
            }

            // Apply vendor filter through ProcessedFile -> Template -> Vendor
            if (filters.Vendors.Any())
            {
                query = query.Where(q => q.ProcessedFile.Template.VendorId.HasValue &&
                                       filters.Vendors.Contains(q.ProcessedFile.Template.VendorId.Value));
            }

            // Apply department filter through ProcessedFile -> Template -> Department
            if (filters.Departments.Any())
            {
                query = query.Where(q => filters.Departments.Contains(q.ProcessedFile.Template.DepartmentId));
            }

            // Apply status filter using IsValid boolean
            if (filters.Status.Any())
            {
                var includeSuccess = filters.Status.Contains("success");
                var includeFailed = filters.Status.Contains("failed");
                var includePending = filters.Status.Contains("pending");

                if (includeSuccess && !includeFailed && !includePending)
                {
                    query = query.Where(q => q.IsValid == true);
                }
                else if (includeFailed && !includeSuccess && !includePending)
                {
                    query = query.Where(q => q.IsValid == false);
                }
                else if (includePending && !includeSuccess && !includeFailed)
                {
                    query = query.Where(q => q.NeedApproval == true && q.IsApproved == false);
                }
                else if (includeSuccess && includeFailed && !includePending)
                {
                    query = query.Where(q => q.IsValid == true || q.IsValid == false);
                }
                // Add other combinations as needed
            }

            return query;
        }

        private string GetDateRangeString(ReportFiltersDto filters)
        {
            if (filters.StartDate.HasValue && filters.EndDate.HasValue)
            {
                return $"{filters.StartDate.Value:MMM d} - {filters.EndDate.Value:MMM d, yyyy}";
            }

            return $"{DateTime.Now.AddDays(-30):MMM d} - {DateTime.Now:MMM d, yyyy}";
        }

        private string GetMonthName(int month)
        {
            return new DateTime(2000, month, 1).ToString("MMM");
        }
    }
}