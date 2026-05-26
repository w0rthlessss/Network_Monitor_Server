using Microsoft.EntityFrameworkCore;
using Network_Monitor_API.Data;
using Network_Monitor_API.DTO;
using Network_Monitor_API.Models.MainDBModels;

namespace Network_Monitor_API.Services
{
    public class ModelService
    {
        private readonly MainDBContext _context;

        public ModelService(MainDBContext context)
        {
            _context = context;
        }

        public async Task<List<ModelDTO>> GetAllModelsAsync()
        {
            return await _context.Models
                .Include(m => m.User)
                .Select(m => new ModelDTO
                {
                    Username = m.User.Login,
                    Metrics = m.Metrics,
                    ModelPath = m.ModelPath
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
                    Username = m.User.Login,
                    Metrics = m.Metrics,
                    ModelPath = m.ModelPath
                })
                .FirstOrDefaultAsync();
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
