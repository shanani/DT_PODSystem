// Models/DTOs/ReportDTOs.cs
using System;
using System.Collections.Generic;

namespace DT_PODSystem.Models.DTOs
{
    public class ReportFiltersDto
    {
        public List<int> Categories { get; set; } = new();
        public List<int> Vendors { get; set; } = new();
        public List<int> Departments { get; set; } = new();
        public List<string> Status { get; set; } = new();
        public string DateRange { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ExportRequestDto
    {
        public ReportFiltersDto Filters { get; set; } = new();
        public string Format { get; set; } = "excel"; // excel or pdf
    }

    public class SummaryStatsDto
    {
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public decimal SuccessRate { get; set; }
        public string AverageProcessingTime { get; set; } = string.Empty;
        public int TotalCategories { get; set; }
        public int TotalVendors { get; set; }
    }

    public class MonthlyTrendsDto
    {
        public string[] Labels { get; set; } = Array.Empty<string>();
        public int[] TotalProcessed { get; set; } = Array.Empty<int>();
        public int[] Successful { get; set; } = Array.Empty<int>();
        public int[] Failed { get; set; } = Array.Empty<int>();
    }

    public class CategoryDistributionDto
    {
        public string[] Labels { get; set; } = Array.Empty<string>();
        public int[] Values { get; set; } = Array.Empty<int>();
    }

    public class VendorPerformanceDto
    {
        public string[] VendorNames { get; set; } = Array.Empty<string>();
        public decimal[] SuccessRates { get; set; } = Array.Empty<decimal>();
        public int[] TotalDocuments { get; set; } = Array.Empty<int>();
    }

    public class ProcessingTimeDto
    {
        public string[] TimeLabels { get; set; } = Array.Empty<string>();
        public decimal[] AverageTime { get; set; } = Array.Empty<decimal>();
        public int[] DocumentVolume { get; set; } = Array.Empty<int>();
    }

    public class DepartmentSuccessDto
    {
        public string[] DepartmentNames { get; set; } = Array.Empty<string>();
        public decimal[] SuccessRates { get; set; } = Array.Empty<decimal>();
    }

    public class LookupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ChartSeriesDto
    {
        public string Name { get; set; } = string.Empty;
        public object[] Data { get; set; } = Array.Empty<object>();
    }

    public class ChartDataDto
    {
        public ChartSeriesDto[] Series { get; set; } = Array.Empty<ChartSeriesDto>();
        public string[] Categories { get; set; } = Array.Empty<string>();
    }
}

