// ✅ Report ViewModels - Query Results Reporting with Advanced Filtering
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DT_PODSystem.Models.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DT_PODSystem.Models.ViewModels
{
    public class ReportSummaryViewModel
    {
        public string DateRange { get; set; } = string.Empty;
        public SummaryStatsDto Summary { get; set; } = new();
        public MonthlyTrendsDto MonthlyTrends { get; set; } = new();
        public CategoryDistributionDto CategoryDistribution { get; set; } = new();
        public VendorPerformanceDto VendorPerformance { get; set; } = new();
        public ProcessingTimeDto ProcessingTime { get; set; } = new();
        public DepartmentSuccessDto DepartmentSuccess { get; set; } = new();

        public List<LookupDto> AvailableCategories { get; set; } = new();
        public List<LookupDto> AvailableVendors { get; set; } = new();
        public List<LookupDto> AvailableDepartments { get; set; } = new();
    }

    /// <summary>
    /// Main Query Results Report ViewModel
    /// </summary>
    public class QueryResultReportViewModel
    {
        // User context
        public string UserRole { get; set; } = string.Empty;
        public bool CanViewFinancialData { get; set; }
        public bool CanAudit { get; set; }
        public bool CanExport { get; set; }

        // Filters
        public QueryResultFiltersViewModel Filters { get; set; } = new QueryResultFiltersViewModel();

        // Summary statistics
        public QueryResultSummaryViewModel Summary { get; set; } = new QueryResultSummaryViewModel();

        // Bulk operations
        public List<BulkActionViewModel> BulkActions { get; set; } = new List<BulkActionViewModel>();

        // Export options
        public List<ExportOptionViewModel> ExportOptions { get; set; } = new List<ExportOptionViewModel>();
    }

    /// <summary>
    /// Query Results Filters for advanced search
    /// </summary>
    public class QueryResultFiltersViewModel
    {
        [Display(Name = "Search")]
        public string? SearchTerm { get; set; }

        [Display(Name = "Period")]
        public string? PeriodId { get; set; }

        [Display(Name = "From Period")]
        public string? FromPeriod { get; set; }

        [Display(Name = "To Period")]
        public string? ToPeriod { get; set; }

        [Display(Name = "Query")]
        public int? QueryId { get; set; }

        [Display(Name = "Output")]
        public int? QueryOutputId { get; set; }

        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        [Display(Name = "Vendor")]
        public int? VendorId { get; set; }

        [Display(Name = "Department")]
        public int? DepartmentId { get; set; }

        [Display(Name = "Status")]
        public string? Status { get; set; }

        [Display(Name = "Data Type")]
        public string? OutputDataType { get; set; }

        [Display(Name = "Min Value")]
        public decimal? MinValue { get; set; }

        [Display(Name = "Max Value")]
        public decimal? MaxValue { get; set; }

        [Display(Name = "Executed From")]
        [DataType(DataType.Date)]
        public DateTime? ExecutedFromDate { get; set; }

        [Display(Name = "Executed To")]
        [DataType(DataType.Date)]
        public DateTime? ExecutedToDate { get; set; }

        [Display(Name = "Min Confidence")]
        [Range(0, 1)]
        public decimal? MinConfidence { get; set; }

        [Display(Name = "Max Confidence")]
        [Range(0, 1)]
        public decimal? MaxConfidence { get; set; }

        [Display(Name = "Needs Approval")]
        public bool? NeedApproval { get; set; }

        [Display(Name = "Has Financial Data")]
        public bool? HasFinancialData { get; set; }

        [Display(Name = "Is Approved")]
        public bool? IsApproved { get; set; }

        [Display(Name = "Is Valid")]
        public bool? IsValid { get; set; }

        [Display(Name = "Has Errors")]
        public bool? HasErrors { get; set; }

        [Display(Name = "Approved By")]
        public string? ApprovedBy { get; set; }

        // Sorting and pagination
        public string SortBy { get; set; } = "ExecutedDate";
        public string SortDirection { get; set; } = "desc";
        public int PageSize { get; set; } = 25;
        public int CurrentPage { get; set; } = 1;

        // UI state
        public bool ShowAdvancedFilters { get; set; }
        public bool ShowFinancialData { get; set; } = true;

        // Filter options (populated by controller)
        public List<SelectListItem> PeriodOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> QueryOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> OutputOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> VendorOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> DepartmentOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> DataTypeOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> PageSizeOptions { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "10", Text = "10 per page" },
            new SelectListItem { Value = "25", Text = "25 per page" },
            new SelectListItem { Value = "50", Text = "50 per page" },
            new SelectListItem { Value = "100", Text = "100 per page" }
        };

        // Active filters count
        public int ActiveFilterCount =>
            (string.IsNullOrEmpty(SearchTerm) ? 0 : 1) +
            (string.IsNullOrEmpty(PeriodId) ? 0 : 1) +
            (QueryId.HasValue ? 1 : 0) +
            (QueryOutputId.HasValue ? 1 : 0) +
            (CategoryId.HasValue ? 1 : 0) +
            (VendorId.HasValue ? 1 : 0) +
            (DepartmentId.HasValue ? 1 : 0) +
            (string.IsNullOrEmpty(Status) ? 0 : 1) +
            (string.IsNullOrEmpty(OutputDataType) ? 0 : 1) +
            (MinValue.HasValue ? 1 : 0) +
            (MaxValue.HasValue ? 1 : 0) +
            (ExecutedFromDate.HasValue ? 1 : 0) +
            (ExecutedToDate.HasValue ? 1 : 0) +
            (MinConfidence.HasValue ? 1 : 0) +
            (MaxConfidence.HasValue ? 1 : 0) +
            (NeedApproval.HasValue ? 1 : 0) +
            (HasFinancialData.HasValue ? 1 : 0) +
            (IsApproved.HasValue ? 1 : 0) +
            (IsValid.HasValue ? 1 : 0) +
            (HasErrors.HasValue ? 1 : 0);

        public bool HasActiveFilters => ActiveFilterCount > 0;
    }

    /// <summary>
    /// Query Results Summary Statistics
    /// </summary>
    public class QueryResultSummaryViewModel
    {
        public int TotalResults { get; set; }
        public int ValidResults { get; set; }
        public int InvalidResults { get; set; }
        public int PendingApproval { get; set; }
        public int ApprovedResults { get; set; }
        public int WithFinancialData { get; set; }
        public int UniqueQueries { get; set; }
        public int UniquePeriods { get; set; }
        public int UniqueVendors { get; set; }
        public double AverageConfidence { get; set; }
        public double AverageExecutionTime { get; set; }
        public string LastExecutedPeriod { get; set; } = string.Empty;
        public DateTime? LastExecutionDate { get; set; }

        // Percentage calculations
        public decimal ValidPercentage => TotalResults > 0 ? (decimal)ValidResults / TotalResults * 100 : 0;
        public decimal ApprovalPercentage => TotalResults > 0 ? (decimal)ApprovedResults / TotalResults * 100 : 0;
        public decimal FinancialPercentage => TotalResults > 0 ? (decimal)WithFinancialData / TotalResults * 100 : 0;
    }

    /// <summary>
    /// DataTables request model for Query Results
    /// </summary>
    public class QueryResultDataTableRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public DataTableSearch? Search { get; set; }
        public List<DataTableOrder>? Order { get; set; }
        public List<DataTableColumn>? Columns { get; set; }

        // Custom filters
        public string? PeriodFilter { get; set; }
        public string? QueryFilter { get; set; }
        public string? OutputFilter { get; set; }
        public string? CategoryFilter { get; set; }
        public string? VendorFilter { get; set; }
        public string? DepartmentFilter { get; set; }
        public string? StatusFilter { get; set; }
        public string? DataTypeFilter { get; set; }
        public string? ApprovalFilter { get; set; }
        public string? ValidFilter { get; set; }
        public string? FinancialFilter { get; set; }
    }

    public class DataTableSearch
    {
        public string? Value { get; set; }
        public bool Regex { get; set; }
    }

    public class DataTableOrder
    {
        public int Column { get; set; }
        public string Dir { get; set; } = "asc";
    }

    public class DataTableColumn
    {
        public string? Data { get; set; }
        public string? Name { get; set; }
        public bool Searchable { get; set; }
        public bool Orderable { get; set; }
        public DataTableSearch? Search { get; set; }
    }

    /// <summary>
    /// DataTables response model for Query Results
    /// </summary>
    public class QueryResultDataTableResponse
    {
        public int Draw { get; set; }
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
        public List<QueryResultDataRowViewModel> Data { get; set; } = new List<QueryResultDataRowViewModel>();
        public string? Error { get; set; }
    }

    /// <summary>
    /// Individual Query Result row for DataTables
    /// </summary>
    public class QueryResultDataRowViewModel
    {
        public int Id { get; set; }
        public string PeriodId { get; set; } = string.Empty;
        public string PeriodDisplay => $"{PeriodId.Substring(0, 4)}-{PeriodId.Substring(4, 2)}";
        public string QueryName { get; set; } = string.Empty;
        public string OutputName { get; set; } = string.Empty;
        public string CalculatedValue { get; set; } = string.Empty;
        public string OutputDataType { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public DateTime ExecutedDate { get; set; }
        public long ExecutionTimeMs { get; set; }
        public decimal CalculationConfidence { get; set; }
        public bool IsValid { get; set; }
        public bool NeedApproval { get; set; }
        public bool HasFinancialData { get; set; }
        public bool IsApproved { get; set; }
        public string? ValidationErrors { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovalDate { get; set; }

        // Display helpers
        public string StatusBadge => IsValid ? (IsApproved ? "success" : (NeedApproval ? "warning" : "info")) : "danger";
        public string StatusText => IsValid ? (IsApproved ? "Approved" : (NeedApproval ? "Pending" : "Valid")) : "Invalid";
        public string ConfidenceBadge => CalculationConfidence >= 0.9m ? "success" : (CalculationConfidence >= 0.7m ? "warning" : "danger");
        public string ConfidenceText => $"{CalculationConfidence:P1}";
        public string ExecutionDisplay => ExecutionTimeMs < 1000 ? $"{ExecutionTimeMs}ms" : $"{ExecutionTimeMs / 1000.0:F1}s";
        public string ValueDisplay => OutputDataType == "Currency" ? $"${decimal.Parse(CalculatedValue):N2}" :
                                     OutputDataType == "Percentage" ? $"{decimal.Parse(CalculatedValue):P2}" :
                                     CalculatedValue;
    }

    /// <summary>
    /// Query Result Details Modal ViewModel
    /// </summary>
    public class QueryResultDetailsViewModel
    {
        public int Id { get; set; }
        public string PeriodId { get; set; } = string.Empty;
        public string QueryName { get; set; } = string.Empty;
        public string QueryDescription { get; set; } = string.Empty;
        public string OutputName { get; set; } = string.Empty;
        public string OutputDescription { get; set; } = string.Empty;
        public string CalculatedValue { get; set; } = string.Empty;
        public string OutputDataType { get; set; } = string.Empty;
        public string OriginalFormula { get; set; } = string.Empty;
        public string ProcessedFormula { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string OrganizedFilePath { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public DateTime ExecutedDate { get; set; }
        public long ExecutionTimeMs { get; set; }
        public decimal CalculationConfidence { get; set; }
        public bool IsValid { get; set; }
        public bool NeedApproval { get; set; }
        public bool HasFinancialData { get; set; }
        public bool IsApproved { get; set; }
        public string? ValidationErrors { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }

        // Related data
        public List<ProcessedFieldSummaryViewModel> InputFields { get; set; } = new List<ProcessedFieldSummaryViewModel>();
        public List<QueryConstantSummaryViewModel> UsedConstants { get; set; } = new List<QueryConstantSummaryViewModel>();
    }

    public class ProcessedFieldSummaryViewModel
    {
        public string FieldName { get; set; } = string.Empty;
        public string ExtractedValue { get; set; } = string.Empty;
        public string OutputDataType { get; set; } = string.Empty;
        public decimal ExtractionConfidence { get; set; }
        public bool IsValid { get; set; }
    }

    public class QueryConstantSummaryViewModel
    {
        public string ConstantName { get; set; } = string.Empty;
        public string ConstantValue { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsGlobal { get; set; }
    }

    /// <summary>
    /// Analytics ViewModel for Query Results
    /// </summary>
    public class QueryResultAnalyticsViewModel
    {
        public string UserRole { get; set; } = string.Empty;
        public QueryResultFiltersViewModel Filters { get; set; } = new QueryResultFiltersViewModel();

        public QueryResultTrendsChartViewModel? TrendsChart { get; set; }
        public SuccessRateChartViewModel? SuccessRateChart { get; set; }
        public QueryPerformanceChartViewModel? QueryPerformanceChart { get; set; }
        public VendorAnalyticsViewModel? VendorAnalytics { get; set; }
    }

    public class QueryResultTrendsChartViewModel
    {
        public List<string> Periods { get; set; } = new List<string>();
        public List<int> TotalResults { get; set; } = new List<int>();
        public List<int> ValidResults { get; set; } = new List<int>();
        public List<int> ApprovedResults { get; set; } = new List<int>();
        public List<decimal> AverageConfidence { get; set; } = new List<decimal>();
    }

    public class SuccessRateChartViewModel
    {
        public decimal ValidPercentage { get; set; }
        public decimal InvalidPercentage { get; set; }
        public decimal ApprovedPercentage { get; set; }
        public decimal PendingPercentage { get; set; }
    }

    public class QueryPerformanceChartViewModel
    {
        public List<string> QueryNames { get; set; } = new List<string>();
        public List<decimal> AverageExecutionTimes { get; set; } = new List<decimal>();
        public List<int> ExecutionCounts { get; set; } = new List<int>();
        public List<decimal> SuccessRates { get; set; } = new List<decimal>();
    }

    public class VendorAnalyticsViewModel
    {
        public List<string> VendorNames { get; set; } = new List<string>();
        public List<int> ResultCounts { get; set; } = new List<int>();
        public List<decimal> SuccessRates { get; set; } = new List<decimal>();
        public List<decimal> AverageValues { get; set; } = new List<decimal>();
    }


    public class ExportOptionViewModel
    {
        public string Format { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }


}