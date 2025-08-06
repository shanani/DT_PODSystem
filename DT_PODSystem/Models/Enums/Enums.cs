

using System.ComponentModel;

namespace DT_PODSystem.Models.Enums
{
    /// <summary>
    /// Query status enumeration
    /// </summary>
    public enum QueryStatus
    {
        Draft = 0,
        Testing = 1,
        Active = 2,
        Archived = 3,
        Suspended = 4
    }
    public enum TemplatePriority
    {
        [Description("Low")]
        Low = 0,
        [Description("Medium")]
        Medium = 1,
        [Description("High")]
        High = 2,
        [Description("Critical")]
        Critical = 3
    }

    public enum TemplateStatus
    {
        [Description("Draft")]
        Draft = 0,
        [Description("Testing")]
        Testing = 1,
        [Description("Active")]
        Active = 2,
        [Description("Archived")]
        Archived = 3,
        [Description("Suspended")]
        Suspended = 4
    }

    public enum DataTypeEnum
    {
        [Description("Text")]
        String = 0,
        [Description("Number")]
        Number = 1,
        [Description("Date")]
        Date = 2,
        [Description("Currency")]
        Currency = 3,
        [Description("Percentage")]
        Percentage = 4,
        [Description("Boolean")]
        Boolean = 5
    }

    public enum AnchorType
    {
        [Description("Horizontal")]
        Horizontal = 0,
        [Description("Vertical")]
        Vertical = 1
    }

    public enum CanvasElementType
    {
        [Description("Field")]
        Field = 0,
        [Description("Variable")]
        Variable = 1,
        [Description("Constant")]
        Constant = 2,
        [Description("Operation")]
        Operation = 3,
        [Description("Function")]
        Function = 4,
        [Description("Output")]
        Output = 5
    }

    public enum AttachmentType
    {
        [Description("Original")]
        Original = 0,
        [Description("Sample")]
        Sample = 1,
        [Description("Reference")]
        Reference = 2
    }
}