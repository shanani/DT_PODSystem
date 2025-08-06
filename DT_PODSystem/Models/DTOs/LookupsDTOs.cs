using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DT_PODSystem.Models.DTOs
{
    /// <summary>
    /// Category Data Transfer Object
    /// </summary>
    public class CategoryDto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; } = 1;

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// General Directorate Data Transfer Object
    /// </summary>
    public class GeneralDirectorateDto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;


        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? ManagerName { get; set; }


        [EmailAddress]
        public string? ContactEmail { get; set; }

        [StringLength(20)]
        public string? ContactPhone { get; set; }

        public int DisplayOrder { get; set; } = 1;

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Department Data Transfer Object
    /// </summary>
    public class DepartmentDto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;



        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public int GeneralDirectorateId { get; set; }

        public string GeneralDirectorateName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ManagerName { get; set; }


        [EmailAddress]
        public string? ContactEmail { get; set; }

        [StringLength(20)]
        public string? ContactPhone { get; set; }

        public int DisplayOrder { get; set; } = 1;

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Vendor Data Transfer Object
    /// </summary>
    public class VendorDto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? CompanyName { get; set; }

        [StringLength(100)]
        public string? ContactPerson { get; set; }


        [EmailAddress]
        public string? ContactEmail { get; set; }

        [StringLength(20)]
        public string? ContactPhone { get; set; }

        [StringLength(50)]
        public string? TaxNumber { get; set; }

        [StringLength(50)]
        public string? CommercialRegister { get; set; }

        public bool IsApproved { get; set; } = false;

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Organizational Hierarchy DTO for combined lookups
    /// </summary>
    public class OrganizationalHierarchyDto
    {
        public List<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
        public List<GeneralDirectorateDto> GeneralDirectorates { get; set; } = new List<GeneralDirectorateDto>();
        public List<DepartmentDto> Departments { get; set; } = new List<DepartmentDto>();
        public List<VendorDto> Vendors { get; set; } = new List<VendorDto>();

    }

    /// <summary>
    /// Lookup Usage Details DTO for dependency checking
    /// </summary>
    public class LookupUsageDetailsDto
    {
        public string LookupType { get; set; } = string.Empty;
        public int LookupId { get; set; }
        public string LookupName { get; set; } = string.Empty;
        public bool IsInUse { get; set; }
        public bool CanBeDeleted { get; set; }
        public int TotalUsageCount { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, int> UsageBreakdown { get; set; } = new Dictionary<string, int>();


    }

    /// <summary>
    /// Lookup Statistics DTO for dashboard metrics
    /// </summary>
    public class LookupStatisticsDto
    {
        public int TotalCategories { get; set; }
        public int ActiveCategories { get; set; }
        public int TotalVendors { get; set; }
        public int ActiveVendors { get; set; }
        public int ApprovedVendors { get; set; }
        public int TotalDepartments { get; set; }
        public int ActiveDepartments { get; set; }
        public int TotalGeneralDirectorates { get; set; }
        public int ActiveGeneralDirectorates { get; set; }
        public int TotalTemplates { get; set; }
        public int ActiveTemplates { get; set; }


    }






}