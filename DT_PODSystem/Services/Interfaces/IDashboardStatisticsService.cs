using System.Collections.Generic;
using System.Threading.Tasks;
using DT_PODSystem.Models.ViewModels;

namespace DT_PODSystem.Services.Interfaces
{
    public interface IDashboardStatisticsService
    {
        // Main statistics
        Task<DashboardStatsViewModel> GetDashboardStatsAsync(string userRole, bool canViewFinancialData);

        // Chart data methods
        Task<TemplateUsageChartViewModel> GetTemplateUsageChartAsync(string period = "6months");
        Task<ProcessingTrendsChartViewModel> GetProcessingTrendsChartAsync(string period = "6months");
        Task<DepartmentDistributionChartViewModel> GetDepartmentDistributionChartAsync();
        Task<MonthlyProcessingChartViewModel> GetMonthlyProcessingChartAsync(string period = "12months");
        Task<StatusDistributionChartViewModel> GetStatusDistributionChartAsync(string period = "current");

        // Activity and notifications
        Task<List<RecentActivityViewModel>> GetRecentActivitiesAsync(int count = 10);
        Task<List<DashboardQuickActionViewModel>> GetQuickActionsAsync(string userRole);
        Task<List<AlertViewModel>> GetAlertsAsync();
        Task<List<PendingAuditViewModel>> GetPendingAuditsAsync(string auditorUserName);

        // Performance metrics
        Task<ProcessingPerformanceViewModel> GetProcessingPerformanceAsync();
        Task<List<TopPerformingTemplateViewModel>> GetTopPerformingTemplatesAsync(int count = 5);
        Task<List<DepartmentPerformanceViewModel>> GetDepartmentPerformanceAsync();
    }
}