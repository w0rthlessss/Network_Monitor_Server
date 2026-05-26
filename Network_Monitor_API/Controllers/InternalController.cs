using Microsoft.AspNetCore.Mvc;
using Network_Monitor_API.DTO;
using Network_Monitor_API.Filters;
using Network_Monitor_API.Models.MainDBModels;
using Network_Monitor_API.Models.SystemUsageDBModels;
using Network_Monitor_API.Services;

namespace Network_Monitor_API.Controllers
{
    [Route("internal")]
    [ApiController]
    [ApiKey]
    public class InternalController : ControllerBase
    {
        private readonly ConnectionService _connectionService;
        private readonly PredictionService _predictionService;
        private readonly AlertsService _alertsService;
        private readonly SystemUsageService _systemUsageService;

        public InternalController(
            ConnectionService connectionService,
            PredictionService predictionService,
            AlertsService alertsService,
            SystemUsageService systemUsageService)
        {
            _connectionService = connectionService;
            _predictionService = predictionService;
            _alertsService = alertsService;
            _systemUsageService = systemUsageService;
        }

        // POST internal/traffic
        // Вызывается Python-сервисом: сохраняет connection + prediction, опционально alert
        [HttpPost("traffic")]
        public async Task<IActionResult> PostTraffic([FromBody] TrafficRecordRequest request)
        {
            var connection = new Connection
            {
                Timestamp = request.Connection.Timestamp,
                SrcIP = request.Connection.SrcIP,
                DstIP = request.Connection.DstIP,
                SrcPort = request.Connection.SrcPort,
                DstPort = request.Connection.DstPort,
                Protocol = request.Connection.Protocol,
                Service = request.Connection.Service,
                Duration = request.Connection.Duration,
                SrcBytes = request.Connection.SrcBytes,
                DstBytes = request.Connection.DstBytes,
                Traits = request.Connection.Traits
            };

            await _connectionService.MakeNewConnectionRecordAsync(connection);

            var prediction = new Prediction
            {
                ConnectionId = connection.Id,
                ModelId = request.Prediction.ModelId,
                Result = request.Prediction.Result,
                Confidence = request.Prediction.Confidence,
                TopFeature = request.Prediction.TopFeature,
                Timestamp = request.Prediction.Timestamp
            };

            await _predictionService.CreateNewPredictionRecordAsync(prediction);

            if (request.AlertDescription != null)
            {
                var alert = new Alert
                {
                    ConnectionId = connection.Id,
                    Description = request.AlertDescription,
                    Resolved = false,
                    Timestamp = DateTime.UtcNow
                };
                await _alertsService.CreateNewAlertAsync(alert);
            }

            return Ok();
        }

        // POST internal/system-usage
        // Вызывается Python-сервисом: сохраняет метрики системы
        [HttpPost("system-usage")]
        public async Task<IActionResult> PostSystemUsage([FromBody] CreateSystemUsageRequest request)
        {
            var record = new SystemUsage
            {
                Timestamp = request.Timestamp,
                CpuUsage = request.CpuUsage,
                MemoryUsage = request.MemoryUsage,
                NetworkUsage = request.NetworkUsage,
                ActiveConnections = request.ActiveConnections
            };

            await _systemUsageService.CreateRecordAsync(record);
            return Ok();
        }
    }
}
