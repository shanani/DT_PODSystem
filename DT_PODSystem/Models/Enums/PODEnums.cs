namespace DT_PODSystem.Models.Enums
{
    /// <summary>
    /// POD automation status - defines how the POD is processed
    /// </summary>
    public enum AutomationStatus
    {
        PDF = 1,                    // PDF processing only
        ManualEntryWorkflow = 2,    // Manual Entry + Workflow
        FullyAutomated = 3         // Fully Automated
    }

    /// <summary>
    /// Processing frequency for POD
    /// </summary>
    public enum ProcessingFrequency
    {
        Monthly = 1,      // Default
        Quarterly = 2,    // Every 3 months
        HalfYearly = 3,   // Every 6 months
        Yearly = 4        // Annual
    }

    /// <summary>
    /// POD status - business lifecycle status
    /// </summary>
    public enum PODStatus
    {
        Draft = 1,
        PendingApproval = 2,
        Approved = 3,
        Active = 4,
        Suspended = 5,
        Archived = 6,
        Cancelled = 7
    }

    /// <summary>
    /// Types of official documents that can be attached to POD
    /// </summary>
    public enum PODAttachmentType
    {
        Contract = 1,           // Main contract document
        PurchaseOrder = 2,      // PO document
        Specification = 3,      // Technical specifications
        Amendment = 4,          // Contract amendments
        Invoice = 5,           // Sample invoices
        Certificate = 6,       // Certificates and compliance docs
        Correspondence = 7,    // Email/letter communications
        Other = 99            // Other official documents
    }
}