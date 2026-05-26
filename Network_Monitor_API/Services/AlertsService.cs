using Microsoft.EntityFrameworkCore;
using Network_Monitor_API.Data;
using Network_Monitor_API.DTO;
using Network_Monitor_API.Models.MainDBModels;

namespace Network_Monitor_API.Services
{
    public class AlertsService
    {
        private readonly MainDBContext _context;

        public AlertsService(MainDBContext context)
        {
            _context = context;
        }

        public async Task<List<AlertDTO>> GetActiveAlertsAsync()
        {
            return await _context.Alerts
                .Where(c => c.Resolved == false)
                .OrderByDescending(c => c.Timestamp)
                .Select(c => new AlertDTO
                {
                    Id = c.Id,
                    ConnectionId = c.ConnectionId,
                    Description = c.Description,
                    Resolved = c.Resolved,
                    Timestamp = c.Timestamp
                })
                .ToListAsync<AlertDTO>();

        }

        public async Task<List<AlertDTO>> GetAllAlertsAsync()
        {
            return await _context.Alerts
                .OrderByDescending(c => c.Timestamp)
                .Select(c => new AlertDTO
                {
                    Id = c.Id,
                    ConnectionId = c.ConnectionId,
                    Description = c.Description,
                    Resolved = c.Resolved,
                    Timestamp = c.Timestamp
                })
                .ToListAsync<AlertDTO>();
        }

        public async Task<AlertDTO?> GetAlertByIdAsync(int id)
        {
            return await _context.Alerts
                .Where(c => c.Id == id)
                .Select(c => new AlertDTO
                {
                    Id = c.Id,
                    ConnectionId = c.ConnectionId,
                    Description = c.Description,
                    Resolved = c.Resolved,
                    Timestamp = c.Timestamp
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateAlertStatusByIdAsync(int id)
        {
            var alert = await _context.Alerts.FirstOrDefaultAsync(c => c.Id == id);
            if (alert == null)
                return false;
            alert.Resolved = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CreateNewAlertAsync(Alert alert)
        {
            if (alert == null) return false;

            await _context.Alerts.AddAsync(alert);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAlertRecordByIdASync(int id, AlertDTO upd)
        {
            if (upd == null) return false;

            var record = await _context.Alerts.FirstOrDefaultAsync(a => a.Id == id);
            if (record == null) return false;

            record.ConnectionId = upd.ConnectionId;
            record.Description = upd.Description;
            record.Resolved = upd.Resolved;
            record.Timestamp = upd.Timestamp;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveAlertByIdAsync(int id)
        {
            var alert = await _context.Alerts.FirstOrDefaultAsync(a => a.Id == id);
            if (alert == null) return false;

            _context.Remove(alert);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
