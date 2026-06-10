using Microsoft.EntityFrameworkCore;
using Network_Monitor_API.Data;
using Network_Monitor_API.DTO;
using Network_Monitor_API.Models.MainDBModels;

namespace Network_Monitor_API.Services
{
    public class ConnectionService
    {
        private readonly MainDBContext _context;
        public ConnectionService(MainDBContext context)
        {
            _context = context;
        }

        public async Task<List<ConnectionDTO>> GetAllConnectionsAsync()
        {
            return await _context.Connections
                .OrderByDescending(c => c.Timestamp)
                .Select(c => new
                {
                    c.Id,
                    c.Timestamp,
                    c.SrcIP,
                    c.DstIP,
                    c.SrcPort,
                    c.DstPort,
                    c.Protocol,
                    c.Service,
                    c.Duration,
                    c.SrcBytes,
                    c.DstBytes,
                    LatestPrediction = c.Predictions
                        .OrderByDescending(p => p.Timestamp)
                        .Select(p => new
                        {
                            p.Result,
                            p.Confidence
                        })
                        .FirstOrDefault()
                })
                .Select(x => new ConnectionDTO
                {
                    Id = x.Id,
                    Timestamp = x.Timestamp,
                    SrcIP = x.SrcIP,
                    DstIP = x.DstIP,
                    SrcPort = x.SrcPort,
                    DstPort = x.DstPort,
                    Protocol = x.Protocol,
                    Service = x.Service,
                    Duration = x.Duration,
                    SrcBytes = x.SrcBytes,
                    DstBytes = x.DstBytes,
                    PredictionResult = x.LatestPrediction != null && x.LatestPrediction.Result == true,
                    PredictionConfidence = x.LatestPrediction != null ? x.LatestPrediction.Confidence : 0.0
                })
                .ToListAsync();
        }

        public async Task<ConnectionDTO?> GetConnectionByIdAsync(int id)
        {
            return await _context.Connections
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.Timestamp,
                    c.SrcIP,
                    c.DstIP,
                    c.SrcPort,
                    c.DstPort,
                    c.Protocol,
                    c.Service,
                    c.Duration,
                    c.SrcBytes,
                    c.DstBytes,
                    LatestPrediction = c.Predictions
                        .OrderByDescending(p => p.Timestamp)
                        .Select(p => new
                        {
                            p.Result,
                            p.Confidence
                        })
                        .FirstOrDefault()
                })
                .Select(x => new ConnectionDTO
                {
                    Id = x.Id,
                    Timestamp = x.Timestamp,
                    SrcIP = x.SrcIP,
                    DstIP = x.DstIP,
                    SrcPort = x.SrcPort,
                    DstPort = x.DstPort,
                    Protocol = x.Protocol,
                    Service = x.Service,
                    Duration = x.Duration,
                    SrcBytes = x.SrcBytes,
                    DstBytes = x.DstBytes,
                    PredictionResult = x.LatestPrediction != null && x.LatestPrediction.Result == true,
                    PredictionConfidence = x.LatestPrediction != null ? x.LatestPrediction.Confidence : 0.0
                })
                .FirstOrDefaultAsync();
        }

        public async Task<PagedResult<ConnectionDTO>> GetConnectionsPagedAsync(int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var query = _context.Connections.OrderByDescending(c => c.Timestamp);

            var total = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Id, c.Timestamp, c.SrcIP, c.DstIP, c.SrcPort, c.DstPort,
                    c.Protocol, c.Service, c.Duration, c.SrcBytes, c.DstBytes,
                    LatestPrediction = c.Predictions
                        .OrderByDescending(p => p.Timestamp)
                        .Select(p => new { p.Result, p.Confidence })
                        .FirstOrDefault()
                })
                .Select(x => new ConnectionDTO
                {
                    Id = x.Id,
                    Timestamp = x.Timestamp,
                    SrcIP = x.SrcIP,
                    DstIP = x.DstIP,
                    SrcPort = x.SrcPort,
                    DstPort = x.DstPort,
                    Protocol = x.Protocol,
                    Service = x.Service,
                    Duration = x.Duration,
                    SrcBytes = x.SrcBytes,
                    DstBytes = x.DstBytes,
                    PredictionResult = x.LatestPrediction != null && x.LatestPrediction.Result,
                    PredictionConfidence = x.LatestPrediction != null ? x.LatestPrediction.Confidence : 0.0
                })
                .ToListAsync();

            return new PagedResult<ConnectionDTO>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<bool> MakeNewConnectionRecordAsync(Connection _connection)
        {
            if (_connection == null)
                return false;
            await _context.Connections.AddAsync(_connection);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateConnectionRecordByIdAsync(int id, ConnectionDTO upd)
        {
            if (upd == null) return false;

            var connection = await _context.Connections.FirstOrDefaultAsync(x => x.Id == id);
            if (connection == null) return false;

            connection.Timestamp = upd.Timestamp;
            connection.SrcIP = upd.SrcIP;
            connection.SrcPort = upd.SrcPort;
            connection.DstIP = upd.DstIP;
            connection.DstPort = upd.DstPort;
            connection.Protocol = upd.Protocol;
            connection.Service = upd.Service;
            connection.Duration = upd.Duration;
            connection.SrcBytes = upd.SrcBytes;
            connection.DstBytes = upd.DstBytes;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveConnectionRecordByIdAsync(int id)
        {
            var connection = await _context.Connections.FirstOrDefaultAsync(x => x.Id == id);
            if (connection == null) return false;

            _context.Connections.Remove(connection);

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
