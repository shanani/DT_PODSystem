//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using DT_PODSystem.Data;
//using DT_PODSystem.Models.Entities;
//using DT_PODSystem.Models.Enums;
//using DT_PODSystem.Models.ViewModels;
//using DT_PODSystem.Services.Interfaces;

//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;

//namespace DT_PODSystem.Services.Implementation
//{
//    public class DashboardStatisticsService : IDashboardStatisticsService
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly ILogger<DashboardStatisticsService> _logger;

//        public DashboardStatisticsService(ApplicationDbContext context, ILogger<DashboardStatisticsService> logger)
//        {
//            _context = context;
//            _logger = logger;
//        }

//        public async Task<List<AlertViewModel>> GetAlertsAsync()
//        {
//            var alerts = new List<AlertViewModel>();

//            try
//            {
//                var currentMonth = DateTime.Now.ToString("yyyyMM");

//                // Check for high failure rate
//                var totalProcessed = await _context.ProcessedFiles.CountAsync(pf => pf.PeriodId == currentMonth);
//                var failedCount = await _context.ProcessedFiles.CountAsync(pf => pf.PeriodId == currentMonth && pf.Status == "Failed");

//                if (totalProcessed > 0 && (decimal)failedCount / totalProcessed > 0.1m) // > 10% failure rate
//                {
//                    alerts.Add(new AlertViewModel
//                    {
//                        Type = "warning",
//                        Title = "High Failure Rate",
//                        Message = $"Current month failure rate is {Math.Round((decimal)failedCount / totalProcessed * 100, 1)}%",
//                        Timestamp = DateTime.Now,
//                        ActionUrl = "/ProcessingOutput?filter=failed"
//                    });
//                }

//                // Check for templates without recent usage
//                var unusedTemplates = await _context.PdfTemplates
//                    .Where(t => t.Status == TemplateStatus.Active && !t.ProcessedFiles.Any(po =>
//                        po.ProcessedDate >= DateTime.Now.AddMonths(-3)))
//                    .CountAsync();

//                if (unusedTemplates > 0)
//                {
//                    alerts.Add(new AlertViewModel
//                    {
//                        Type = "info",
//                        Title = "Unused Templates",
//                        Message = $"{unusedTemplates} active templates haven't been used in 3 months",
//                        Timestamp = DateTime.Now,
//                        ActionUrl = "/Template?filter=unused"
//                    });
//                }

//                // Check for pending approvals older than 7 days
//                var oldPendingCount = await _context.ProcessedFiles
//                    .CountAsync(pf => pf.NeedApproval && pf.Status != "Approved" &&
//                                    pf.CreatedDate < DateTime.Now.AddDays(-7));

//                if (oldPendingCount > 0)
//                {
//                    alerts.Add(new AlertViewModel
//                    {
//                        Type = "error",
//                        Title = "Overdue Approvals",
//                        Message = $"{oldPendingCount} documents pending approval for over 7 days",
//                        Timestamp = DateTime.Now,
//                        ActionUrl = "/ProcessingOutput?filter=overdue"
//                    });
//                }

//                return alerts;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting alerts");
//                return alerts;
//            }
//        }

//        public async Task<List<PendingAuditViewModel>> GetPendingAuditsAsync(string auditorUserName)
//        {
//            try
//            {
//                var pendingAudits = await _context.ProcessedFiles
//                    .Include(pf => pf.Template)
//                    .Include(pf => pf.Template.Department)
//                    .Where(pf => pf.NeedApproval && pf.Status != "Approved")
//                    .OrderBy(pf => pf.CreatedDate)
//                    .Take(10)
//                    .ToListAsync();

//                return pendingAudits.Select(pf => new PendingAuditViewModel
//                {
//                    Id = pf.Id,
//                    FileName = pf.OriginalFileName,
//                    TemplateName = pf.Template.Name,
//                    DepartmentName = pf.Template.Department.Name,
//                    ProcessedDate = pf.ProcessedDate,
//                    DaysWaiting = (DateTime.Now - pf.ProcessedDate).Days,
//                    HasFinancialInfo = pf.HasFinancialInfo,
//                    Priority = pf.HasFinancialInfo ? "High" : "Normal",
//                    PriorityBadgeClass = pf.HasFinancialInfo ? "bg-danger" : "bg-warning"
//                }).ToList();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting pending audits for {AuditorUserName}", auditorUserName);
//                return new List<PendingAuditViewModel>();
//            }
//        }

