// ✅ UPDATED: WorkerModels.cs - Added CalculationDetails for audit
namespace DT_PODSystemWorker.Models
{
    public class ExtractionResult
    {
        public bool Success { get; set; }
        public Dictionary<string, object> ExtractedFields { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
        public decimal CalibrationConfidence { get; set; } = 1.0m;

        // 🆕 ANCHOR CALIBRATION RESULTS
        public AnchorCalibrationResult? AnchorResults { get; set; }

        /// <summary>
        /// Confidence scores for each extracted field (0.0 to 1.0)
        /// </summary>
        public Dictionary<string, decimal>? FieldConfidences { get; set; }

        /// <summary>
        /// Any warnings during extraction
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Number of anchor points successfully calibrated
        /// </summary>
        public int CalibratedAnchors { get; set; }

        /// <summary>
        /// Whether coordinate calibration was applied
        /// </summary>
        public bool CoordinateCalibrationApplied { get; set; }


        /// <summary>
        /// Create successful result
        /// </summary>
        public static ExtractionResult CreateSuccess(
            Dictionary<string, object?> extractedFields,
            Dictionary<string, decimal>? confidences = null,
            List<string>? warnings = null)
        {
            return new ExtractionResult
            {
                Success = true,
                ExtractedFields = extractedFields,
                FieldConfidences = confidences,
                Warnings = warnings ?? new List<string>()
            };
        }

        /// <summary>
        /// Create failure result
        /// </summary>
        public static ExtractionResult CreateFailure(string errorMessage)
        {
            return new ExtractionResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                ExtractedFields = new Dictionary<string, object?>()
            };
        }

    }

    public class AnchorCalibrationResult
    {
        public decimal Confidence { get; set; } = 1.0m;
        public int AnchorsFound { get; set; } = 0;
        public int AnchorsTotal { get; set; } = 0;
        public int AnchorsMatched { get; set; } = 0;
        public decimal CoordinateOffsetX { get; set; } = 0.0m;
        public decimal CoordinateOffsetY { get; set; } = 0.0m;
        public decimal CoordinateScaleX { get; set; } = 1.0m;
        public decimal CoordinateScaleY { get; set; } = 1.0m;
        public bool CoordinatesAdjusted { get; set; } = false;
        public string Details { get; set; } = string.Empty;
    }

    /// <summary>
    /// ✅ File processing information
    /// </summary>
    public class FileProcessInfo
    {
        public int TemplateId { get; set; }
        public string PeriodId { get; set; } = string.Empty; // yyyyMM format
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public long FileSizeBytes { get; set; }
        public string? Department { get; set; }
        public string? TemplateName { get; set; }
        public string Category { get; internal set; }
        public string Vendor { get; internal set; }
        public string PODName { get; internal set; }
        public string PODCode { get; internal set; }
        public string AutomationStatus { get; internal set; }
        public string ProcessingFrequency { get; internal set; }
        public bool IsFinancialData { get; internal set; }
        public bool RequiresApproval { get; internal set; }
    }




    /// <summary>
    /// Result of formula calculations with audit trail
    /// </summary>
    public class CalculationResult
    {
        public bool Success { get; set; }
        public Dictionary<string, object> CalculatedOutputs { get; set; } = new();
        public Dictionary<string, string> CalculationDetails { get; set; } = new(); // ✅ NEW: Processed formulas for audit
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Template matching result for file discovery
    /// </summary>
    public class TemplateMatchResult
    {
        public bool IsMatch { get; set; }
        public string PeriodId { get; set; } = string.Empty;
    }

    public class WorkerSettings
    {
        public string RootFolderPath { get; set; } = string.Empty;
        public string ProcessedFolderPath { get; set; } = string.Empty;
        public string ErrorFolderPath { get; set; } = string.Empty;
        public int ProcessingIntervalMinutes { get; set; } = 5;
        public int MaxConcurrentFiles { get; set; } = 10;
        public string[] SupportedExtensions { get; set; } = new[] { ".pdf" };
        public int MaxFileSizeMB { get; set; } = 50;
    }

    public class FileOrganizationSettings
    {
        public bool CreateCategoryFolders { get; set; } = true;
        public bool CreateVendorFolders { get; set; } = true;
        public bool CreateMonthlyFolders { get; set; } = true;
        public string FolderStructure { get; set; } = "{Category}/{Vendor}/{PeriodId}";
    }
}