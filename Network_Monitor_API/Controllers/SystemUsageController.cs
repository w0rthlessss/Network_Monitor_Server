using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Network_Monitor_API.Services;

namespace Network_Monitor_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SystemUsageController : ControllerBase
    {
        private readonly SystemUsageService _systemUsageService;

        public SystemUsageController(SystemUsageService systemUsageService)
        {
            _systemUsageService = systemUsageService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _systemUsageService.GetAllRecordsAsync());

        [HttpGet("last")]
        public async Task<IActionResult> GetLast()
        {
            var record = await _systemUsageService.GetLastAsync();
            return record == null ? NotFound() : Ok(record);
        }

        [HttpGet("day")]
        public async Task<IActionResult> GetDay() =>
            Ok(await _systemUsageService.GetDayDuringRecordsAsync());
    }
}