//        public async Task<ProcessingPerformanceViewModel> GetProcessingPerformanceAsync()
//        {
//            try
//            {
//                var currentMonth = DateTime.Now.ToString("yyyyMM");
//                var previousMonth = DateTime.Now.AddMonths(-1).ToString("yyyyMM");

//                var currentStats = await GetMonthlyPerformanceStats(currentMonth);
//                var previousStats = await GetMonthlyPerformanceStats(previousMonth);

//                return new ProcessingPerformanceViewModel
//                {
//                    CurrentMonth = new MonthlyPerformanceViewModel
//                    {
//                        TotalProcessed = currentStats.TotalProcessed,
//                        SuccessCount = currentStats.SuccessCount,
//                        SuccessRate = currentStats.SuccessRate
//                    },
//                    PreviousMonth = new MonthlyPerformanceViewModel
//                    {
//                        TotalProcessed = previousStats.TotalProcessed,
//                        SuccessCount = previousStats.SuccessCount,
//                        SuccessRate = previousStats.SuccessRate
//                    },
//                    Trend = CalculateTrend(currentStats.SuccessRate, previousStats.SuccessRate)
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting processing performance");
//                return new ProcessingPerformanceViewModel();
//            }
//        }

//        public async Task<List<TopPerformingTemplateViewModel>> GetTopPerformingTemplatesAsync(int count = 5)
//        {
//            try
//            {
//                var templatePerformance = await _context.ProcessedFiles
//                    .Include(pf => pf.Template)
//                    .Include(pf => pf.ProcessedFields)
//                    .GroupBy(pf => new { pf.TemplateId, pf.Template.Name })
//                    .Select(g => new
//                    {
//                        TemplateId = g.Key.TemplateId,
//                        TemplateName = g.Key.Name,
//                        TotalProcessed = g.Count(),
//                        SuccessCount = g.Count(pf => pf.Status == "Success"),
//                        AverageConfidence = g.SelectMany(pf => pf.ProcessedFields)
//                                           .Where(po => po.ExtractionConfidence > 0)
//                                           .DefaultIfEmpty()
//                                           .Average(po => po.ExtractionConfidence)
//                    })
//                    .Where(x => x.TotalProcessed >= 10) // Only templates with sufficient data
//                    .OrderByDescending(x => x.SuccessCount / (decimal)x.TotalProcessed)
//                    .ThenByDescending(x => x.AverageConfidence)
//                    .Take(count)
//                    .ToListAsync();

//                return templatePerformance.Select(tp => new TopPerformingTemplateViewModel
//                {
//                    TemplateId = tp.TemplateId,
//                    TemplateName = tp.TemplateName,
//                    TotalProcessed = tp.TotalProcessed,
//                    SuccessRate = Math.Round((decimal)tp.SuccessCount / tp.TotalProcessed * 100, 1),
//                    AverageConfidence = Math.Round(tp.AverageConfidence, 1)
//                }).ToList();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting top performing templates");
//                return new List<TopPerformingTemplateViewModel>();
//            }
//        }

//        public async Task<List<DepartmentPerformanceViewModel>> GetDepartmentPerformanceAsync()
//        {
//            try
//            {
//                var departmentStats = await _context.ProcessedFiles
//                    .Include(pf => pf.Template)
//                    .Include(pf => pf.Template.Department)
//                    .GroupBy(pf => new { pf.Template.Department.Id, pf.Template.Department.Name })
//                    .Select(g => new
//                    {
//                        DepartmentId = g.Key.Id,
//                        DepartmentName = g.Key.Name,
//                        TotalProcessed = g.Count(),
//                        SuccessCount = g.Count(pf => pf.Status == "Success"),
//                        TemplateCount = g.Select(pf => pf.TemplateId).Distinct().Count()
//                    })
//                    .OrderByDescending(x => x.TotalProcessed)
//                    .ToListAsync();

