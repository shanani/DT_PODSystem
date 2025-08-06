using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace DT_PODSystem.Models.ViewModels
{
    /// <summary>
    /// POD filters for list view
    /// </summary>
    public class PODFiltersViewModel
    {
        public string? SearchTerm { get; set; }
        public PODStatus? Status { get; set; }
        public AutomationStatus? AutomationStatus { get; set; }
        public ProcessingFrequency? Frequency { get; set; }
        public int? CategoryId { get; set; }
        public int? DepartmentId { get; set; }
        public int? VendorId { get; set; }
        public bool? RequiresApproval { get; set; }
        public bool? IsFinancialData { get; set; }
        public DateTime? CreatedFromDate { get; set; }
        public DateTime? CreatedToDate { get; set; }

        // Pagination
        public PaginationViewModel Pagination { get; set; } = new PaginationViewModel();

        // Filter options for dropdowns
        public List<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AutomationStatusOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> FrequencyOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> DepartmentOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> VendorOptions { get; set; } = new List<SelectListItem>();
    }

    /// <summary>
    /// POD list view model
    /// </summary>
    public class PODListViewModel
    {
        public List<PODListItemDto> PODs { get; set; } = new List<PODListItemDto>();
        public PODFiltersViewModel Filters { get; set; } = new PODFiltersViewModel();
        public PaginationViewModel Pagination { get; set; } = new PaginationViewModel();

        // User permissions
        public string UserRole { get; set; } = string.Empty;
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

}
