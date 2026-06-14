using Microsoft.EntityFrameworkCore;
using Network_Monitor_API.Data;
using Network_Monitor_API.DTO;
using Network_Monitor_API.Models.SystemUsageDBModels;
namespace Network_Monitor_API.Services
{
    public class SystemUsageService
    {
        private readonly SystemUsageDbContext _context;

        public SystemUsageService(SystemUsageDbContext context)
        {
            _context = context;
        }

        public async Task<List<SystemUsageDTO>> GetAllRecordsAsync()
        {
            return await _context.SystemUsages
                .OrderByDescending(s => s.Timestamp)
                .Select(s => new SystemUsageDTO
                {
                    Id = s.SystemUsageId,
                    Timestamp = s.Timestamp,
                    NetworkUsage = s.NetworkUsage,
                    MemoryUsage = s.MemoryUsage,
                    CpuUsage = s.CpuUsage,
                    ActiveConnections = s.ActiveConnections
                })
                .ToListAsync();
        }

        public async Task<SystemUsageDTO?> GetLastAsync()
        {
            return await _context.SystemUsages
                .OrderByDescending(s => s.Timestamp)
                .Select(s => new SystemUsageDTO
                {
                    Id = s.SystemUsageId,
                    Timestamp = s.Timestamp,
                    NetworkUsage = s.NetworkUsage,
                    MemoryUsage = s.MemoryUsage,
                    CpuUsage = s.CpuUsage,
                    ActiveConnections = s.ActiveConnections
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> CreateRecordAsync(SystemUsage record)
        {
            if (record == null) return false;
            await _context.SystemUsages.AddAsync(record);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var record = await _context.SystemUsages.FirstOrDefaultAsync(s => s.SystemUsageId == id);
            if (record == null) return false;

            _context.SystemUsages.Remove(record);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<SystemUsageDTO>> GetDayDuringRecordsAsync()
        {
            var since = DateTime.UtcNow.AddHours(-24);
            return await _context.SystemUsages
                .Where(s => s.Timestamp >= since)
                .OrderByDescending(s => s.Timestamp)
                .Select(s => new SystemUsageDTO
                {
                    Id = s.SystemUsageId,
                    Timestamp = s.Timestamp,
                    NetworkUsage = s.NetworkUsage,
                    MemoryUsage = s.MemoryUsage,
                    CpuUsage = s.CpuUsage,
                    ActiveConnections = s.ActiveConnections
                })
                .ToListAsync();
        }
    }
}