//                return departmentStats.Select(ds => new DepartmentPerformanceViewModel
//                {
//                    DepartmentId = ds.DepartmentId,
//                    DepartmentName = ds.DepartmentName,
//                    TemplateCount = ds.TemplateCount,
//                    TotalProcessed = ds.TotalProcessed,
//                    SuccessRate = ds.TotalProcessed > 0 ?
//                        Math.Round((decimal)ds.SuccessCount / ds.TotalProcessed * 100, 1) : 0
//                }).ToList();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting department performance");
//                return new List<DepartmentPerformanceViewModel>();
//            }
//        }

//        // Helper methods
//        private string GetStatusIcon(string status)
//        {
//            return status switch
//            {
//                "Success" => "fa fa-check-circle text-success",
//                "Failed" => "fa fa-times-circle text-danger",
//                "Fuzzy" => "fa fa-exclamation-triangle text-warning",
//                "Pending" => "fa fa-clock text-info",
//                "Approved" => "fa fa-thumbs-up text-success",
//                _ => "fa fa-question-circle text-muted"
//            };
//        }

//        private string GetStatusBadgeClass(string status)
//        {
//            return status switch
//            {
//                "Success" => "bg-success",
//                "Failed" => "bg-danger",
//                "Fuzzy" => "bg-warning",
//                "Pending" => "badge-info",
//                "Approved" => "bg-success",
//                _ => "bg-secondary"
//            };
//        }

//        private async Task<MonthlyPerformanceStats> GetMonthlyPerformanceStats(string monthPeriod)
//        {
//            var files = await _context.ProcessedFiles
//                .Where(pf => pf.PeriodId == monthPeriod)
//                .ToListAsync();

//            var successCount = files.Count(f => f.Status == "Success");
//            var totalProcessed = files.Count;

//            return new MonthlyPerformanceStats
//            {
//                TotalProcessed = totalProcessed,
//                SuccessCount = successCount,
//                SuccessRate = totalProcessed > 0 ?
//                    Math.Round((decimal)successCount / totalProcessed * 100, 1) : 0m
//            };
//        }

//        private string CalculateTrend(decimal current, decimal previous)
//        {
//            if (previous == 0) return "stable";
//            return current > previous ? "up" : current < previous ? "down" : "stable";
//        }

//        // Helper classes for internal use
//        private class MonthlyPerformanceStats
//        {
//            public int TotalProcessed { get; set; }
//            public int SuccessCount { get; set; }
//            public decimal SuccessRate { get; set; }
//        }

//        public async Task<TemplateUsageChartViewModel> GetTemplateUsageChartAsync(string period = "6months")
//        {
//            try
//            {
//                var monthsCount = period switch
//                {
//                    "3months" => 3,
//                    "12months" => 12,
//                    _ => 6
//                };

//                var currentDate = DateTime.Now;
//                var months = Enumerable.Range(0, monthsCount)
//                    .Select(i => currentDate.AddMonths(-i).ToString("yyyyMM"))
//                    .Reverse()
//                    .ToList();

//                var templateCreationData = new List<decimal>();
//                var templateUsageData = new List<decimal>();

//                foreach (var month in months)
//                {
//                    var monthDate = DateTime.ParseExact(month + "01", "yyyyMMdd", null);

//                    // Templates created in this month
//                    var created = await _context.PdfTemplates
//                        .CountAsync(t => t.CreatedDate.Year == monthDate.Year &&
//                                       t.CreatedDate.Month == monthDate.Month);

//                    // Templates used for processing in this month
//                    var used = await _context.ProcessedFiles
//                        .CountAsync(pf => pf.PeriodId == month);

//                    templateCreationData.Add(created);
//                    templateUsageData.Add(used);
//                }

//                return new TemplateUsageChartViewModel
//                {
//                    Labels = months.Select(m => DateTime.ParseExact(m + "01", "yyyyMMdd", null).ToString("MMM yyyy")).ToList(),
//                    Series = new List<ChartSeriesViewModel>
//                    {
//                        new ChartSeriesViewModel
//                        {
//                            Name = "Templates Created",
//                            Data = templateCreationData,
//                            Color = "#A54EE1"
//                        },
//                        new ChartSeriesViewModel
//                        {
//                            Name = "Templates Used",
//                            Data = templateUsageData,
//                            Color = "#4F008C"
//                        }
//                    },
//                    Title = "Template Usage Analytics",
//                    Type = "line"
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting template usage chart data");
//                return new TemplateUsageChartViewModel();
//            }
//        }

