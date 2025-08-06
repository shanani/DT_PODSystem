using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using DT_PODSystem.Models.Enums;
using Humanizer;

namespace DT_PODSystem.Models.DTOs
{
    /// <summary>
    /// Query definition DTO for export/import
    /// </summary>
    public class QueryDefinitionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public QueryStatus Status { get; set; }
        public string Version { get; set; } = "1.0";
        public int ExecutionPriority { get; set; } = 5;
        public bool IsActive { get; set; } = true;

        public List<QueryConstantDto> Constants { get; set; } = new List<QueryConstantDto>();
        public List<QueryOutputDto> Outputs { get; set; } = new List<QueryOutputDto>();
        public FormulaCanvasDto? FormulaCanvas { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }

    /// <summary>
    /// Save result for CRUD operations
    /// </summary>
    public class SaveResult
    {
        public bool Success { get; set; }
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new List<string>();
        public object? Data { get; set; }
    }

    /// <summary>
    /// Delete result with usage validation
    /// </summary>
    public class DeleteResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }
        public List<string> UsageDetails { get; set; } = new List<string>();
        public List<string> RequiredActions { get; set; } = new List<string>();
        public object? Data { get; set; }
    }




    /// <summary>
    /// Query execution result
    /// </summary>
    public class QueryExecutionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> Results { get; set; } = new Dictionary<string, object>();
        public List<string> Errors { get; set; } = new List<string>();
        public TimeSpan ExecutionTime { get; set; }
        public int ProcessedRecords { get; set; }
    }


    /// <summary>
    /// Constant DTO for legacy compatibility
    /// </summary>
    public class ConstantDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsGlobal { get; set; }
        public string Description { get; set; } = string.Empty;
    }
    public class QueryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public QueryStatus Status { get; set; } = QueryStatus.Draft;
        public int ExecutionPriority { get; set; } = 5;
        public int ExecutionCount { get; set; } = 0;
        public DateTime? LastExecuted { get; set; }
        public TimeSpan? LastExecutionTime { get; set; }
        public string Version { get; set; } = "1.0";
        public bool IsActive { get; set; } = true;

        // Audit fields
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? ModifiedBy { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string? ApprovedBy { get; set; }

        // Validation
        public bool IsValid { get; set; } = false;
        public List<string> ValidationErrors { get; set; } = new List<string>();

        // Related data counts
        public int ConstantsCount { get; set; }
        public int OutputsCount { get; set; }
        public bool HasCanvas { get; set; }
    }

    // Add missing properties to QueryConstantDto if not complete
    public partial class QueryConstantDto
    {
        public int Id { get; set; }
        public int? QueryId { get; set; } // Nullable for global constants
        public string Name { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public DataTypeEnum DataType { get; set; } = DataTypeEnum.Number;
        public string? Description { get; set; }
        public string? DefaultValue { get; set; }
        public bool IsRequired { get; set; } = false;
        public bool IsConstant { get; set; } = true;
        public bool IsGlobal { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public string? InputType { get; set; }
        public string? SelectOptions { get; set; }
        public string? SystemSource { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public string? ValidationPattern { get; set; }
        public string? ValidationMessage { get; set; }
    }

    // Add missing properties to QueryOutputDto if not complete  
    public partial class QueryOutputDto
    {
        public int Id { get; set; }
        public int QueryId { get; set; }
        public int? FormulaCanvasId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public DataTypeEnum DataType { get; set; } = DataTypeEnum.Number;
        public string? Description { get; set; }
        public string FormulaExpression { get; set; } = string.Empty;
        public int ExecutionOrder { get; set; } = 0;
        public string? InputDependencies { get; set; } = "[]";
        public string? OutputDependencies { get; set; } = "[]";
        public string? GlobalDependencies { get; set; } = "[]";
        public string? LocalDependencies { get; set; } = "[]";
        public string FormatString { get; set; } = "N2";
        public int DecimalPlaces { get; set; } = 2;
        public string? CurrencySymbol { get; set; }
        public bool IsValid { get; set; } = false;
        public string? ValidationErrors { get; set; }
        public DateTime? LastValidated { get; set; }
        public bool IncludeInOutput { get; set; } = true;
        public bool IsRequired { get; set; } = false;
        public string? DefaultValue { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }


    // ✅ ENHANCED: Query Data DTO with all fields in one place
    public class QueryDataDto
    {
        // ✅ ADDED: Basic query information
        public string? Name { get; set; }
        public string? Description { get; set; }
        public QueryStatus? Status { get; set; }

        // ✅ EXISTING: Canvas and formula data
        public List<QueryConstantDto> Constants { get; set; } = new List<QueryConstantDto>();
        public List<QueryOutputDto> Outputs { get; set; } = new List<QueryOutputDto>();
        public string CanvasState { get; set; } = "{}";
    }

    public class SaveQueryConstantRequest
    {
        public int QueryId { get; set; }
        public QueryConstantDto Constant { get; set; } = new();
    }

    public class SaveQueryOutputRequest
    {
        public int QueryId { get; set; }
        public QueryOutputDto Output { get; set; } = new();
    }

    public class DeleteQueryConstantRequest
    {
        public int QueryId { get; set; }
        public int ConstantId { get; set; }
    }

    public class DeleteQueryOutputRequest
    {
        public int QueryId { get; set; }
        public int OutputId { get; set; }
    }

    public class SaveQueryRequest
    {
        public int QueryId { get; set; }
        public QueryDataDto Data { get; set; } = new();
    }

    // ✅ Formula Canvas DTOs (now for Query)
    public class FormulaCanvasDto
    {
        public int Id { get; set; }
        public int QueryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Width { get; set; } = 1200;
        public int Height { get; set; } = 800;
        public decimal ZoomLevel { get; set; } = 1.0m;
        public string? CanvasState { get; set; }
        public string? FormulaExpression { get; set; }
        public bool IsValid { get; set; } = true;
        public string? ValidationErrors { get; set; }
        public string? Version { get; set; } = "1.0";
        public List<CanvasElementDto> Elements { get; set; } = new List<CanvasElementDto>();
        public List<CanvasConnectionDto> Connections { get; set; } = new List<CanvasConnectionDto>();
    }

    public class CanvasElementDto
    {
        public int Id { get; set; }
        public string ElementId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public CanvasElementType Type { get; set; }
        public decimal X { get; set; }
        public decimal Y { get; set; }
        public decimal Width { get; set; } = 120;
        public decimal Height { get; set; } = 60;
        public string? BackgroundColor { get; set; }
        public string? BorderColor { get; set; }
        public string? TextColor { get; set; }
        public string? IconClass { get; set; }
        public bool IsSelected { get; set; }
        public bool IsLocked { get; set; }
        public int ZIndex { get; set; }
        public string? FieldName { get; set; }
        public string? VariableName { get; set; }
        public string? ConstantValue { get; set; }
        public string? OperationType { get; set; }
        public string? FunctionName { get; set; }
        public string? Parameters { get; set; }
        public DataTypeEnum DataType { get; set; } = DataTypeEnum.Number;
    }

    public class CanvasConnectionDto
    {
        public int Id { get; set; }
        public string ConnectionId { get; set; } = string.Empty;
        public string SourceElementId { get; set; } = string.Empty;
        public string TargetElementId { get; set; } = string.Empty;
        public string? SourcePort { get; set; }
        public string? TargetPort { get; set; }
        public string? Label { get; set; }
        public string? Description { get; set; }
        public string? PathCoordinates { get; set; }
        public bool IsValid { get; set; } = true;
        public string? ValidationError { get; set; }
        public string? Color { get; set; }
        public decimal Thickness { get; set; } = 2;
        public string? LineStyle { get; set; }
        public bool IsSelected { get; set; }
        public int ZIndex { get; set; }
        public int ExecutionOrder { get; set; }
    }



    // ✅ Formula Validation DTOs
    public class FormulaValidationDto
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public string GeneratedFormula { get; set; } = string.Empty;
    }

    public class FormulaTestResultDto
    {
        public bool Success { get; set; }
        public Dictionary<string, object> Results { get; set; } = new Dictionary<string, object>();
        public List<FormulaExecutionStepDto> Steps { get; set; } = new List<FormulaExecutionStepDto>();
        public List<string> Errors { get; set; } = new List<string>();
        public TimeSpan ExecutionTime { get; set; }
    }

    public class FormulaExecutionStepDto
    {
        public int Order { get; set; }
        public string ElementId { get; set; } = string.Empty;
        public string ElementName { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public Dictionary<string, object> Inputs { get; set; } = new Dictionary<string, object>();
        public object? Result { get; set; }
        public TimeSpan Duration { get; set; }
    }


}