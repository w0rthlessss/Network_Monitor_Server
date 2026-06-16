using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Network_Monitor_API.DTO;
using Network_Monitor_API.Services;

namespace Network_Monitor_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ModelController : ControllerBase
    {
        private readonly ModelService _modelService;

        public ModelController(ModelService modelService)
        {
            _modelService = modelService;
        }
        [Authorize(Roles = "DatabaseAdministrator")]
        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _modelService.GetAllModelsAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var model = await _modelService.GetModelByIdAsync(id);
            return model == null ? NotFound() : Ok(model);
        }

        [Authorize(Roles = "MathSpecialist")]
        [HttpPatch("{id}/activate")]
        public async Task<IActionResult> Activate(int id)
        {
            var result = await _modelService.ActivateModelAsync(id);
            return result ? NoContent() : NotFound();
        }

        [Authorize(Roles = "MathSpecialist")]
        [HttpPost("train")]
        public async Task<IActionResult> Train([FromBody] TrainModelRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return Unauthorized();

            try
            {
                var result = await _modelService.TrainModelAsync(
                    request.ModelId, userId, request.Hyperparameters);
                return result == null ? NotFound() : Accepted();
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, "Python service unavailable");
            }
        }

        [Authorize(Roles = "MathSpecialist")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _modelService.RemoveModelByIdAsync(id);
            return result ? NoContent() : NotFound();
        }
    }
}