//        public async Task<ProcessingTrendsChartViewModel> GetProcessingTrendsChartAsync(string period = "6months")
//        {
//            try
//            {
//                var monthsCount = period switch
//                {
//                    "3months" => 3,
//                    "12months" => 12,
//                    _ => 6
//                };

//                var months = Enumerable.Range(0, monthsCount)
//                    .Select(i => DateTime.Now.AddMonths(-i).ToString("yyyyMM"))
//                    .Reverse()
//                    .ToList();

//                var successData = new List<decimal>();
//                var failedData = new List<decimal>();
//                var fuzzyData = new List<decimal>();

//                foreach (var month in months)
//                {
//                    var successCount = await _context.ProcessedFiles
//                        .CountAsync(pf => pf.PeriodId == month && pf.Status == "Success");
//                    var failedCount = await _context.ProcessedFiles
//                        .CountAsync(pf => pf.PeriodId == month && pf.Status == "Failed");
//                    var fuzzyCount = await _context.ProcessedFiles
//                        .CountAsync(pf => pf.PeriodId == month && pf.Status == "Fuzzy");

//                    successData.Add(successCount);
//                    failedData.Add(failedCount);
//                    fuzzyData.Add(fuzzyCount);
//                }

//                return new ProcessingTrendsChartViewModel
//                {
//                    Categories = months.Select(m => DateTime.ParseExact(m + "01", "yyyyMMdd", null).ToString("MMM")).ToList(),
//                    Series = new List<ChartSeriesViewModel>
//                    {
//                        new ChartSeriesViewModel
//                        {
//                            Name = "Successful",
//                            Data = successData,
//                            Color = "#00C48C"
//                        },
//                        new ChartSeriesViewModel
//                        {
//                            Name = "Failed",
//                            Data = failedData,
//                            Color = "#FF375E"
//                        },
//                        new ChartSeriesViewModel
//                        {
//                            Name = "Fuzzy",
//                            Data = fuzzyData,
//                            Color = "#EF7945"
//                        }
//                    },
//                    Title = "Processing Trends",
//                    Type = "column"
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting processing trends chart data");
//                return new ProcessingTrendsChartViewModel();
//            }
//        }

//        public async Task<DepartmentDistributionChartViewModel> GetDepartmentDistributionChartAsync()
//        {
//            try
//            {
//                var departmentStats = await _context.PdfTemplates
//                    .Include(t => t.Department)
//                    .GroupBy(t => new { t.Department.Id, t.Department.Name })
//                    .Select(g => new
//                    {
//                        Name = g.Key.Name,
//                        Count = g.Count()
//                    })
//                    .OrderByDescending(x => x.Count)
//                    .Take(10)
//                    .ToListAsync();

//                var stcColors = new[] { "#A54EE1", "#4F008C", "#00C48C", "#FF375E", "#EF7945", "#FFE923", "#1BCED8", "#ADB5BD" };

//                return new DepartmentDistributionChartViewModel
//                {
//                    Data = departmentStats.Select((item, index) => new PieChartDataViewModel
//                    {
//                        Name = item.Name,
//                        Value = item.Count,
//                        Color = stcColors[index % stcColors.Length]
//                    }).ToList(),
//                    Title = "Templates by Department",
//                    Type = "pie"
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting department distribution chart data");
//                return new DepartmentDistributionChartViewModel();
//            }
//        }

//        public async Task<MonthlyProcessingChartViewModel> GetMonthlyProcessingChartAsync(string period = "12months")
//        {
//            try
//            {
//                var monthsCount = period switch
//                {
//                    "6months" => 6,
//                    "24months" => 24,
//                    _ => 12
//                };

//                var months = Enumerable.Range(0, monthsCount)
//                    .Select(i => DateTime.Now.AddMonths(-i).ToString("yyyyMM"))
//                    .Reverse()
//                    .ToList();

