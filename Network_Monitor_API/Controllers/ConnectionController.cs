using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Network_Monitor_API.DTO;
using Network_Monitor_API.Services;

namespace Network_Monitor_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ConnectionController : ControllerBase
    {
        private readonly ConnectionService _connectionService;

        public ConnectionController(ConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50) =>
            Ok(await _connectionService.GetConnectionsPagedAsync(page, pageSize));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var connection = await _connectionService.GetConnectionByIdAsync(id);
            return connection == null ? NotFound() : Ok(connection);
        }

        [Authorize(Roles = "DatabaseAdministrator")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ConnectionDTO dto)
        {
            var result = await _connectionService.UpdateConnectionRecordByIdAsync(id, dto);
            return result ? NoContent() : NotFound();
        }

        [Authorize(Roles = "DatabaseAdministrator")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _connectionService.RemoveConnectionRecordByIdAsync(id);
            return result ? NoContent() : NotFound();
        }
    }
}
