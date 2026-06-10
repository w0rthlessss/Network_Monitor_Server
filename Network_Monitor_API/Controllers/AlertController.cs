using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Network_Monitor_API.DTO;
using Network_Monitor_API.Services;

namespace Network_Monitor_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AlertController : ControllerBase
    {
        private readonly AlertsService _alertsService;

        public AlertController(AlertsService alertsService)
        {
            _alertsService = alertsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _alertsService.GetAllAlertsAsync());

        [HttpGet("active")]
        public async Task<IActionResult> GetActive() =>
            Ok(await _alertsService.GetActiveAlertsAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var alert = await _alertsService.GetAlertByIdAsync(id);
            return alert == null ? NotFound() : Ok(alert);
        }

        [HttpPatch("{id}/resolve")]
        public async Task<IActionResult> Resolve(int id)
        {
            var result = await _alertsService.UpdateAlertStatusByIdAsync(id);
            return result ? NoContent() : NotFound();
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AlertDTO dto)
        {
            var result = await _alertsService.UpdateAlertRecordByIdASync(id, dto);
            return result ? NoContent() : NotFound();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _alertsService.RemoveAlertByIdAsync(id);
            return result ? NoContent() : NotFound();
        }
    }
}
