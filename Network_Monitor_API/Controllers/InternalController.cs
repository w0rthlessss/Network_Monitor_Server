using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Network_Monitor_API.DTO;
using Network_Monitor_API.Filters;
using Network_Monitor_API.Hubs;
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
        private readonly ModelService _modelService;
        private readonly IHubContext<MonitorHub> _hub;

        public InternalController(
            ConnectionService connectionService,
            PredictionService predictionService,
            AlertsService alertsService,
            SystemUsageService systemUsageService,
            ModelService modelService,
            IHubContext<MonitorHub> hub)
        {
            _connectionService = connectionService;
            _predictionService = predictionService;
            _alertsService = alertsService;
            _systemUsageService = systemUsageService;
            _modelService = modelService;
            _hub = hub;
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

            await _hub.Clients.All.SendAsync("ReceiveConnection", new ConnectionDTO
            {
                Id = connection.Id,
                Timestamp = connection.Timestamp,
                SrcIP = connection.SrcIP,
                DstIP = connection.DstIP,
                SrcPort = connection.SrcPort,
                DstPort = connection.DstPort,
                Protocol = connection.Protocol,
                Service = connection.Service,
                Duration = connection.Duration,
                SrcBytes = connection.SrcBytes,
                DstBytes = connection.DstBytes,
                PredictionResult = prediction.Result,
                PredictionConfidence = prediction.Confidence
            });

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

                await _hub.Clients.All.SendAsync("ReceiveAlert", new AlertDTO
                {
                    Id = alert.Id,
                    ConnectionId = alert.ConnectionId,
                    Description = alert.Description,
                    Resolved = alert.Resolved,
                    Timestamp = alert.Timestamp
                });
            }

            return Ok();
        }

        // POST internal/models
        // Вызывается Python-сервисом после успешного обучения новой модели
        [HttpPost("models")]
        public async Task<IActionResult> CreateModel([FromBody] InternalCreateModelRequest request)
        {
            var model = new Network_Monitor_API.Models.MainDBModels.Model
            {
                UserId = request.UserId,
                Metrics = request.Metrics,
                ModelPath = request.ModelPath,
                IsActive = false
            };

            var ok = await _modelService.CreateModelAsync(model);
            if (!ok) return BadRequest();

            var dto = await _modelService.GetModelByIdAsync(model.Id);
            await _hub.Clients.All.SendAsync("ReceiveModel", dto);

            return CreatedAtAction(nameof(CreateModel), new { id = model.Id }, dto);
        }

        // PATCH internal/models/{id}
        // Вызывается Python-сервисом после дообучения существующей модели
        [HttpPatch("models/{id}")]
        public async Task<IActionResult> UpdateModel(int id, [FromBody] InternalUpdateModelRequest request)
        {
            var dto = await _modelService.UpdateModelAsync(id, request.Metrics, request.ModelPath);
            if (dto == null) return NotFound();

            await _hub.Clients.All.SendAsync("ReceiveModel", dto);
            return Ok(dto);
        }

        // GET internal/active-model
        // Вызывается Python-сервисом при старте для восстановления активной модели
        [HttpGet("active-model")]
        public async Task<IActionResult> GetActiveModel()
        {
            var model = await _modelService.GetActiveModelAsync();
            return model == null ? NotFound() : Ok(model);
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

            await _hub.Clients.All.SendAsync("ReceiveSystemUsage", new SystemUsageDTO
            {
                Timestamp = record.Timestamp,
                CpuUsage = record.CpuUsage,
                MemoryUsage = record.MemoryUsage,
                NetworkUsage = record.NetworkUsage,
                ActiveConnections = record.ActiveConnections
            });

            return Ok();
        }
    }
}
