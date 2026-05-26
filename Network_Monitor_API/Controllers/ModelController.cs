using Microsoft.AspNetCore.Mvc;
using Network_Monitor_API.Services;

namespace Network_Monitor_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModelController : ControllerBase
    {
        private readonly ModelService _modelService;

        public ModelController(ModelService modelService)
        {
            _modelService = modelService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _modelService.GetAllModelsAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var model = await _modelService.GetModelByIdAsync(id);
            return model == null ? NotFound() : Ok(model);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _modelService.RemoveModelByIdAsync(id);
            return result ? NoContent() : NotFound();
        }
    }
}
