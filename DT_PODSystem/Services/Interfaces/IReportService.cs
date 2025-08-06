// ✅ IReportService - Query Results Reporting Service Interface
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.ViewModels;

namespace DT_PODSystem.Services.Interfaces
{
    /// <summary>
    /// Report service interface for Query Results reporting and analytics
    /// </summary>
    public interface IReportService
    {
        Task<ReportSummaryViewModel> GetReportSummaryAsync(ReportFiltersDto? filters = null);
        Task<ChartDataDto> GetMonthlyTrendsDataAsync(ReportFiltersDto filters, string viewType = "monthly");
        Task<CategoryDistributionDto> GetCategoryDistributionDataAsync(ReportFiltersDto filters);
        Task<VendorPerformanceDto> GetVendorPerformanceDataAsync(ReportFiltersDto filters);
        Task<ProcessingTimeDto> GetProcessingTimeDataAsync(ReportFiltersDto filters);
        Task<DepartmentSuccessDto> GetDepartmentSuccessDataAsync(ReportFiltersDto filters);
        Task<SummaryStatsDto> GetSummaryStatsAsync(ReportFiltersDto filters);
        Task<List<LookupDto>> GetAvailableCategoriesAsync();
        Task<List<LookupDto>> GetAvailableVendorsAsync();
        Task<List<LookupDto>> GetAvailableDepartmentsAsync();
        Task<byte[]> GenerateExcelReportAsync(ReportFiltersDto filters);
        Task<byte[]> GeneratePdfReportAsync(ReportFiltersDto filters);
    }

    /// <summary>
    /// Validation result for Query Results
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public int TotalRecords { get; set; }
        public int ValidRecords { get; set; }
        public int InvalidRecords { get; set; }
        public Dictionary<string, int> ErrorSummary { get; set; } = new Dictionary<string, int>();
    }

    /// <summary>
    /// Recalculation result for Query Results
    /// </summary>
    public class RecalculationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ProcessedQueries { get; set; }
        public int TotalQueries { get; set; }
        public int UpdatedResults { get; set; }
        public int NewResults { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public Dictionary<string, object> Statistics { get; set; } = new Dictionary<string, object>();
    }
}