//                var processedCounts = new List<decimal>();
//                var avgConfidences = new List<decimal>();

//                foreach (var month in months)
//                {
//                    var monthlyFiles = await _context.ProcessedFiles
//                        .Include(pf => pf.ProcessedFields)
//                        .Where(pf => pf.PeriodId == month)
//                        .ToListAsync();

//                    processedCounts.Add(monthlyFiles.Count);

//                    var avgConfidence = monthlyFiles.Any() ?
//                        monthlyFiles.SelectMany(pf => pf.ProcessedFields)
//                                  .Where(po => po.ExtractionConfidence > 0)
//                                  .DefaultIfEmpty()
//                                  .Average(po => po?.ExtractionConfidence ?? 0m) : 0m;

//                    avgConfidences.Add(Math.Round(avgConfidence, 1));
//                }

//                return new MonthlyProcessingChartViewModel
//                {
//                    Categories = months.Select(m => DateTime.ParseExact(m + "01", "yyyyMMdd", null).ToString("MMM yyyy")).ToList(),
//                    Series = new List<ChartSeriesViewModel>
//                    {
//                        new ChartSeriesViewModel
//                        {
//                            Name = "Files Processed",
//                            Data = processedCounts,
//                            Color = "#A54EE1"
//                        }
//                    },
//                    ConfidenceSeries = new ChartSeriesViewModel
//                    {
//                        Name = "Avg Confidence %",
//                        Data = avgConfidences,
//                        Color = "#00C48C"
//                    },
//                    Title = "Monthly Processing Volume & Confidence",
//                    Type = "line"
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting monthly processing chart data");
//                return new MonthlyProcessingChartViewModel();
//            }
//        }

//        public async Task<StatusDistributionChartViewModel> GetStatusDistributionChartAsync(string period = "current")
//        {
//            try
//            {
//                var query = _context.ProcessedFiles.AsQueryable();

//                if (period == "current")
//                {
//                    var currentMonth = DateTime.Now.ToString("yyyyMM");
//                    query = query.Where(pf => pf.PeriodId == currentMonth);
//                }

//                var statusStats = await query
//                    .GroupBy(pf => pf.Status)
//                    .Select(g => new { Status = g.Key, Count = g.Count() })
//                    .ToListAsync();

//                var statusColors = new Dictionary<string, string>
//                {
//                    ["Success"] = "#00C48C",
//                    ["Failed"] = "#FF375E",
//                    ["Fuzzy"] = "#EF7945",
//                    ["Pending"] = "#FFE923",
//                    ["Approved"] = "#1BCED8"
//                };

//                return new StatusDistributionChartViewModel
//                {
//                    Data = statusStats.Select(item => new PieChartDataViewModel
//                    {
//                        Name = item.Status,
//                        Value = item.Count,
//                        Color = statusColors.ContainsKey(item.Status) ? statusColors[item.Status] : "#ADB5BD"
//                    }).ToList(),
//                    Title = period == "current" ? "Current Month Status Distribution" : "Overall Status Distribution",
//                    Type = "pie"
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting status distribution chart data");
//                return new StatusDistributionChartViewModel();
//            }
//        }

//        public async Task<List<RecentActivityViewModel>> GetRecentActivitiesAsync(int count = 10)
//        {
//            try
//            {
//                var recentFiles = await _context.ProcessedFiles
//                    .Include(pf => pf.Template)
//                    .OrderByDescending(pf => pf.ProcessedDate)
//                    .Take(count)
//                    .ToListAsync();

//                return recentFiles.Select(pf => new RecentActivityViewModel
//                {
//                    Timestamp = pf.ProcessedDate,
//                    Action = "File Processed",
//                    EntityType = "Document",
//                    EntityName = pf.OriginalFileName,
//                    UserName = "System",
//                    Status = pf.Status,
//                    Description = $"Template: {pf.Template.Name}",
//                    IconClass = GetStatusIcon(pf.Status),
//                    BadgeClass = GetStatusBadgeClass(pf.Status)
//                }).ToList();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting recent activities");
//                return new List<RecentActivityViewModel>();
//            }
//        }

