using System;
using System.Collections.Generic;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.Enums;

namespace DT_PODSystem.Models.ViewModels
{
    /// <summary>
    /// PDF mapping wizard step 3 with visual field selection
    /// </summary>
    public class PdfMappingViewModel
    {
        // Template context
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;

        // PDF file information
        public string PdfFileName { get; set; } = string.Empty;
        public string PdfFilePath { get; set; } = string.Empty;
        public int TotalPages { get; set; }
        public string? PdfVersion { get; set; }
        public bool HasFormFields { get; set; }

        // Current view state
        public int CurrentPage { get; set; } = 1;
        public decimal ZoomLevel { get; set; } = 1.0m;
        public decimal MinZoom { get; set; } = 0.25m;
        public decimal MaxZoom { get; set; } = 3.0m;
        public decimal ZoomStep { get; set; } = 0.25m;

        // PDF rendering dimensions
        public int PdfWidth { get; set; }
        public int PdfHeight { get; set; }
        public int ViewportWidth { get; set; }
        public int ViewportHeight { get; set; }

        // Field mappings
        public List<FieldMappingDto> FieldMappings { get; set; } = new List<FieldMappingDto>();
        public FieldMappingDto? SelectedField { get; set; }
        public bool IsEditingField { get; set; }

        // Available data types
        public List<DataTypeOptionViewModel> DataTypeOptions { get; set; } = new List<DataTypeOptionViewModel>();

        // Mapping tools configuration
        public MappingToolsViewModel Tools { get; set; } = new MappingToolsViewModel();

        // Validation and preview
        public List<FieldValidationViewModel> FieldValidations { get; set; } = new List<FieldValidationViewModel>();
        public Dictionary<string, string> FieldPreviews { get; set; } = new Dictionary<string, string>();

        // OCR settings
        public OcrSettingsViewModel OcrSettings { get; set; } = new OcrSettingsViewModel();

        // Auto-detection results
        public List<AutoDetectedFieldViewModel> AutoDetectedFields { get; set; } = new List<AutoDetectedFieldViewModel>();
        public bool HasAutoDetection { get; set; }

        // Progress and statistics
        public MappingProgressViewModel Progress { get; set; } = new MappingProgressViewModel();

        // UI state
        public bool ShowFieldList { get; set; } = true;
        public bool ShowProperties { get; set; } = true;
        public bool ShowPreview { get; set; } = false;
        public bool ShowValidation { get; set; } = false;
        public string ActiveTab { get; set; } = "fields";

        // API endpoints
        public string SaveFieldUrl { get; set; } = "/Template/SaveFieldMapping";
        public string DeleteFieldUrl { get; set; } = "/Template/DeleteFieldMapping";
        public string PreviewFieldUrl { get; set; } = "/Template/PreviewFieldExtraction";
        public string AutoDetectUrl { get; set; } = "/Template/AutoDetectFields";
        public string ValidateFieldUrl { get; set; } = "/Template/ValidateFieldMapping";
    }

    public class DataTypeOptionViewModel
    {
        public DataTypeEnum Value { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
        public List<string> SampleFormats { get; set; } = new List<string>();
        public List<ValidationRuleOptionViewModel> ValidationRules { get; set; } = new List<ValidationRuleOptionViewModel>();
    }

    public class ValidationRuleOptionViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class MappingToolsViewModel
    {
        public bool ShowGrid { get; set; } = true;
        public bool ShowRulers { get; set; } = true;
        public bool SnapToGrid { get; set; } = true;
        public int GridSize { get; set; } = 10;
        public bool ShowGuides { get; set; } = true;
        public bool ShowFieldBounds { get; set; } = true;
        public bool ShowAnchorPoints { get; set; } = true;

        // Selection tools
        public string SelectionMode { get; set; } = "rectangle"; // rectangle, polygon, freehand
        public bool MultiSelect { get; set; } = true;
        public bool AutoNaming { get; set; } = true;

        // Visual aids
        public bool HighlightSimilar { get; set; } = false;
        public bool ShowConfidence { get; set; } = true;
        public bool ShowDataType { get; set; } = true;
        public decimal FieldOpacity { get; set; } = 0.7m;

        // Keyboard shortcuts
        public Dictionary<string, string> KeyboardShortcuts { get; set; } = new Dictionary<string, string>();
    }

    public class FieldValidationViewModel
    {
        public int FieldMappingId { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public decimal? Confidence { get; set; }
        public string? ExtractedText { get; set; }
        public string? FormattedValue { get; set; }
    }

    public class OcrSettingsViewModel
    {
        public bool UseOCR { get; set; } = true;
        public string Language { get; set; } = "eng";
        public decimal ConfidenceThreshold { get; set; } = 0.7m;
        public bool PreprocessImage { get; set; } = true;
        public bool CorrectSkew { get; set; } = true;
        public bool EnhanceContrast { get; set; } = true;
        public bool RemoveNoise { get; set; } = true;

        public List<OcrLanguageOptionViewModel> LanguageOptions { get; set; } = new List<OcrLanguageOptionViewModel>();
        public List<PreprocessingOptionViewModel> PreprocessingOptions { get; set; } = new List<PreprocessingOptionViewModel>();
    }

    public class OcrLanguageOptionViewModel
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string NativeName { get; set; } = string.Empty;
        public bool IsInstalled { get; set; }
        public decimal Accuracy { get; set; }
    }

    public class PreprocessingOptionViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public bool IsRecommended { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class AutoDetectedFieldViewModel
    {
        public string SuggestedName { get; set; } = string.Empty;
        public DataTypeEnum SuggestDataTypeEnum { get; set; }
        public decimal X { get; set; }
        public decimal Y { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public int PageNumber { get; set; }
        public decimal Confidence { get; set; }
        public string? ExtractedText { get; set; }
        public string? DetectionMethod { get; set; }
        public bool IsAccepted { get; set; }
        public string? RejectionReason { get; set; }
        public List<string> AlternativeNames { get; set; } = new List<string>();
    }

    public class MappingProgressViewModel
    {
        public int TotalFields { get; set; }
        public int MappedFields { get; set; }
        public int ValidatedFields { get; set; }
        public int FieldsWithErrors { get; set; }
        public int FieldsWithWarnings { get; set; }

        public decimal CompletionPercentage { get; set; }
        public decimal ValidationPercentage { get; set; }
        public decimal AverageConfidence { get; set; }

        public bool IsComplete { get; set; }
        public bool HasErrors { get; set; }
        public bool HasWarnings { get; set; }

        public TimeSpan EstimatedTimeRemaining { get; set; }
        public DateTime? LastSaved { get; set; }
        public bool HasUnsavedChanges { get; set; }
    }

    public class FieldMappingFormViewModel
    {
        public int? Id { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public DataTypeEnum DataType { get; set; }
        public string? Description { get; set; }

        // Coordinates
        public decimal X { get; set; }
        public decimal Y { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public int PageNumber { get; set; }

        // Validation
        public bool IsRequired { get; set; }
        public string? ValidationPattern { get; set; }
        public string? ValidationMessage { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public string? DefaultValue { get; set; }

        // OCR settings
        public bool UseOCR { get; set; } = true;
        public string? OCRLanguage { get; set; }
        public decimal OCRConfidenceThreshold { get; set; } = 0.7m;

        // Display
        public int DisplayOrder { get; set; }
        public string? BorderColor { get; set; }
        public bool IsVisible { get; set; } = true;

        // Preview data
        public string? PreviewText { get; set; }
        public decimal? PreviewConfidence { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
    }
}