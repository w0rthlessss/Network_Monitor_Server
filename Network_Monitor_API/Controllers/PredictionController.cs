using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Network_Monitor_API.DTO;
using Network_Monitor_API.Services;

namespace Network_Monitor_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PredictionController : ControllerBase
    {
        private readonly PredictionService _predictionService;

        public PredictionController(PredictionService predictionService)
        {
            _predictionService = predictionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _predictionService.GetAllPredictionsAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var prediction = await _predictionService.GetPredictionByIdAsync(id);
            return prediction == null ? NotFound() : Ok(prediction);
        }

        [HttpGet("connection/{connectionId}")]
        public async Task<IActionResult> GetByConnectionId(int connectionId) =>
            Ok(await _predictionService.GetPredictionsByConnectionIdAsync(connectionId));

        [Authorize(Roles = "DatabaseAdministrator")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PredictionDTO dto)
        {
            var result = await _predictionService.UpdatePredictionRecordByIdAsync(id, dto);
            return result ? NoContent() : NotFound();
        }

        [Authorize(Roles = "DatabaseAdministrator")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _predictionService.RemovePredictionRecordByIdAsync(id);
            return result ? NoContent() : NotFound();
        }
    }
}
