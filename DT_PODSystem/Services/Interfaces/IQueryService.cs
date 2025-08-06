using System.Collections.Generic;
using System.Threading.Tasks;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.Entities;
using DT_PODSystem.Models.Enums;
using DT_PODSystem.Models.ViewModels;

namespace DT_PODSystem.Services.Interfaces
{
    /// <summary>
    /// Query service interface for managing Query entities and their formula logic (previously Step 4)
    /// Handles constants, outputs, and formula canvas functionality - MATCHES ORIGINAL STEP 4
    /// </summary>
    public interface IQueryService
    {

        Task<bool> SaveQueryDataAsync(int queryId, QueryDataDto? queryData);


        #region Query CRUD Operations
        /// <summary>
        /// Create a new draft query
        /// </summary>
        /// <returns>New draft query entity</returns>
        Task<Query> CreateDraftQueryAsync();

        /// <summary>
        /// Create a new query with specified data
        /// </summary>
        /// <param name="queryDto">Query creation data</param>
        /// <returns>Created query entity</returns>
        Task<Query> CreateQueryAsync(QueryDto queryDto);

        /// <summary>
        /// Get query by ID with all related entities
        /// </summary>
        /// <param name="id">Query ID</param>
        /// <returns>Query entity or null if not found</returns>
        Task<Query?> GetQueryAsync(int id);

        /// <summary>
        /// Update query basic information
        /// </summary>
        /// <param name="queryId">Query ID</param>
        /// <param name="name">Query name</param>
        /// <param name="description">Query description</param>
        /// <returns>Success indicator</returns>
        Task<bool> UpdateQueryInfoAsync(int queryId, string name, string? description = null);

        /// <summary>
        /// Update query with DTO data
        /// </summary>
        /// <param name="queryId">Query ID</param>
        /// <param name="queryDto">Query data</param>
        /// <returns>Success indicator</returns>
        Task<bool> UpdateQueryAsync(int queryId, QueryDto queryDto);

        /// <summary>
        /// Update query status
        /// </summary>
        /// <param name="queryId">Query ID</param>
        /// <param name="status">New status</param>
        /// <returns>Success indicator</returns>
        Task<bool> UpdateQueryStatusAsync(int queryId, QueryStatus status);

        /// <summary>
        /// Delete query and all related data
        /// </summary>
        /// <param name="id">Query ID</param>
        /// <returns>Success indicator</returns>
        Task<bool> DeleteQueryAsync(int id);

        /// <summary>
        /// Validate query completeness (ORIGINAL Step 4 validation)
        /// </summary>
        /// <param name="queryId">Query ID</param>
        /// <returns>Validation result with errors and warnings</returns>
        Task<QueryValidationResult> ValidateQueryCompletenessAsync(int queryId);

        /// <summary>
        /// Test query execution and validation (Simple test like original)
        /// </summary>
        /// <param name="queryId">Query ID</param>
        /// <returns>Test result with validation and execution details</returns>
        Task<QueryTestResult> TestQueryAsync(int queryId);

        /// <summary>
        /// Perform bulk actions on multiple queries
        /// </summary>
        /// <param name="action">Action to perform (activate, test, delete)</param>
        /// <param name="queryIds">List of query IDs</param>
        /// <returns>Bulk action result</returns>
        Task<BulkActionResult> BulkActionAsync(string action, List<int> queryIds);
        #endregion

        #region Query Constants Management (ORIGINAL Step 4)
        /// <summary>
        /// Save or update a query constant
        /// </summary>
        /// <param name="queryId">Query ID</param>
        /// <param name="constant">Constant data</param>
        /// <returns>Save result with success status and ID</returns>
        Task<SaveResult> SaveConstantAsync(int queryId, QueryConstantDto constant);

        /// <summary>
        /// Delete a query constant with usage validation
        /// </summary>
        /// <param name="queryId">Query ID</param>
        /// <param name="constantId">Constant ID</param>
        /// <returns>Delete result with usage details if applicable</returns>
        Task<DeleteResult> DeleteConstantAsync(int queryId, int constantId);

