using AttendVisionReportsApi.DTOs;

namespace AttendVisionReportsApi.Services
{
    public interface IDevicesService
    {
        Task<List<DeviceResponse>> GetAllAsync(CancellationToken ct = default);
        Task<DeviceResponse> UpsertLicenseAsync(string hikDevIndexCode, DeviceLicenseInput input, CancellationToken ct = default);
        Task<DeviceResponse> RenewAsync(string hikDevIndexCode, int extensionMonths, CancellationToken ct = default);
        Task<bool> DeleteLicenseAsync(string hikDevIndexCode);
        Task<DateOnly?> GetEarliestLogDateAsync(string hikDevIndexCode, CancellationToken ct = default);
    }
}
