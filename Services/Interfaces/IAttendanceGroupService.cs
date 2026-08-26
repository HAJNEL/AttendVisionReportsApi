using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface IAttendanceGroupService
    {
        Task<IEnumerable<AttendanceGroupDto>> GetAllAsync();
        Task<AttendanceGroupDto?> GetByIdAsync(Guid id);
        Task<AttendanceGroupDto> CreateAsync(CreateAttendanceGroupDto dto);
        Task<AttendanceGroupDto?> UpdateAsync(Guid id, UpdateAttendanceGroupDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> SetMembersAsync(Guid groupId, List<Guid> employeeIds);
    }
}
