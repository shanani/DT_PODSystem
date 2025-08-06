using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// Vendor lookup for template associations
    /// </summary>
    public class Vendor : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;


        [StringLength(100)]
        public string? CompanyName { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [StringLength(100)]
        public string? ContactEmail { get; set; }

        [StringLength(20)]
        public string? ContactPhone { get; set; }

        [StringLength(50)]
        public string? TaxNumber { get; set; }

        [StringLength(50)]
        public string? CommercialRegister { get; set; }

        public bool IsApproved { get; set; } = false;

        public DateTime? ApprovalDate { get; set; }

        [StringLength(100)]
        public string? ApprovedBy { get; set; }

        // Navigation properties
        public virtual ICollection<PdfTemplate> Templates { get; set; } = new List<PdfTemplate>();
    }
}