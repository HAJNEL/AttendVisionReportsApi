using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface ICompanyService
    {
        Task<IEnumerable<CompanyDto>> GetAllAsync();
        Task<CompanyDto?> GetByIdAsync(Guid id);
        Task<CompanyDto> CreateAsync(CreateCompanyDto dto);
        Task<bool> UpdateAsync(Guid id, UpdateCompanyDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
