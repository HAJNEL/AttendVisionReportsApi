using AttendVisionReportsApi.Data;
using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendVisionReportsApi.Services
{
    public class DevicesService(AppDbContext db, IHikCentralService hikCentralService) : IDevicesService
    {
        public async Task<List<DeviceResponse>> GetAllAsync(CancellationToken ct = default)
        {
            var devices = await hikCentralService.GetAccessControlDevicesAsync(1, 500, ct);
            var licenses = await db.DeviceLicenses.ToListAsync(ct);
            var companies = await db.Companies.ToDictionaryAsync(c => c.Id, c => c.Name, ct);

            return devices
                .Where(d => !string.IsNullOrEmpty(d.AcsDevIndexCode))
                .Select(d =>
                {
                    var license = licenses.FirstOrDefault(l => l.HikDevIndexCode == d.AcsDevIndexCode);
                    return Map(d.AcsDevIndexCode!, d.AcsDevName, d.AcsDevIp, d.AcsDevCode, d.Status, license, companies);
                })
                .ToList();
        }

        public async Task<DeviceResponse> UpsertLicenseAsync(string hikDevIndexCode, DeviceLicenseInput input, CancellationToken ct = default)
        {
            var license = await db.DeviceLicenses.FirstOrDefaultAsync(l => l.HikDevIndexCode == hikDevIndexCode, ct);
            if (license is null)
            {
                license = new DeviceLicense { Id = Guid.NewGuid(), HikDevIndexCode = hikDevIndexCode };
                db.DeviceLicenses.Add(license);
            }

            license.CompanyId = input.CompanyId;
            license.Status = input.Status;
            license.IssueDate = input.IssueDate;
            license.ExpiryDate = input.ExpiryDate;
            license.Notes = input.Notes;
            await db.SaveChangesAsync(ct);

            return await GetDeviceResponseAsync(hikDevIndexCode, ct);
        }

        public async Task<DeviceResponse> RenewAsync(string hikDevIndexCode, int extensionMonths, CancellationToken ct = default)
        {
            var license = await db.DeviceLicenses.FirstOrDefaultAsync(l => l.HikDevIndexCode == hikDevIndexCode, ct)
                ?? throw new InvalidOperationException("Device has no license to renew.");

            license.ExpiryDate = license.ExpiryDate.AddMonths(extensionMonths);
            license.Status = "Active";
            await db.SaveChangesAsync(ct);

            return await GetDeviceResponseAsync(hikDevIndexCode, ct);
        }

        public async Task<bool> DeleteLicenseAsync(string hikDevIndexCode)
        {
            var license = await db.DeviceLicenses.FirstOrDefaultAsync(l => l.HikDevIndexCode == hikDevIndexCode);
            if (license is null) return false;
            db.DeviceLicenses.Remove(license);
            await db.SaveChangesAsync();
            return true;
        }

        // HikCentral's door/events search rejects any startTime/endTime span over 31
        // days, so there's no single call that can return the all-time earliest event.
        // Instead we walk backward in windows (30, with a day of margin against off-by-one
        // rejections at the exact 31-day boundary), keeping the earliest event seen, and
        // give up once several windows in a row come back empty (treated as having reached
        // the start of this device's history, tolerating small gaps in logging).
        private const int EventSearchWindowDays = 30;
        private const int MaxEventSearchWindows = 36;
        private const int MaxConsecutiveEmptyWindows = 3;

        public async Task<DateOnly?> GetEarliestLogDateAsync(string hikDevIndexCode, CancellationToken ct = default)
        {
            DateOnly? earliest = null;
            var windowEnd = DateTimeOffset.Now;
            var consecutiveEmpty = 0;

            for (var i = 0; i < MaxEventSearchWindows; i++)
            {
                var windowStart = windowEnd.AddDays(-EventSearchWindowDays);

                var request = new HikCentralAcsEventSearchRequest(
                    StartTime: FormatHikTime(windowStart),
                    EndTime: FormatHikTime(windowEnd),
                    PageNo: 1,
                    PageSize: 1,
                    PersonName: null,
                    DoorIndexCodes: [hikDevIndexCode],
                    EventType: null,
                    SortField: "SwipeTime",
                    OrderType: 0
                );

                var result = await hikCentralService.SearchAccessEventsAsync(request, ct);
                var eventTime = result.List?.FirstOrDefault()?.EventTime;

                if (eventTime is not null && DateTimeOffset.TryParse(eventTime, out var parsed))
                {
                    earliest = DateOnly.FromDateTime(parsed.LocalDateTime);
                    consecutiveEmpty = 0;
                }
                else if (++consecutiveEmpty >= MaxConsecutiveEmptyWindows)
                {
                    break;
                }

                windowEnd = windowStart;
            }

            return earliest;
        }

        private static string FormatHikTime(DateTimeOffset value) => value.ToString("yyyy-MM-ddTHH:mm:sszzz");

        private async Task<DeviceResponse> GetDeviceResponseAsync(string hikDevIndexCode, CancellationToken ct)
        {
            var devices = await hikCentralService.GetAccessControlDevicesAsync(1, 500, ct);
            var device = devices.FirstOrDefault(d => d.AcsDevIndexCode == hikDevIndexCode)
                ?? throw new InvalidOperationException("Device not found in HikCentral.");
            var license = await db.DeviceLicenses.FirstOrDefaultAsync(l => l.HikDevIndexCode == hikDevIndexCode, ct);
            var companies = await db.Companies.ToDictionaryAsync(c => c.Id, c => c.Name, ct);
            return Map(hikDevIndexCode, device.AcsDevName, device.AcsDevIp, device.AcsDevCode, device.Status, license, companies);
        }

        private static DeviceResponse Map(
            string hikDevIndexCode, string? name, string? ip, string? serialCode, int? onlineStatus,
            DeviceLicense? license, Dictionary<Guid, string> companies)
        {
            string? companyName = license?.CompanyId is Guid cid && companies.TryGetValue(cid, out var n) ? n : null;
            return new DeviceResponse(
                hikDevIndexCode,
                name,
                ip,
                serialCode,
                onlineStatus,
                license?.Id,
                license?.CompanyId,
                companyName,
                license?.Status ?? "Not Licensed",
                license?.IssueDate,
                license?.ExpiryDate,
                license?.Notes
            );
        }
    }
}
