using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Network_Monitor_API.Data;
using Network_Monitor_API.DTO;
using Network_Monitor_API.Models.MainDBModels;

namespace Network_Monitor_API.Services
{
    public class ModelService
    {
        private readonly MainDBContext _context;
        private readonly PythonServiceClient _pythonClient;

        public ModelService(MainDBContext context, PythonServiceClient pythonClient)
        {
            _context = context;
            _pythonClient = pythonClient;
        }

        public async Task<List<ModelDTO>> GetAllModelsAsync()
        {
            return await _context.Models
                .Include(m => m.User)
                .Select(m => new ModelDTO
                {
                    Id = m.Id,
                    Username = m.User.Login,
                    Metrics = m.Metrics,
                    ModelPath = m.ModelPath,
                    IsActive = m.IsActive
                })
                .ToListAsync();
        }

        public async Task<ModelDTO?> GetModelByIdAsync(int id)
        {
            return await _context.Models
                .Include(m => m.User)
                .Where(m => m.Id == id)
                .Select(m => new ModelDTO
                {
                    Id = m.Id,
                    Username = m.User.Login,
                    Metrics = m.Metrics,
                    ModelPath = m.ModelPath,
                    IsActive = m.IsActive
                })
                .FirstOrDefaultAsync();
        }

        public async Task<ActiveModelDTO?> GetActiveModelAsync()
        {
            return await _context.Models
                .Where(m => m.IsActive)
                .Select(m => new ActiveModelDTO
                {
                    Id = m.Id,
                    ModelPath = m.ModelPath
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ActivateModelAsync(int id)
        {
            var target = await _context.Models.FirstOrDefaultAsync(m => m.Id == id);
            if (target == null) return false;

            var current = await _context.Models.FirstOrDefaultAsync(m => m.IsActive);
            if (current != null) current.IsActive = false;

            target.IsActive = true;
            await _context.SaveChangesAsync();

            try
            {
                await _pythonClient.SetActiveModelAsync(
                    new PythonServiceClient.ActiveModelPayload(target.Id, target.ModelPath));
            }
            catch (HttpRequestException)
            {
                // Python-сервис недоступен — модель активирована в БД,
                // Python подхватит её при следующем старте через GET /internal/active-model
            }

            return true;
        }

        // null = модель не найдена, throws HttpRequestException = Python недоступен
        public async Task<bool?> TrainModelAsync(int? modelId, int userId, Dictionary<string, JsonElement> hyperparameters)
        {
            if (modelId.HasValue && !await _context.Models.AnyAsync(m => m.Id == modelId.Value))
                return null;

            await _pythonClient.TrainModelAsync(
                new PythonServiceClient.TrainPayload(modelId, userId, hyperparameters));
            return true;
        }

        public async Task<ModelDTO?> UpdateModelAsync(int id, string metrics, string modelPath)
        {
            var model = await _context.Models
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (model == null) return null;

            model.Metrics = metrics;
            model.ModelPath = modelPath;
            await _context.SaveChangesAsync();

            return new ModelDTO
            {
                Id = model.Id,
                Username = model.User.Login,
                Metrics = model.Metrics,
                ModelPath = model.ModelPath,
                IsActive = model.IsActive
            };
        }

        public async Task<bool> CreateModelAsync(Model model)
        {
            if (model == null) return false;
            await _context.Models.AddAsync(model);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveModelByIdAsync(int id)
        {
            var model = await _context.Models.FirstOrDefaultAsync(m => m.Id == id);
            if (model == null) return false;
            _context.Models.Remove(model);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