//        public async Task<List<DashboardQuickActionViewModel>> GetQuickActionsAsync(string userRole)
//        {
//            var actions = new List<DashboardQuickActionViewModel>();

//            try
//            {
//                if (userRole == "Admin")
//                {
//                    actions.AddRange(new[]
//                    {
//                        new DashboardQuickActionViewModel
//                        {
//                            Title = "Create Template",
//                            Description = "Start the template wizard",
//                            Url = "/Template/Wizard",
//                            IconClass = "fa fa-plus-circle",
//                            BadgeClass = "btn-primary"
//                        },
//                        new DashboardQuickActionViewModel
//                        {
//                            Title = "Manage Lookups",
//                            Description = "Categories, Departments, Vendors",
//                            Url = "/Lookups",
//                            IconClass = "fa fa-cogs",
//                            BadgeClass = "btn-secondary"
//                        },
//                        new DashboardQuickActionViewModel
//                        {
//                            Title = "View Processing Outputs",
//                            Description = "Review monthly results",
//                            Url = "/ProcessingOutput",
//                            IconClass = "fa fa-chart-bar",
//                            BadgeClass = "btn-success"
//                        }
//                    });

//                    // Check for pending approvals
//                    var pendingCount = await _context.ProcessedFiles.CountAsync(pf => pf.NeedApproval);
//                    if (pendingCount > 0)
//                    {
//                        actions.Add(new DashboardQuickActionViewModel
//                        {
//                            Title = $"Pending Approvals ({pendingCount})",
//                            Description = "Documents requiring approval",
//                            Url = "/ProcessingOutput?filter=needsapproval",
//                            IconClass = "fa fa-exclamation-triangle",
//                            BadgeClass = "btn-warning"
//                        });
//                    }
//                }
//                else if (userRole == "Auditor")
//                {
//                    var pendingAudits = await _context.ProcessedFiles.CountAsync(pf => pf.NeedApproval && pf.Status != "Approved");

//                    actions.AddRange(new[]
//                    {
//                        new DashboardQuickActionViewModel
//                        {
//                            Title = $"Pending Audits ({pendingAudits})",
//                            Description = "Documents awaiting review",
//                            Url = "/ProcessingOutput?filter=pendingaudit",
//                            IconClass = "fa fa-clipboard-check",
//                            BadgeClass = "btn-primary"
//                        },
//                        new DashboardQuickActionViewModel
//                        {
//                            Title = "Audit History",
//                            Description = "Review completed audits",
//                            Url = "/ProcessingOutput?filter=audited",
//                            IconClass = "fa fa-history",
//                            BadgeClass = "btn-info"
//                        }
//                    });
//                }
//                else // Viewer
//                {
//                    actions.AddRange(new[]
//                    {
//                        new DashboardQuickActionViewModel
//                        {
//                            Title = "View Templates",
//                            Description = "Browse available templates",
//                            Url = "/Template",
//                            IconClass = "fa fa-file-alt",
//                            BadgeClass = "btn-primary"
//                        },
//                        new DashboardQuickActionViewModel
//                        {
//                            Title = "Processing Status",
//                            Description = "Check processing results",
//                            Url = "/ProcessingOutput",
//                            IconClass = "fa fa-chart-line",
//                            BadgeClass = "btn-info"
//                        }
//                    });
//                }

//                return actions;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting quick actions for role {UserRole}", userRole);
//                return actions;
//            }
//        }



//        public async Task<DashboardStatsViewModel> GetDashboardStatsAsync(string userRole, bool canViewFinancialData)
//        {
//            try
//            {
//                var stats = new DashboardStatsViewModel();
//                var currentMonth = DateTime.Now.ToString("yyyyMM");
//                var previousMonth = DateTime.Now.AddMonths(-1).ToString("yyyyMM");

//                // Template statistics
//                stats.TotalTemplates = await _context.PdfTemplates.CountAsync();
//                stats.ActiveTemplates = await _context.PdfTemplates.CountAsync(t => t.Status == TemplateStatus.Active);
//                stats.DraftTemplates = await _context.PdfTemplates.CountAsync(t => t.Status == TemplateStatus.Draft);
//                stats.ArchivedTemplates = await _context.PdfTemplates.CountAsync(t => t.Status == TemplateStatus.Archived);

