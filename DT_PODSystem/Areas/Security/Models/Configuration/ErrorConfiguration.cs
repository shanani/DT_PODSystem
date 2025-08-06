using System.Collections.Generic;

namespace DT_PODSystem.Areas.Security.Models.Configuration
{
    public class ErrorConfiguration
    {
        public string BasePath { get; set; } = "/Security/Error";
        public DefaultRedirects DefaultRedirects { get; set; } = new();
        public Dictionary<string, ErrorMessage> Messages { get; set; } = new();
        public bool EnableDetailedErrors { get; set; } = false;
        public bool LogErrors { get; set; } = true;
        public int RedirectAfterSeconds { get; set; } = 10;
    }

    public class DefaultRedirects
    {
        public string HomePage { get; set; } = "/";
        public string LoginPage { get; set; } = "/Security/Account/Login";
        public string DashboardPage { get; set; } = "/Dashboard";
        public string SupportEmail { get; set; } = "support@company.com";
        public string SupportPhone { get; set; } = "";
    }

    public class ErrorMessage
    {
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "fas fa-exclamation-triangle";
    }
}