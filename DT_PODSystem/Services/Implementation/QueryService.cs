using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Data;
using DT_PODSystem.Helpers;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.Entities;
using DT_PODSystem.Models.Enums;
using DT_PODSystem.Models.ViewModels;
using DT_PODSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DT_PODSystem.Services.Implementation
{
    /// <summary>
    /// Query service implementation - manages Query entities and formula logic (migrated from TemplateService Step 4)
    /// </summary>
    public class QueryService : IQueryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QueryService> _logger;

        public QueryService(ApplicationDbContext context, ILogger<QueryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ✅ SaveConstantAsync (aligned with Template pattern)
        public async Task<SaveResult> SaveConstantAsync(int queryId, QueryConstantDto constant)
        {
            try
            {
                if (constant.Id > 0)
                {
                    // Update existing constant
                    var existing = await _context.QueryConstants.FindAsync(constant.Id);
                    if (existing == null)
                    {
                        return new SaveResult { Success = false, Message = "Constant not found" };
                    }

                    existing.Name = constant.Name;
                    existing.DisplayName = constant.DisplayName;
                    existing.DefaultValue = constant.DefaultValue;
                    existing.IsGlobal = constant.IsGlobal;
                    existing.QueryId = constant.IsGlobal ? null : queryId; // ✅ NULL for global constants
                    existing.Description = constant.Description;
                    existing.ModifiedDate = DateTime.UtcNow;
                    existing.ModifiedBy = Util.GetCurrentUser().Code;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Updated constant {ConstantId} - Global: {IsGlobal}",
                        existing.Id, constant.IsGlobal);

                    return new SaveResult { Success = true, Id = existing.Id, Message = "Constant updated successfully" };
                }
                else
                {
                    // Create new constant
                    var newConstant = new QueryConstant
                    {
                        QueryId = constant.IsGlobal ? null : queryId, // ✅ NULL for global constants
                        Name = constant.Name,
                        DisplayName = constant.DisplayName,
                        DataType = DataTypeEnum.Number,
                        DefaultValue = constant.DefaultValue,
                        IsGlobal = constant.IsGlobal,
                        IsConstant = true,
                        IsRequired = constant.IsRequired,
                        Description = constant.Description,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = Util.GetCurrentUser().Code
                    };

                    _context.QueryConstants.Add(newConstant);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Created new constant {ConstantId} - Global: {IsGlobal}",
                        newConstant.Id, constant.IsGlobal);

                    return new SaveResult { Success = true, Id = newConstant.Id, Message = "Constant created successfully" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving constant for query {QueryId}", queryId);
                return new SaveResult { Success = false, Message = "Error saving constant" };
            }
        }

        // ✅ SaveOutputAsync (aligned with Template pattern)
        public async Task<SaveResult> SaveOutputAsync(int queryId, QueryOutputDto output)
        {
            try
            {
                var query = await _context.Queries.FindAsync(queryId);
                if (query == null)
                {
                    return new SaveResult { Success = false, Message = "Query not found" };
                }

                if (output.Id > 0)
                {
                    // Update existing output
                    var existing = await _context.QueryOutputs.FindAsync(output.Id);
                    if (existing == null)
                    {
                        return new SaveResult { Success = false, Message = "Output not found" };
                    }

                    existing.Name = output.Name;
                    existing.DisplayName = output.DisplayName;
                    existing.Description = output.Description;
                    existing.ExecutionOrder = output.ExecutionOrder;
                    existing.DisplayOrder = output.DisplayOrder;
                    existing.IsActive = output.IsActive;
                    existing.ModifiedDate = DateTime.UtcNow;
                    existing.ModifiedBy = Util.GetCurrentUser().Code;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Updated output {OutputId} for query {QueryId}",
                        existing.Id, queryId);

                    return new SaveResult { Success = true, Id = existing.Id, Message = "Output updated successfully" };
                }
                else
                {
                    // Create new output
                    var newOutput = new QueryOutput
                    {
                        QueryId = queryId,
                        Name = output.Name,
                        DisplayName = output.DisplayName,
                        DataType = DataTypeEnum.Number,
                        Description = output.Description,
                        FormulaExpression = output.FormulaExpression ?? "",
                        ExecutionOrder = output.ExecutionOrder,
                        DisplayOrder = output.DisplayOrder,
                        IsRequired = output.IsRequired,
                        IsActive = output.IsActive,
                        IncludeInOutput = output.IncludeInOutput,
                        IsVisible = output.IsVisible,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = Util.GetCurrentUser().Code
                    };

                    _context.QueryOutputs.Add(newOutput);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Created new output {OutputId} for query {QueryId}",
                        newOutput.Id, queryId);

                    return new SaveResult { Success = true, Id = newOutput.Id, Message = "Output created successfully" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving output for query {QueryId}", queryId);
                return new SaveResult { Success = false, Message = "Error saving output" };
            }
        }

        // ✅ Canvas Usage Check Methods (aligned with Template pattern)
        private bool IsConstantUsedInCanvasState(string canvasState, int constantId)
        {
            if (string.IsNullOrEmpty(canvasState))
            {
                _logger.LogInformation("Canvas state is empty for constant {ConstantId}", constantId);
                return false;
            }

            try
            {
                _logger.LogInformation("Checking constant {ConstantId} usage in canvas state", constantId);

                // ✅ Parse the JSON structure to access nodes
                var canvasData = JsonConvert.DeserializeObject<dynamic>(canvasState);
                string htmlContent = "";

                // Structure 1: {"nodes": {...}} - New structure
                if (canvasData?.nodes != null)
                {
                    var nodes = canvasData.nodes;
                    foreach (var node in nodes)
                    {
                        if (node.Value?.html != null)
                        {
                            htmlContent += node.Value.html.ToString() + " ";
                        }
                    }
                }

                // ✅ Search for the constant in the combined HTML content
                var searchPattern = $"data-constant-id=\"{constantId}\"";
                var found = htmlContent.Contains(searchPattern);

                _logger.LogInformation("Searching for pattern: {Pattern} in HTML content, Found: {Found}", searchPattern, found);

                return found;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing canvas state for constant {ConstantId} usage check", constantId);
                return true; // Err on side of caution
            }
        }

        private bool IsOutputUsedInCanvasState(string canvasState, int outputId)
        {
            if (string.IsNullOrEmpty(canvasState))
            {
                _logger.LogInformation("Canvas state is empty for output {OutputId}", outputId);
                return false;
            }

            try
            {
                _logger.LogInformation("Checking output {OutputId} usage in canvas state", outputId);

                // ✅ Parse the JSON structure to access nodes
                var canvasData = JsonConvert.DeserializeObject<dynamic>(canvasState);
                string htmlContent = "";

                // Structure 1: {"nodes": {...}} - New structure
                if (canvasData?.nodes != null)
                {
                    var nodes = canvasData.nodes;
                    foreach (var node in nodes)
                    {
                        if (node.Value?.html != null)
                        {
                            htmlContent += node.Value.html.ToString() + " ";
                        }
                    }
                }

                // ✅ Search for the output in the combined HTML content
                var searchPattern = $"data-output-id=\"{outputId}\"";
                var found = htmlContent.Contains(searchPattern);

                _logger.LogInformation("Searching for pattern: {Pattern} in HTML content, Found: {Found}", searchPattern, found);

                return found;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing canvas state for output {OutputId} usage check", outputId);
                return true; // Err on side of caution
            }
        }

        // ✅ Delete Methods (aligned with Template pattern)
        public async Task<DeleteResult> DeleteConstantAsync(int queryId, int constantId)
        {
            try
            {
                var constant = await _context.QueryConstants
                    .FirstOrDefaultAsync(c => c.Id == constantId && c.IsConstant);

                if (constant == null)
                {
                    return new DeleteResult
                    {
                        Success = false,
                        Message = "Constant not found",
                        ErrorCode = "CONSTANT_NOT_FOUND"
                    };
                }

                // Security checks for global vs local constants
                if (constant.IsGlobal && constant.QueryId != null)
                {
                    return new DeleteResult
                    {
                        Success = false,
                        Message = "Data inconsistency detected",
                        ErrorCode = "DATA_INCONSISTENCY"
                    };
                }

                if (!constant.IsGlobal && constant.QueryId != queryId)
                {
                    return new DeleteResult
                    {
                        Success = false,
                        Message = "Access denied",
                        ErrorCode = "ACCESS_DENIED"
                    };
                }

                // ✅ Check usage in canvas
                var usageCheck = await GetConstantUsageDetailsAsync(constantId, queryId, constant.IsGlobal);

                if (usageCheck.IsInUse)
                {
                    var scope = constant.IsGlobal ? "multiple queries" : "this query";
                    return new DeleteResult
                    {
                        Success = false,
                        Message = $"Cannot delete '{constant.DisplayName}' - it's currently being used in {scope}",
                        ErrorCode = constant.IsGlobal ? "GLOBAL_CONSTANT_IN_USE" : "LOCAL_CONSTANT_IN_USE",
                        UsageDetails = usageCheck.UsageDetails,
                        RequiredActions = new List<string>
                {
                    "Remove the constant from the formula canvas first",
                    "Delete any connections to this constant",
                    "Then try deleting the constant again"
                }
                    };
                }

                // Safe to delete
                _context.QueryConstants.Remove(constant);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted constant {ConstantId} (Global: {IsGlobal}) from query {QueryId}",
                    constantId, constant.IsGlobal, queryId);

                return new DeleteResult
                {
                    Success = true,
                    Message = $"Constant '{constant.DisplayName}' deleted successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting constant {ConstantId} for query {QueryId}", constantId, queryId);
                return new DeleteResult
                {
                    Success = false,
                    Message = "An error occurred while deleting the constant",
                    ErrorCode = "DELETE_ERROR"
                };
            }
        }

        public async Task<DeleteResult> DeleteOutputAsync(int queryId, int outputId)
        {
            try
            {
                var output = await _context.QueryOutputs
                    .FirstOrDefaultAsync(o => o.Id == outputId && o.QueryId == queryId);

                if (output == null)
                {
                    _logger.LogWarning("Output {OutputId} not found for query {QueryId}", outputId, queryId);
                    return new DeleteResult
                    {
                        Success = false,
                        Message = "Output field not found",
                        ErrorCode = "OUTPUT_NOT_FOUND"
                    };
                }

                // ✅ Check if output is being used in canvas
                var usageCheck = await GetOutputUsageDetailsAsync(outputId, queryId);

                if (usageCheck.IsInUse)
                {
                    return new DeleteResult
                    {
                        Success = false,
                        Message = $"Cannot delete '{output.DisplayName}' - it's currently being used in the formula canvas",
                        ErrorCode = "OUTPUT_IN_USE",
                        UsageDetails = usageCheck.UsageDetails,
                        RequiredActions = new List<string>
                {
                    "Remove the output field from the formula canvas first",
                    "Delete any connections to this output field",
                    "Then try deleting the output field again"
                }
                    };
                }

                // ✅ Safe to delete
                _context.QueryOutputs.Remove(output);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted output {OutputId} from query {QueryId}", outputId, queryId);

                return new DeleteResult
                {
                    Success = true,
                    Message = $"Output field '{output.DisplayName}' deleted successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting output {OutputId} for query {QueryId}", outputId, queryId);
                return new DeleteResult
                {
                    Success = false,
                    Message = "An error occurred while deleting the output field",
                    ErrorCode = "DELETE_ERROR"
                };
            }
        }

        // ✅ Get Methods (aligned with Template pattern)
        public async Task<QueryConstantDto?> GetConstantAsync(int constantId)
        {
            try
            {
                var constant = await _context.QueryConstants
                    .Where(c => c.Id == constantId && c.IsConstant)
                    .FirstOrDefaultAsync();

                if (constant == null)
                {
                    return null;
                }

                return new QueryConstantDto
                {
                    Id = constant.Id,
                    Name = constant.Name,
                    DisplayName = constant.DisplayName,
                    DefaultValue = constant.DefaultValue,
                    IsGlobal = constant.IsGlobal,
                    Description = constant.Description
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting constant {ConstantId}", constantId);
                return null;
            }
        }

        public async Task<QueryOutputDto?> GetOutputAsync(int outputId)
        {
            try
            {
                var output = await _context.QueryOutputs
                    .Where(o => o.Id == outputId)
                    .FirstOrDefaultAsync();

                if (output == null)
                {
                    return null;
                }

                return new QueryOutputDto
                {
                    Id = output.Id,
                    Name = output.Name,
                    DisplayName = output.DisplayName,
                    Description = output.Description,
                    ExecutionOrder = output.ExecutionOrder,
                    DisplayOrder = output.DisplayOrder,
                    IsActive = output.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting output {OutputId}", outputId);
                return null;
            }
        }

        // ✅ Usage Details Methods (aligned with Template pattern)
        public async Task<ConstantUsageResult> GetConstantUsageDetailsAsync(int constantId, int queryId, bool isGlobal)
        {
            var result = new ConstantUsageResult();

            try
            {
                if (isGlobal)
                {
                    // Check ALL queries for global constants
                    var queriesWithUsage = await _context.Queries
                        .Include(q => q.FormulaCanvas)
                        .Where(q => q.FormulaCanvas != null && q.FormulaCanvas.CanvasState != null)
                        .ToListAsync();

                    foreach (var query in queriesWithUsage)
                    {
                        if (IsConstantUsedInCanvasState(query.FormulaCanvas.CanvasState, constantId))
                        {
                            result.UsageDetails.Add($"Used in canvas: '{query.Name}' (ID: {query.Id})");
                            result.IsInUse = true;
                        }
                    }

                    // Check query outputs across all queries
                    var outputsWithUsage = await _context.QueryOutputs
                        .Include(qo => qo.Query)
                        .Where(qo => qo.FormulaExpression != null &&
                                    qo.FormulaExpression.Contains($"CONST_{constantId}"))
                        .ToListAsync();

                    foreach (var output in outputsWithUsage)
                    {
                        result.UsageDetails.Add($"Used in formula: '{output.DisplayName}' in query '{output.Query.Name}'");
                        result.IsInUse = true;
                    }
                }
                else
                {
                    // Check only the specific query for local constants
                    var query = await _context.Queries
                        .Include(q => q.FormulaCanvas)
                        .FirstOrDefaultAsync(q => q.Id == queryId);

                    if (query?.FormulaCanvas?.CanvasState != null)
                    {
                        if (IsConstantUsedInCanvasState(query.FormulaCanvas.CanvasState, constantId))
                        {
                            result.UsageDetails.Add($"Used in canvas: '{query.Name}'");
                            result.IsInUse = true;
                        }
                    }

                    var localOutputsWithUsage = await _context.QueryOutputs
                        .Where(qo => qo.QueryId == queryId &&
                                    qo.FormulaExpression != null &&
                                    qo.FormulaExpression.Contains($"CONST_{constantId}"))
                        .ToListAsync();

                    foreach (var output in localOutputsWithUsage)
                    {
                        result.UsageDetails.Add($"Used in formula: '{output.DisplayName}'");
                        result.IsInUse = true;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking constant usage details for {ConstantId}", constantId);
                result.IsInUse = true;
                result.UsageDetails.Add("Unable to verify usage - deletion blocked for safety");
                return result;
            }
        }

        public async Task<ConstantUsageResult> GetOutputUsageDetailsAsync(int outputId, int queryId)
        {
            var result = new ConstantUsageResult();

            try
            {
                // Check if output is used in canvas
                var query = await _context.Queries
                    .Include(q => q.FormulaCanvas)
                    .FirstOrDefaultAsync(q => q.Id == queryId);

                if (query?.FormulaCanvas?.CanvasState != null)
                {
                    if (IsOutputUsedInCanvasState(query.FormulaCanvas.CanvasState, outputId))
                    {
                        result.UsageDetails.Add($"Used in canvas: Connected as output endpoint");
                        result.IsInUse = true;
                    }
                }

                // Check if this output is referenced in other query output formulas
                var outputsWithUsage = await _context.QueryOutputs
                    .Where(qo => qo.QueryId == queryId &&
                                qo.Id != outputId && // Don't check self
                                qo.FormulaExpression != null &&
                                qo.FormulaExpression.Contains($"OUTPUT_{outputId}"))
                    .ToListAsync();

                foreach (var output in outputsWithUsage)
                {
                    result.UsageDetails.Add($"Used in formula: '{output.DisplayName}' references this output");
                    result.IsInUse = true;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking output usage details for {OutputId}", outputId);
                result.IsInUse = true;
                result.UsageDetails.Add("Unable to verify usage - deletion blocked for safety");
                return result;
            }
        }

        // ✅ Additional methods for finalization
        public async Task<bool> FinalizeQueryAsync(int queryId)
        {
            try
            {
                var query = await _context.Queries
                    .FirstOrDefaultAsync(q => q.Id == queryId);

                if (query == null)
                    return false;

                // Update status and timestamps
                query.Status = QueryStatus.Active;
                query.ModifiedDate = DateTime.UtcNow;
                query.ModifiedBy = Util.GetCurrentUser().Code;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Query {QueryId} finalized successfully", queryId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing query {QueryId}", queryId);
                return false;
            }
        }

        public async Task<QueryValidationResult> ValidateQueryCompletenessAsync(int queryId)
        {
            var result = new QueryValidationResult();

            try
            {
                var query = await _context.Queries
                    .Include(q => q.QueryConstants)
                    .Include(q => q.QueryOutputs)
                    .FirstOrDefaultAsync(q => q.Id == queryId);

                if (query == null)
                {
                    result.Errors.Add("Query not found");
                    return result;
                }

                // Basic validation
                if (string.IsNullOrWhiteSpace(query.Name))
                {
                    result.Errors.Add("Query name is required");
                }

                // Check if query has at least one output
                if (!query.QueryOutputs.Any())
                {
                    result.Warnings.Add("No output fields defined - query will have limited functionality");
                }

                result.IsValid = !result.Errors.Any();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating query {QueryId}", queryId);
                result.Errors.Add("Validation failed due to system error");
                return result;
            }
        }


        /// <summary>
        /// Save complete query data - basic information + canvas state + formulas
        /// This is the main save method that handles everything
        /// </summary>
        public async Task<bool> SaveQueryDataAsync(int queryId, QueryDataDto queryData)
        {
            try
            {
                _logger.LogInformation("Saving complete query data for query {QueryId}", queryId);

                var query = await _context.Queries
                    .Include(q => q.QueryConstants)
                    .Include(q => q.QueryOutputs)
                    .Include(q => q.FormulaCanvas)
                    .FirstOrDefaultAsync(q => q.Id == queryId);

                if (query == null)
                {
                    _logger.LogWarning("Query {QueryId} not found", queryId);
                    return false;
                }

                // 1. UPDATE BASIC INFORMATION (if provided)
                if (!string.IsNullOrWhiteSpace(queryData.Name))
                {
                    query.Name = queryData.Name.Trim();
                    query.Status = queryData.Status ?? QueryStatus.Draft;
                    query.Description = queryData.Description?.Trim() ?? string.Empty;
                    query.ModifiedDate = DateTime.UtcNow;
                    query.ModifiedBy = Util.GetCurrentUser().Code;

                    _logger.LogInformation("Updated basic info for query {QueryId}: {Name}", queryId, queryData.Name);
                }

                // 2. UPDATE CANVAS STATE AND FORMULAS (if provided)
                if (queryData != null)
                {
                    decimal zoomLevel = 1.0m;
                    int canvasX = 0;
                    int canvasY = 0;

                    // Parse canvas state for zoom/position
                    if (!string.IsNullOrEmpty(queryData.CanvasState) && queryData.CanvasState != "{}")
                    {
                        try
                        {
                            var canvasData = JsonConvert.DeserializeObject<dynamic>(queryData.CanvasState);
                            if (canvasData?.zoom != null)
                            {
                                zoomLevel = (decimal)(double)canvasData.zoom;
                            }
                            if (canvasData?.position?.x != null)
                            {
                                canvasX = (int)canvasData.position.x;
                            }
                            if (canvasData?.position?.y != null)
                            {
                                canvasY = (int)canvasData.position.y;
                            }
                            _logger.LogInformation("Extracted canvas state - Zoom: {Zoom}, Position: ({X}, {Y})",
                                zoomLevel, canvasX, canvasY);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse canvas state for zoom extraction");
                        }
                    }

                    // Save/Update FormulaCanvas
                    var formulaCanvas = query.FormulaCanvas;
                    if (formulaCanvas == null)
                    {
                        formulaCanvas = new FormulaCanvas
                        {
                            QueryId = queryId,
                            Name = $"{query.Name}_Formula",
                            Description = "Visual formula canvas",
                            Width = 1200,
                            Height = 800,
                            ZoomLevel = zoomLevel,
                            Version = "1.0",
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow,
                            CreatedBy = Util.GetCurrentUser().Code
                        };
                        _context.FormulaCanvases.Add(formulaCanvas);
                        await _context.SaveChangesAsync(); // Save to get ID
                    }
                    else
                    {
                        formulaCanvas.ZoomLevel = zoomLevel;
                        formulaCanvas.ModifiedDate = DateTime.UtcNow;
                        formulaCanvas.ModifiedBy = Util.GetCurrentUser().Code;
                    }

                    formulaCanvas.CanvasState = queryData.CanvasState;
                    formulaCanvas.LastValidated = DateTime.UtcNow;

                    // Update QueryOutputs with FormulaExpression
                    if (queryData.Outputs != null && queryData.Outputs.Any())
                    {
                        _logger.LogInformation("Updating {Count} query outputs with formulas", queryData.Outputs.Count);

                        foreach (var outputDto in queryData.Outputs)
                        {
                            var existingOutput = query.QueryOutputs.FirstOrDefault(qo => qo.Id == outputDto.Id);

                            if (existingOutput != null)
                            {
                                existingOutput.FormulaExpression = outputDto.FormulaExpression ?? string.Empty;
                                existingOutput.ModifiedDate = DateTime.UtcNow;
                                existingOutput.ModifiedBy = Util.GetCurrentUser().Code;

                                _logger.LogInformation("Updated FormulaExpression for QueryOutput {OutputId}: '{Expression}'",
                                    existingOutput.Id, outputDto.FormulaExpression);
                            }
                            else
                            {
                                _logger.LogWarning("QueryOutput with ID {OutputId} not found for query {QueryId}",
                                    outputDto.Id, queryId);
                            }
                        }
                    }
                }

                // 3. SAVE ALL CHANGES
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully saved complete query data for query {QueryId}", queryId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving complete query data for query {QueryId}", queryId);
                return false;
            }
        }





        // Add these missing methods to QueryService.cs

        #region Missing Interface Implementations

        // ========================================================================================
        // QueryService - CreateQueryAsync Implementation
        // Add this method to your QueryService Implementation
        // ========================================================================================

        /// <summary>
        /// Create a new query with basic information only
        /// </summary>
        public async Task<Query> CreateQueryAsync(QueryDto queryDto)
        {
            try
            {
                _logger.LogInformation("Creating new query: {QueryName}", queryDto.Name);

                // Validate input
                if (string.IsNullOrWhiteSpace(queryDto.Name))
                {
                    throw new ArgumentException("Query name is required");
                }

                if (queryDto.Name.Length < 3)
                {
                    throw new ArgumentException("Query name must be at least 3 characters long");
                }

                if (queryDto.Name.Length > 200)
                {
                    throw new ArgumentException("Query name cannot exceed 200 characters");
                }

                // Check if query name already exists
                var existingQuery = await _context.Queries
                    .Where(q => q.Name.ToLower() == queryDto.Name.ToLower() && q.IsActive)
                    .FirstOrDefaultAsync();

                if (existingQuery != null)
                {
                    throw new InvalidOperationException($"A query with the name '{queryDto.Name}' already exists");
                }

                // Create new query entity with basic information only
                var query = new Query
                {
                    Name = queryDto.Name.Trim(),
                    Description = queryDto.Description?.Trim() ?? string.Empty,
                    Status = QueryStatus.Draft,
                    IsActive = true,
                    ExecutionPriority = 5, // Default priority
                    ExecutionCount = 0,
                    Version = "1.0",
                    CreatedBy = Util.GetCurrentUser().Code,
                    CreatedDate = DateTime.UtcNow
                };

                // Add to database
                _context.Queries.Add(query);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Query created successfully with ID: {QueryId}", query.Id);

                return query;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation exceptions
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw business logic exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating query: {QueryName}", queryDto.Name);
                throw new InvalidOperationException("Failed to create query", ex);
            }
        }


        /// <summary>
        /// Update query with DTO data
        /// </summary>
        public async Task<bool> UpdateQueryAsync(int queryId, QueryDto queryDto)
        {
            try
            {
                var query = await _context.Queries.FirstOrDefaultAsync(q => q.Id == queryId);
                if (query == null)
                    return false;

                query.Name = queryDto.Name;
                query.Description = queryDto.Description;
                query.Status = queryDto.Status;
                query.ExecutionPriority = queryDto.ExecutionPriority;
                query.IsActive = queryDto.IsActive;
                query.ModifiedDate = DateTime.UtcNow;
                query.ModifiedBy = "System";

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating query {QueryId}", queryId);
                return false;
            }
        }

        /// <summary>
        /// Test query execution and validation
        /// </summary>
        public async Task<QueryTestResult> TestQueryAsync(int queryId)
        {
            var result = new QueryTestResult();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("Testing query {QueryId}", queryId);

                var query = await _context.Queries
                    .Include(q => q.QueryConstants)
                    .Include(q => q.QueryOutputs)
                    .Include(q => q.FormulaCanvas)
                    .FirstOrDefaultAsync(q => q.Id == queryId);

                if (query == null)
                {
                    result.Success = false;
                    result.Message = "Query not found";
                    return result;
                }

                // Validate query structure
                var validationResult = await ValidateQueryCompletenessAsync(queryId);
                if (!validationResult.IsValid)
                {
                    result.Success = false;
                    result.Message = "Query validation failed";
                    result.ValidationErrors = validationResult.Errors;
                    return result;
                }

                // Test formula expressions
                foreach (var output in query.QueryOutputs)
                {
                    if (!string.IsNullOrEmpty(output.FormulaExpression))
                    {
                        try
                        {
                            // Basic syntax validation
                            if (output.FormulaExpression.Contains("([") && output.FormulaExpression.Contains("])"))
                            {
                                result.TestResults[$"Output_{output.Name}"] = "Formula syntax valid";
                            }
                            else
                            {
                                result.ValidationErrors.Add($"Invalid formula syntax in output '{output.DisplayName}'");
                            }
                        }
                        catch (Exception ex)
                        {
                            result.ValidationErrors.Add($"Error testing formula in '{output.DisplayName}': {ex.Message}");
                        }
                    }
                }

                // Test canvas state
                if (query.FormulaCanvas != null && !string.IsNullOrEmpty(query.FormulaCanvas.CanvasState))
                {
                    try
                    {
                        JsonConvert.DeserializeObject(query.FormulaCanvas.CanvasState);
                        result.TestResults["CanvasState"] = "Valid JSON structure";
                    }
                    catch (Exception ex)
                    {
                        result.ValidationErrors.Add($"Invalid canvas state JSON: {ex.Message}");
                    }
                }

                result.ExecutionTime = DateTime.UtcNow - startTime;
                result.Success = !result.ValidationErrors.Any();
                result.Message = result.Success ? "Query test completed successfully" : "Query test completed with errors";

                _logger.LogInformation("Query {QueryId} test completed - Success: {Success}, Errors: {ErrorCount}",
                    queryId, result.Success, result.ValidationErrors.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing query {QueryId}", queryId);
                result.Success = false;
                result.Message = "Test execution failed";
                result.ValidationErrors.Add($"System error: {ex.Message}");
                result.ExecutionTime = DateTime.UtcNow - startTime;
                return result;
            }
        }

        /// <summary>
        /// Perform bulk actions on multiple queries
        /// </summary>
        public async Task<BulkActionResult> BulkActionAsync(string action, List<int> queryIds)
        {
            var result = new BulkActionResult
            {
                TotalCount = queryIds.Count
            };

            try
            {
                _logger.LogInformation("Performing bulk action '{Action}' on {Count} queries", action, queryIds.Count);

                foreach (var queryId in queryIds)
                {
                    try
                    {
                        bool actionResult = action.ToLower() switch
                        {
                            "activate" => await UpdateQueryStatusAsync(queryId, QueryStatus.Active),
                            "test" => (await TestQueryAsync(queryId)).Success,
                            "delete" => await DeleteQueryAsync(queryId),
                            _ => false
                        };

                        if (actionResult)
                        {
                            result.SuccessfulIds.Add(queryId);
                            result.ProcessedCount++;
                        }
                        else
                        {
                            result.FailedIds.Add(queryId);
                            result.Errors.Add($"Failed to {action} query {queryId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailedIds.Add(queryId);
                        result.Errors.Add($"Error processing query {queryId}: {ex.Message}");
                        _logger.LogError(ex, "Error in bulk action for query {QueryId}", queryId);
                    }
                }

                result.Success = result.ProcessedCount > 0;
                result.Message = result.Success ?
                    $"Bulk action completed: {result.ProcessedCount}/{result.TotalCount} successful" :
                    "Bulk action failed - no items processed successfully";

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk action '{Action}'", action);
                result.Success = false;
                result.Message = "Bulk action failed due to system error";
                result.Errors.Add($"System error: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Get query builder state for wizard interface (parameterless for new query)
        /// </summary>
        public async Task<QueryBuilderViewModel> GetQueryBuilderStateAsync()
        {
            var model = new QueryBuilderViewModel
            {
                QueryId = 0,
                QueryName = "",
                Description = "",
                Status = QueryStatus.Draft,
                IsEditMode = false
            };

            // Load global constants for new query
            var globalConstants = await _context.QueryConstants
                .Where(c => c.QueryId == null && c.IsGlobal && c.IsConstant)
                .ToListAsync();

            model.GlobalConstants = globalConstants.Select(gc => new QueryConstantDto
            {
                Id = gc.Id,
                Name = gc.Name,
                DisplayName = gc.DisplayName,
                DefaultValue = gc.DefaultValue,
                Description = gc.Description,
                IsConstant = true,
                IsGlobal = true,
                IsRequired = gc.IsRequired
            }).ToList();

            // Empty collections for new query
            model.LocalConstants = new List<QueryConstantDto>();
            model.OutputFields = new List<QueryOutputDto>();
            model.CanvasState = "{}";

            return model;
        }


        /// <summary>
        /// Get all constants for a query (returns QueryConstantDto list)
        /// </summary>
        public async Task<List<QueryConstantDto>> GetQueryConstantsAsync(int queryId)
        {
            try
            {
                // Get both global constants (QueryId = null) and local constants for this query
                var constants = await _context.QueryConstants
                    .Where(c => c.QueryId == null || c.QueryId == queryId) // Global OR local to this query
                    .Where(c => c.IsConstant)
                    .OrderBy(c => c.IsGlobal ? 0 : 1) // Global first, then local
                    .ThenBy(c => c.Name)
                    .ToListAsync();

                return constants.Select(c => new QueryConstantDto
                {
                    Id = c.Id,
                    QueryId = c.QueryId,
                    Name = c.Name,
                    DisplayName = c.DisplayName,
                    DefaultValue = c.DefaultValue,
                    IsGlobal = c.IsGlobal,
                    IsConstant = c.IsConstant,
                    IsRequired = c.IsRequired,
                    Description = c.Description,
                    DataType = c.DataType,
                    IsActive = c.IsActive
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting constants for query {QueryId}", queryId);
                return new List<QueryConstantDto>();
            }
        }



        /// <summary>
        /// Get all outputs for a query (returns QueryOutputDto list)
        /// </summary>
        public async Task<List<QueryOutputDto>> GetQueryOutputsAsync(int queryId)
        {
            try
            {
                var outputs = await _context.QueryOutputs
                    .Where(o => o.QueryId == queryId)
                    .OrderBy(o => o.ExecutionOrder)
                    .ThenBy(o => o.DisplayOrder)
                    .ToListAsync();

                return outputs.Select(o => new QueryOutputDto
                {
                    Id = o.Id,
                    QueryId = o.QueryId,
                    Name = o.Name,
                    DisplayName = o.DisplayName ?? "",
                    Description = o.Description ?? "",
                    FormulaExpression = o.FormulaExpression,
                    ExecutionOrder = o.ExecutionOrder,
                    DisplayOrder = o.DisplayOrder,
                    IsActive = o.IsActive,
                    IsRequired = o.IsRequired,
                    IsVisible = o.IsVisible,
                    IncludeInOutput = o.IncludeInOutput,
                    DataType = o.DataType,
                    DefaultValue = o.DefaultValue
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outputs for query {QueryId}", queryId);
                return new List<QueryOutputDto>();
            }
        }


        #endregion


        #region Query CRUD Operations

        public async Task<Query> CreateDraftQueryAsync()
        {
            var query = new Query
            {
                Name = $"Draft Query {DateTime.Now:yyyy-MM-dd HH:mm}",
                Description = "Query in development",
                Status = QueryStatus.Draft,
                Version = "1.0",
                IsActive = true,
                ExecutionPriority = 5,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "System"
            };

            _context.Queries.Add(query);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created draft query with ID {QueryId}", query.Id);
            return query;
        }

        public async Task<Query?> GetQueryAsync(int id)
        {
            return await _context.Queries
                .Include(q => q.QueryConstants)
                .Include(q => q.QueryOutputs)
                .Include(q => q.FormulaCanvas)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<bool> UpdateQueryInfoAsync(int queryId, string name, string? description = null)
        {
            try
            {
                var query = await _context.Queries.FirstOrDefaultAsync(q => q.Id == queryId);
                if (query == null)
                    return false;

                query.Name = name;
                if (description != null)
                    query.Description = description;
                query.ModifiedDate = DateTime.UtcNow;
                query.ModifiedBy = "System";

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating query info for {QueryId}", queryId);
                return false;
            }
        }

        public async Task<bool> UpdateQueryStatusAsync(int queryId, QueryStatus status)
        {
            try
            {
                var query = await _context.Queries.FirstOrDefaultAsync(q => q.Id == queryId);
                if (query == null)
                    return false;

                query.Status = status;
                query.ModifiedDate = DateTime.UtcNow;
                query.ModifiedBy = "System";

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating query status for {QueryId}", queryId);
                return false;
            }
        }



        public async Task<bool> DeleteQueryAsync(int id)
        {
            try
            {
                var query = await _context.Queries.FindAsync(id);
                if (query == null)
                    return false;

                _context.Queries.Remove(query);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting query {QueryId}", id);
                return false;
            }
        }



        #endregion

        #region Query Constants Management



        public async Task<List<ConstantDto>> GetGlobalConstantsAsync()
        {
            var queryConstants = await _context.QueryConstants
                .Where(c => c.IsGlobal && c.IsConstant && c.QueryId == null)
                .ToListAsync();

            var globalConstants = queryConstants.Select(c => new ConstantDto
            {
                Id = c.Id.ToString(),
                Name = c.Name,
                Value = c.DefaultValue ?? "0",
                IsGlobal = true,
                Description = c.Description ?? string.Empty
            }).ToList();

            return globalConstants;
        }

        #endregion

        #region Query Outputs Management








        #endregion

        #region Formula Canvas Management


        public async Task<FormulaCanvas?> GetFormulaCanvasAsync(int queryId)
        {
            return await _context.FormulaCanvases
                .FirstOrDefaultAsync(fc => fc.QueryId == queryId);
        }

        public async Task<bool> UpdateCanvasStateAsync(int queryId, string canvasState)
        {
            try
            {
                var canvas = await GetFormulaCanvasAsync(queryId);
                if (canvas == null)
                    return false;

                canvas.CanvasState = canvasState;
                canvas.ModifiedDate = DateTime.UtcNow;
                canvas.ModifiedBy = "System";

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating canvas state for query {QueryId}", queryId);
                return false;
            }
        }

        #endregion



        #region Query Builder & Wizard

        public async Task<QueryBuilderViewModel> GetQueryBuilderStateAsync(int queryId)
        {
            var model = new QueryBuilderViewModel
            {
                QueryId = queryId
            };

            var query = await _context.Queries
                .Include(q => q.QueryConstants)
                .Include(q => q.QueryOutputs)
                .Include(q => q.FormulaCanvas)
                .FirstOrDefaultAsync(q => q.Id == queryId);

            if (query != null)
            {
                model.QueryName = query.Name;
                model.Description = query.Description;
                model.Status = query.Status;

                // Load global constants
                var globalConstants = await _context.QueryConstants
                    .Where(c => c.QueryId == null && c.IsGlobal && c.IsConstant)
                    .ToListAsync();

                model.GlobalConstants = globalConstants.Select(gc => new QueryConstantDto
                {
                    Id = gc.Id,
                    Name = gc.Name,
                    DisplayName = gc.DisplayName,
                    DefaultValue = gc.DefaultValue,
                    Description = gc.Description,
                    IsConstant = true,
                    IsGlobal = true,
                    IsRequired = gc.IsRequired
                }).ToList();

                // Load local constants
                var localConstants = query.QueryConstants
                    .Where(c => c.IsConstant && !c.IsGlobal)
                    .ToList();

                model.LocalConstants = localConstants.Select(lc => new QueryConstantDto
                {
                    Id = lc.Id,
                    Name = lc.Name,
                    DisplayName = lc.DisplayName,
                    DefaultValue = lc.DefaultValue,
                    Description = lc.Description,
                    IsConstant = true,
                    IsGlobal = false,
                    IsRequired = lc.IsRequired
                }).ToList();

                // Load outputs
                model.OutputFields = query.QueryOutputs.Select(qo => new QueryOutputDto
                {
                    Id = qo.Id,
                    Name = qo.Name,
                    DisplayName = qo.DisplayName ?? string.Empty,
                    Description = qo.Description ?? string.Empty,
                    FormulaExpression = qo.FormulaExpression,
                    DisplayOrder = qo.ExecutionOrder,
                    IsActive = qo.IsActive
                }).ToList();

                model.CanvasState = query.FormulaCanvas?.CanvasState ?? "{}";
            }

            return model;
        }

        public async Task<QueryListViewModel> GetQueryListAsync(QueryFiltersViewModel filters)
        {
            var query = _context.Queries.Where(q => q.IsActive);

            if (!string.IsNullOrEmpty(filters.SearchTerm))
            {
                query = query.Where(q => q.Name.Contains(filters.SearchTerm) || q.Description.Contains(filters.SearchTerm));
            }

            if (filters.Status.HasValue)
            {
                query = query.Where(q => q.Status == filters.Status.Value);
            }

            var totalCount = await query.CountAsync();

            var queries = await query
                .Skip((filters.Pagination.CurrentPage - 1) * filters.Pagination.PageSize)
                .Take(filters.Pagination.PageSize)
                .ToListAsync();

            var queryItems = queries.Select(q => new QueryListItemViewModel
            {
                Id = q.Id,
                Name = q.Name,
                Description = q.Description,
                Status = q.Status,
                CreatedDate = q.CreatedDate,
                ModifiedDate = q.ModifiedDate ?? q.CreatedDate,
                ExecutionCount = q.ExecutionCount,
                ExecutionPriority = q.ExecutionPriority,
                CreatedBy = q.CreatedBy ?? "System"
            }).ToList();

            return new QueryListViewModel
            {
                Queries = queryItems,
                Filters = filters,
                Pagination = new PaginationViewModel
                {
                    CurrentPage = filters.Pagination.CurrentPage,
                    PageSize = filters.Pagination.PageSize,
                    TotalItems = totalCount
                }
            };
        }

        public async Task<QueryDefinitionDto> ExportQueryAsync(int id)
        {
            var query = await GetQueryAsync(id);
            if (query == null) throw new ArgumentException($"Query {id} not found");

            return new QueryDefinitionDto
            {
                Id = query.Id,
                Name = query.Name,
                Description = query.Description,
                Status = query.Status,
                Constants = query.QueryConstants.Select(c => new QueryConstantDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    DisplayName = c.DisplayName,
                    DefaultValue = c.DefaultValue,
                    IsGlobal = c.IsGlobal,
                    Description = c.Description
                }).ToList(),
                Outputs = query.QueryOutputs.Select(o => new QueryOutputDto
                {
                    Id = o.Id,
                    Name = o.Name,
                    DisplayName = o.DisplayName,
                    FormulaExpression = o.FormulaExpression,
                    ExecutionOrder = o.ExecutionOrder
                }).ToList(),
                FormulaCanvas = query.FormulaCanvas != null ? new FormulaCanvasDto
                {
                    Id = query.FormulaCanvas.Id,
                    QueryId = query.FormulaCanvas.QueryId,
                    Name = query.FormulaCanvas.Name,
                    CanvasState = query.FormulaCanvas.CanvasState
                } : null
            };
        }

        #endregion
    }
}