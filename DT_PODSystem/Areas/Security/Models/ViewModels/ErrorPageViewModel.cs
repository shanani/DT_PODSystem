using System;

namespace DT_PODSystem.Areas.Security.Models.ViewModels
{
    public class ErrorPageViewModel
    {
        public int StatusCode { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string OriginalPath { get; set; }
        public string ReturnUrl { get; set; }
        public bool ShowDetails { get; set; }
        public Exception Exception { get; set; }
        public bool IsAuthenticated { get; set; }
        public string UserCode { get; set; }
        public string UserDisplayName { get; set; }
        public bool RequiresAdmin { get; set; }
        public bool RequiresSuperAdmin { get; set; }
        public string HomePage { get; set; }
        public string LoginPage { get; set; }
        public string DashboardPage { get; set; }
        public string SupportEmail { get; set; }
        public string SupportPhone { get; set; }
        public int RedirectAfterSeconds { get; set; }
    }
}