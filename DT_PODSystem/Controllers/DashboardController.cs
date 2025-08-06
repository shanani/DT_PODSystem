using System;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Helpers;
using DT_PODSystem.Models.ViewModels;
using DT_PODSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DT_PODSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardStatisticsService _statisticsService;

        public DashboardController(IDashboardStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel
            {
                UserName = User.Identity?.Name ?? "User",
                UserRole = User.IsAdmin() ? "Admin" : User.IsInRole("Auditor") ? "Auditor" : "Viewer",
                CanViewFinancialData = User.IsAdmin() || User.IsInRole("Auditor")
            };

            // Load statistics based on user role
            model.Stats = await _statisticsService.GetDashboardStatsAsync(model.UserRole, model.CanViewFinancialData);

            // Load chart data
            model.TemplateUsageChart = await _statisticsService.GetTemplateUsageChartAsync();
            model.ProcessingTrendsChart = await _statisticsService.GetProcessingTrendsChartAsync();
            model.DepartmentChart = await _statisticsService.GetDepartmentDistributionChartAsync();
            model.MonthlyProcessingChart = await _statisticsService.GetMonthlyProcessingChartAsync();
            model.StatusDistributionChart = await _statisticsService.GetStatusDistributionChartAsync();

            // Load recent activities
            model.RecentActivities = await _statisticsService.GetRecentActivitiesAsync(10);

            // Load quick actions based on role
            model.QuickActions = await _statisticsService.GetQuickActionsAsync(model.UserRole);

            // Load alerts and notifications
            model.Alerts = await _statisticsService.GetAlertsAsync();

            // Load pending audits for auditors
            if (User.IsInRole("Auditor"))
            {
                model.PendingAudits = await _statisticsService.GetPendingAuditsAsync(User.Identity?.Name ?? "");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetChartData(string chartType, string period = "6months")
        {
            try
            {
                object data = chartType.ToLower() switch
                {
                    "templateusage" => await _statisticsService.GetTemplateUsageChartAsync(period),
                    "processingtrends" => await _statisticsService.GetProcessingTrendsChartAsync(period),
                    "monthlyprocessing" => await _statisticsService.GetMonthlyProcessingChartAsync(period),
                    "statusdistribution" => await _statisticsService.GetStatusDistributionChartAsync(period),
                    _ => null
                };

                if (data == null)
                    return BadRequest("Invalid chart type");

                return Json(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var userRole = User.IsAdmin() ? "Admin" : User.IsInRole("Auditor") ? "Auditor" : "Viewer";
                var canViewFinancialData = User.IsAdmin() || User.IsInRole("Auditor");

                var stats = await _statisticsService.GetDashboardStatsAsync(userRole, canViewFinancialData);

                return Json(new { success = true, stats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentActivities(int count = 10)
        {
            try
            {
                var activities = await _statisticsService.GetRecentActivitiesAsync(count);
                return Json(new { success = true, activities });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}