        /// <summary>
        /// Get specific constant by ID
        /// </summary>
        /// <param name="constantId">Constant ID</param>
        /// <returns>Constant DTO or null if not found</returns>
        Task<QueryConstantDto?> GetConstantAsync(int constantId);

        /// <summary>
        /// Get all constants for a query (both global and local)
        /// </summary>
        /// <param name="queryId">Query ID</param>
        /// <returns>List of query constants</returns>
        Task<List<QueryConstantDto>> GetQueryConstantsAsync(int queryId);

        /// <summary>
        /// Get constant usage details for validation
        /// </summary>
        /// <param name="constantId">Constant ID</param>
        /// <param name="queryId">Query ID</param>
        /// <param name="isGlobal">Whether constant is global</param>
        /// <returns>Usage result with details</returns>
        Task<ConstantUsageResult> GetConstantUsageDetailsAsync(int constantId, int queryId, bool isGlobal);
        #endregion

        #region Query Outputs Management (ORIGINAL Step 4)
        /// <summary>
        /// Save or update a query output
        /// </summary>
        /// <param name="queryId">Query ID</param>
        /// <param name="output">Output data</param>
        /// <returns>Save result with success status and ID</returns>
        Task<SaveResult> SaveOutputAsync(int queryId, QueryOutputDto output);

        /// <summary>
        /// Delete a query output with usage validation
        /// </summary>
        /// <param name="queryId">Query ID</param>
        /// <param name="outputId">Output ID</param>
        /// <returns>Delete result with usage details if applicable</returns>
        Task<DeleteResult> DeleteOutputAsync(int queryId, int outputId);

        /// <summary>
        /// Get specific output by ID
        /// </summary>
        /// <param name="outputId">Output ID</param>
        /// <returns>Output DTO or null if not found</returns>
        Task<QueryOutputDto?> GetOutputAsync(int outputId);

        /// <summary>
        /// Get all outputs for a query
        /// </summary>
        /// <param name="queryId">Query ID</param>
        /// <returns>List of query outputs</returns>
        Task<List<QueryOutputDto>> GetQueryOutputsAsync(int queryId);

        /// <summary>
        /// Get output usage details for validation
        /// </summary>
        /// <param name="outputId">Output ID</param>
        /// <param name="queryId">Query ID</param>
        /// <returns>Usage result with details</returns>
        Task<ConstantUsageResult> GetOutputUsageDetailsAsync(int outputId, int queryId);
        #endregion

        #region Formula Canvas Management (ORIGINAL Step 4)

        /// <summary>
        /// Get formula canvas for a query
        /// </summary>
        /// <param name="queryId">Query ID</param>
        /// <returns>Formula canvas or null if not found</returns>
        Task<FormulaCanvas?> GetFormulaCanvasAsync(int queryId);

        /// <summary>
        /// Update canvas state only
        /// </summary>
        /// <param name="queryId">Query ID</param>
        /// <param name="canvasState">Serialized canvas state</param>
        /// <returns>Success indicator</returns>
        Task<bool> UpdateCanvasStateAsync(int queryId, string canvasState);
        #endregion

        #region Query Builder & Wizard (ORIGINAL Step 4 UI)
        /// <summary>
        /// Get query builder state for wizard interface (without parameters for new query)
        /// </summary>
        /// <returns>Empty query builder view model</returns>
        Task<QueryBuilderViewModel> GetQueryBuilderStateAsync();

        /// <summary>
        /// Get query builder state for wizard interface (with queryId for editing)
        /// </summary>
        /// <param name="queryId">Query ID</param>
        /// <returns>Query builder view model</returns>
        Task<QueryBuilderViewModel> GetQueryBuilderStateAsync(int queryId);

        /// <summary>
        /// Get query list with filtering and pagination
        /// </summary>
        /// <param name="filters">Filter criteria</param>
        /// <returns>Filtered and paginated query list</returns>
        Task<QueryListViewModel> GetQueryListAsync(QueryFiltersViewModel filters);
        #endregion
    }
}