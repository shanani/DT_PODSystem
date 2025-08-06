using System.Collections.Generic;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.Entities;

namespace DT_PODSystem.Models.ViewModels
{
    public class LookupsViewModel
    {
        public string PageTitle { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;

        // Collections for different entity types
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<Vendor> Vendors { get; set; } = new List<Vendor>();
        public List<DepartmentDto> Departments { get; set; } = new List<DepartmentDto>();
        public List<GeneralDirectorateDto> GeneralDirectorates { get; set; } = new List<GeneralDirectorateDto>();

        // Statistics
        public LookupStatisticsDto Statistics { get; set; } = new LookupStatisticsDto();

        // UI Configuration
        public bool ShowImportExport { get; set; } = true;
        public bool AllowCreate { get; set; } = true;
        public bool AllowEdit { get; set; } = true;
        public bool AllowDelete { get; set; } = true;
        public bool ShowUsageDetails { get; set; } = true;
    }


}