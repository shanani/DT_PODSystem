using System.Collections.Generic;
using System.Threading.Tasks;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.Entities;

namespace DT_PODSystem.Services.Interfaces
{
    public interface ILookupsService
    {
        // Category operations
        Task<List<Category>> GetCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<ServiceResult<Category>> CreateCategoryAsync(Category category);
        Task<ServiceResult<Category>> UpdateCategoryAsync(int id, CategoryDto categoryDto);
        Task<ServiceResult<bool>> DeleteCategoryAsync(int id);

        // Vendor operations
        Task<List<Vendor>> GetVendorsAsync();
        Task<Vendor?> GetVendorByIdAsync(int id);
        Task<ServiceResult<Vendor>> CreateVendorAsync(Vendor vendor);
        Task<ServiceResult<Vendor>> UpdateVendorAsync(int id, VendorDto vendorDto);
        Task<ServiceResult<bool>> DeleteVendorAsync(int id);

        // Department operations
        Task<List<Department>> GetDepartmentsAsync();
        Task<Department?> GetDepartmentByIdAsync(int id);
        Task<ServiceResult<Department>> CreateDepartmentAsync(Department department);
        Task<ServiceResult<Department>> UpdateDepartmentAsync(int id, DepartmentDto departmentDto);
        Task<ServiceResult<bool>> DeleteDepartmentAsync(int id);

        // General Directorate operations
        Task<List<GeneralDirectorate>> GetGeneralDirectoratesAsync();
        Task<GeneralDirectorate?> GetGeneralDirectorateByIdAsync(int id);
        Task<ServiceResult<GeneralDirectorate>> CreateGeneralDirectorateAsync(GeneralDirectorate gd);
        Task<ServiceResult<GeneralDirectorate>> UpdateGeneralDirectorateAsync(int id, GeneralDirectorateDto gdDto);
        Task<ServiceResult<bool>> DeleteGeneralDirectorateAsync(int id);

        // Combined operations
        Task<OrganizationalHierarchyDto> GetOrganizationalHierarchyAsync();

        // Utility operations
        Task<LookupUsageDetailsDto> GetUsageDetailsAsync(string entityType, int id);
        Task<ServiceResult<bool>> ToggleStatusAsync(string entityType, int id);

        Task<bool> IsNameUniqueAsync(string entityType, string name, int? excludeId = null);
    }

    // Service Result wrapper
    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ServiceResult<T> SuccessResult(T data, string message = "Operation completed successfully")
        {
            return new ServiceResult<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ServiceResult<T> ErrorResult(string message, T? data = default)
        {
            return new ServiceResult<T>
            {
                Success = false,
                Message = message,
                Data = data
            };
        }
    }
}