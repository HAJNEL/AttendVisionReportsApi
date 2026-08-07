using AttendVisionReportsApi.DTOs;
using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendVisionReportsApi.Controllers
{
    [ApiController, Route("api/hikcentral"), Authorize]
    public class HikCentralController(IHikCentralService hikCentralService) : ControllerBase
    {
        [HttpGet("persons")]
        public Task<HikCentralPersonListData> SearchPersons(
            [FromQuery] int pageNo = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? orgIndexCode = null,
            [FromQuery] string? personName = null) =>
            hikCentralService.SearchPersonsAsync(new HikCentralPersonSearchRequest(pageNo, pageSize, orgIndexCode, personName));

        [HttpGet("persons/{personId}")]
        public async Task<ActionResult<HikCentralPerson>> GetPerson(string personId)
        {
            var person = await hikCentralService.GetPersonAsync(personId);
            if (person is null) return NotFound();
            return Ok(person);
        }

        [HttpPost("persons")]
        public async Task<ActionResult<object>> AddPerson(HikCentralAddPersonRequest request)
        {
            var personId = await hikCentralService.AddPersonAsync(request);
            return CreatedAtAction(nameof(GetPerson), new { personId }, new { personId });
        }

        [HttpPut("persons/{personId}")]
        public async Task<IActionResult> UpdatePerson(string personId, HikCentralUpdatePersonRequest request)
        {
            if (personId != request.PersonId) return BadRequest("personId in the route must match the request body.");
            await hikCentralService.UpdatePersonAsync(request);
            return NoContent();
        }

        [HttpDelete("persons/{personId}")]
        public async Task<IActionResult> DeletePerson(string personId)
        {
            await hikCentralService.DeletePersonAsync(personId);
            return NoContent();
        }

        [HttpGet("organizations")]
        public Task<HikCentralOrgListData> GetOrganizations([FromQuery] int pageNo = 1, [FromQuery] int pageSize = 100) =>
            hikCentralService.GetOrganizationsAsync(pageNo, pageSize);

        [HttpGet("access-devices")]
        public Task<List<HikCentralAcsDevice>> GetAccessDevices([FromQuery] int pageNo = 1, [FromQuery] int pageSize = 100) =>
            hikCentralService.GetAccessControlDevicesAsync(pageNo, pageSize);

        [HttpPost("access-events/search")]
        public Task<HikCentralAcsEventListData> SearchAccessEvents(HikCentralAcsEventSearchRequest request) =>
            hikCentralService.SearchAccessEventsAsync(request);

        [HttpPost("doors/control")]
        public async Task<IActionResult> ControlDoor(HikCentralDoorControlRequest request)
        {
            await hikCentralService.ControlDoorAsync(request);
            return NoContent();
        }

        [HttpGet("access-levels")]
        public Task<HikCentralAccessLevelListData> GetAccessLevels([FromQuery] int pageNo = 1, [FromQuery] int pageSize = 100) =>
            hikCentralService.GetAccessLevelsAsync(pageNo, pageSize);

        [HttpPost("access-levels/{privilegeGroupId}/assign")]
        public async Task<IActionResult> AssignAccessLevel(string privilegeGroupId, [FromBody] AssignAccessLevelBody body)
        {
            await hikCentralService.AssignAccessLevelAsync(privilegeGroupId, body.PersonId);
            return NoContent();
        }
    }

    public record AssignAccessLevelBody(string PersonId);
}
