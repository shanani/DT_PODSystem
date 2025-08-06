using System;
using System.Collections.Generic;

namespace DT_PODSystem.Models.ViewModels
{
    // Additional ViewModels for extended dashboard functionality

    public class MonthlyProcessingChartViewModel
    {
        public List<string> Categories { get; set; } = new List<string>();
        public List<ChartSeriesViewModel> Series { get; set; } = new List<ChartSeriesViewModel>();
        public ChartSeriesViewModel? ConfidenceSeries { get; set; }
        public string Title { get; set; } = "Monthly Processing";
        public string Type { get; set; } = "line";
    }

    public class StatusDistributionChartViewModel
    {
        public List<PieChartDataViewModel> Data { get; set; } = new List<PieChartDataViewModel>();
        public string Title { get; set; } = "Status Distribution";
        public string Type { get; set; } = "pie";
    }

    public class PendingAuditViewModel
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public DateTime ProcessedDate { get; set; }
        public int DaysWaiting { get; set; }
        public bool HasFinancialInfo { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string PriorityBadgeClass { get; set; } = string.Empty;
    }

    public class ProcessingPerformanceViewModel
    {
        public MonthlyPerformanceViewModel CurrentMonth { get; set; } = new MonthlyPerformanceViewModel();
        public MonthlyPerformanceViewModel PreviousMonth { get; set; } = new MonthlyPerformanceViewModel();
        public string Trend { get; set; } = "stable"; // up, down, stable
    }

    public class MonthlyPerformanceViewModel
    {
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public decimal SuccessRate { get; set; }
    }

    public class TopPerformingTemplateViewModel
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public int TotalProcessed { get; set; }
        public decimal SuccessRate { get; set; }
        public decimal AverageConfidence { get; set; }
    }

    public class DepartmentPerformanceViewModel
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int TemplateCount { get; set; }
        public int TotalProcessed { get; set; }
        public decimal SuccessRate { get; set; }
    }


    public class DashboardViewModel
    {
        public string UserName { get; set; }
        public string UserRole { get; set; }
        public DashboardStatsViewModel Stats { get; set; }
        public IEnumerable<DashboardQuickActionViewModel> QuickActions { get; set; }
        public IEnumerable<AlertViewModel> Alerts { get; set; }
        public List<RecentActivityViewModel> RecentActivities { get; set; }
        public bool CanViewFinancialData { get; set; }
        public IEnumerable<PendingAuditViewModel> PendingAudits { get; set; }
        public TemplateUsageChartViewModel TemplateUsageChart { get; internal set; }
        public ProcessingTrendsChartViewModel ProcessingTrendsChart { get; internal set; }
        public DepartmentDistributionChartViewModel DepartmentChart { get; internal set; }
        public MonthlyProcessingChartViewModel MonthlyProcessingChart { get; set; } = new MonthlyProcessingChartViewModel();
        public StatusDistributionChartViewModel StatusDistributionChart { get; set; } = new StatusDistributionChartViewModel();

        // Performance metrics
        public ProcessingPerformanceViewModel ProcessingPerformance { get; set; } = new ProcessingPerformanceViewModel();
        public List<TopPerformingTemplateViewModel> TopPerformingTemplates { get; set; } = new List<TopPerformingTemplateViewModel>();
        public List<DepartmentPerformanceViewModel> DepartmentPerformance { get; set; } = new List<DepartmentPerformanceViewModel>();

        // System health indicators
        public SystemHealthViewModel SystemHealth { get; set; } = new SystemHealthViewModel();


    }

    public class DashboardStats
    {
        public int TotalTemplates { get; set; }
        public int ActiveTemplates { get; set; }
        public int MonthlyProcessingCount { get; set; }
        public double SuccessRate { get; set; }
        public decimal? TotalMonthlyAmount { get; set; }
        public string Currency { get; set; }
        public double? MonthlyVariance { get; set; }
        public int? PendingAudits { get; set; }
        public double? AuditEfficiency { get; set; }
    }

    public class QuickAction
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string IconClass { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class Alert
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public string ActionUrl { get; set; }
    }

    public class Activity
    {
        public string EntityName { get; set; }
        public string Description { get; set; }
        public string IconClass { get; set; }
        public string BadgeClass { get; set; }
        public string Status { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class Audit
    {
        public string FileName { get; set; }
        public string TemplateName { get; set; }
        public string DepartmentName { get; set; }
        public DateTime ProcessedDate { get; set; }
        public string Priority { get; set; }
        public string PriorityBadgeClass { get; set; }
        public int DaysWaiting { get; set; }
        public bool HasFinancialInfo { get; set; }
        public int Id { get; set; }
    }


    public class SystemHealthViewModel
    {
        public decimal OverallHealthScore { get; set; } // 0-100
        public string HealthStatus { get; set; } = "Good"; // Excellent, Good, Fair, Poor
        public string HealthStatusClass { get; set; } = "text-success"; // CSS class
        public List<HealthIndicatorViewModel> Indicators { get; set; } = new List<HealthIndicatorViewModel>();
    }

    public class HealthIndicatorViewModel
    {
        public string Name { get; set; } = string.Empty;
        public decimal Score { get; set; } // 0-100
        public string Status { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public string? Description { get; set; }
    }




    public class DashboardStatsViewModel
    {
        public int TotalTemplates { get; set; }
        public int ActiveTemplates { get; set; }
        public int DraftTemplates { get; set; }
        public int ArchivedTemplates { get; set; }

        public int MonthlyProcessingCount { get; set; }
        public decimal SuccessRate { get; set; }
        public decimal AverageProcessingTime { get; set; }

        // Financial data (role-restricted)
        public decimal? TotalMonthlyAmount { get; set; }
        public string? Currency { get; set; }
        public decimal? MonthlyVariance { get; set; }

        // Audit metrics (for Auditors)
        public int? PendingAudits { get; set; }
        public int? CompletedAudits { get; set; }
        public decimal? AuditEfficiency { get; set; }
    }

    public class RecentActivityViewModel
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconClass { get; set; }
        public string? BadgeClass { get; set; }
    }

    public class TemplateUsageChartViewModel
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<ChartSeriesViewModel> Series { get; set; } = new List<ChartSeriesViewModel>();
        public string Title { get; set; } = "Template Usage Analytics";
        public string Type { get; set; } = "line";
    }

    public class ProcessingTrendsChartViewModel
    {
        public List<string> Categories { get; set; } = new List<string>();
        public List<ChartSeriesViewModel> Series { get; set; } = new List<ChartSeriesViewModel>();
        public string Title { get; set; } = "Processing Trends";
        public string Type { get; set; } = "column";
    }

    public class DepartmentDistributionChartViewModel
    {
        public List<PieChartDataViewModel> Data { get; set; } = new List<PieChartDataViewModel>();
        public string Title { get; set; } = "Templates by Department";
        public string Type { get; set; } = "pie";
    }

    public class ChartSeriesViewModel
    {
        public string Name { get; set; } = string.Empty;
        public List<decimal> Data { get; set; } = new List<decimal>();
        public string? Color { get; set; }
    }

    public class PieChartDataViewModel
    {
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string? Color { get; set; }
    }

    public class DashboardQuickActionViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public string BadgeClass { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public string? RequiredRole { get; set; }
        public string? CssClass { get; set; }

    }

    public class AlertViewModel
    {
        public string Type { get; set; } = string.Empty; // success, warning, error, info
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsDismissible { get; set; } = true;
        public string? ActionUrl { get; set; }
        public string? ActionText { get; set; }
    }


}