//                // Processing statistics
//                var currentMonthProcessed = await _context.ProcessedFiles
//                    .CountAsync(pf => pf.PeriodId == currentMonth);
//                var currentMonthSuccessful = await _context.ProcessedFiles
//                    .CountAsync(pf => pf.PeriodId == currentMonth && pf.Status == "Success");

//                stats.MonthlyProcessingCount = currentMonthProcessed;
//                stats.SuccessRate = currentMonthProcessed > 0 ?
//                    Math.Round((decimal)currentMonthSuccessful / currentMonthProcessed * 100, 1) : 0;

//                // FIXED: Average processing time using EF.Functions
//                var avgProcessingTimeMs = await _context.ProcessedFiles
//                    .Where(pf => pf.PeriodId == currentMonth && pf.Status == "Success")
//                    .Select(pf => (double?)EF.Functions.DateDiffMillisecond(pf.CreatedDate, pf.ProcessedDate))
//                    .AverageAsync();

//                stats.AverageProcessingTime = avgProcessingTimeMs.HasValue ?
//                    Math.Round((decimal)(avgProcessingTimeMs.Value / 1000), 2) : 0; // Convert to seconds

//                // Financial data (role-restricted)
//                if (canViewFinancialData)
//                {
//                    var financialOutputs = await _context.ProcessedFields
//                        .Include(po => po.ProcessedFile)
//                        .Where(po => po.ProcessedFile.PeriodId == currentMonth &&
//                                   po.ProcessedFile.HasFinancialInfo &&
//                                   po.OutputDataType == "Currency")
//                        .ToListAsync();

//                    if (financialOutputs.Any())
//                    {
//                        stats.TotalMonthlyAmount = financialOutputs
//                            .Where(po => decimal.TryParse(po.OutputValue, out _))
//                            .Sum(po => decimal.Parse(po.OutputValue ?? "0"));

//                        stats.Currency = financialOutputs.FirstOrDefault()?.CurrencySymbol ?? "SAR";

//                        // Calculate variance vs previous month
//                        var previousMonthOutputs = await _context.ProcessedFields
//                            .Include(po => po.ProcessedFile)
//                            .Where(po => po.ProcessedFile.PeriodId == previousMonth &&
//                                       po.ProcessedFile.HasFinancialInfo &&
//                                       po.OutputDataType == "Currency")
//                            .ToListAsync();

//                        var previousMonthAmount = previousMonthOutputs
//                            .Where(po => !string.IsNullOrEmpty(po.OutputValue) && decimal.TryParse(po.OutputValue, out _))
//                            .Sum(po => decimal.Parse(po.OutputValue ?? "0"));

//                        if (previousMonthAmount > 0)
//                        {
//                            stats.MonthlyVariance = Math.Round(
//                                ((stats.TotalMonthlyAmount ?? 0) - previousMonthAmount) / previousMonthAmount * 100, 1);
//                        }
//                    }
//                }

//                // Audit metrics (for Auditors)
//                if (userRole == "Auditor")
//                {
//                    stats.PendingAudits = await _context.ProcessedFiles
//                        .CountAsync(pf => pf.NeedApproval && pf.Status != "Approved");

//                    // Get auditor's completed audits this month
//                    var auditedThisMonth = await _context.ProcessedFiles
//                        .CountAsync(pf => pf.Status == "Approved" &&
//                                        pf.ModifiedDate.HasValue &&
//                                        pf.ModifiedDate.Value.Month == DateTime.Now.Month);

//                    stats.CompletedAudits = auditedThisMonth;

//                    // Calculate audit efficiency (completed vs assigned)
//                    var totalAssigned = (decimal)(stats.PendingAudits + auditedThisMonth);
//                    stats.AuditEfficiency = totalAssigned > 0 ?
//                        Math.Round(auditedThisMonth / totalAssigned * 100, 1) : 0;
//                }

//                return stats;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting dashboard statistics");
//                return new DashboardStatsViewModel(); // Return empty stats on error
//            }
//        }



//    } // End of class
//}