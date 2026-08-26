using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendVisionReportsApi.Controllers
{
    [ApiController, Route("api/devices"), Authorize]
    public class DevicesController(IDevicesService devicesService) : ControllerBase
    {
        [HttpGet]
        public Task<List<DeviceResponse>> GetAll() => devicesService.GetAllAsync();

        [HttpPut("{hikDevIndexCode}/license")]
        public async Task<ActionResult<DeviceResponse>> UpsertLicense(string hikDevIndexCode, DeviceLicenseInput input)
        {
            var device = await devicesService.UpsertLicenseAsync(hikDevIndexCode, input);
            return Ok(device);
        }

        [HttpPost("{hikDevIndexCode}/license/renew")]
        public async Task<ActionResult<DeviceResponse>> Renew(string hikDevIndexCode, DeviceRenewInput input)
        {
            try
            {
                var device = await devicesService.RenewAsync(hikDevIndexCode, input.ExtensionMonths);
                return Ok(device);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpDelete("{hikDevIndexCode}/license")]
        public async Task<IActionResult> DeleteLicense(string hikDevIndexCode)
        {
            if (!await devicesService.DeleteLicenseAsync(hikDevIndexCode)) return NotFound();
            return NoContent();
        }

        [HttpGet("{hikDevIndexCode}/earliest-log-date")]
        public async Task<ActionResult<DeviceEarliestLogResponse>> GetEarliestLogDate(string hikDevIndexCode)
        {
            var earliestDate = await devicesService.GetEarliestLogDateAsync(hikDevIndexCode);
            return Ok(new DeviceEarliestLogResponse(earliestDate));
        }
    }
}
