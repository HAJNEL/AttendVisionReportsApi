using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AttendVisionReportsApi.Services;

namespace AttendVisionReportsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthCheckController : ControllerBase
    {
        private readonly IHealthCheckService _healthCheckService;

        public HealthCheckController(IHealthCheckService healthCheckService)
        {
            _healthCheckService = healthCheckService;
        }

        [HttpGet]
        [Route("")]
        [AllowAnonymous]
        public IActionResult Get()
        {
            var status = _healthCheckService.CheckHealth();
            return Ok(new { status });
        }
    }
}
