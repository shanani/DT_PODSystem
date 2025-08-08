using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.Entities;
using DT_PODSystem.Models.ViewModels;

namespace DT_PODSystem.Services.Interfaces
{
    /// <summary>
    /// POD Service Interface - Basic CRUD operations for POD management
    /// POD is the parent business entity that contains organizational logic and templates as children
    /// </summary>
    public interface IPODService
    {
        #region Basic CRUD Operations

        /// <summary>
        /// Create a new POD with business information
        /// </summary>
        Task<POD> CreatePODAsync(PODCreationDto podData);

        /// <summary>
        /// Get POD by ID with all related data
        /// </summary>
        Task<POD?> GetPODAsync(int id);

        /// <summary>
        /// Update existing POD
        /// </summary>
        Task<bool> UpdatePODAsync(int id, PODUpdateDto podData);

        /// <summary>
        /// Delete POD (soft delete - sets IsActive = false)
        /// </summary>
        Task<bool> DeletePODAsync(int id);

        /// <summary>
        /// Get paginated list of PODs with filtering
        /// </summary>
        Task<PODListViewModel> GetPODListAsync(PODFiltersViewModel filters);

        /// <summary>
        /// Get all PODs for dropdown/selection lists
        /// </summary>
        Task<List<PODSelectionDto>> GetPODsForSelectionAsync(bool activeOnly = true);

        #endregion

        #region Validation

        /// <summary>
        /// Validate POD data for creation/update
        /// </summary>
        Task<PODValidationResult> ValidatePODAsync(PODCreationDto podData, int? existingPodId = null);

        /// <summary>
        /// Generate unique POD code
        /// </summary>
        Task<string> GeneratePODCodeAsync(string baseName);

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get POD display name with code
        /// </summary>
        string GetPODDisplayName(POD pod);
        Task<PODDto?> GetPODWithEntriesAsync(int id);

        Task<bool> SavePODEntriesFromJsonAsync(int podId, dynamic entriesJson);

        #endregion
    }
}