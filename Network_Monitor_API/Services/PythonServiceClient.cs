using System.Text.Json;

namespace Network_Monitor_API.Services
{
    public class PythonServiceClient
    {
        private readonly HttpClient _httpClient;

        public PythonServiceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task SetActiveModelAsync(ActiveModelPayload payload)
        {
            await _httpClient.PostAsJsonAsync("/set-active-model", payload);
        }

        public async Task TrainModelAsync(TrainPayload payload)
        {
            await _httpClient.PostAsJsonAsync("/train", payload);
        }

        public record ActiveModelPayload(int ModelId, string ModelPath);
        public record TrainPayload(int? ModelId, int UserId, Dictionary<string, JsonElement> Hyperparameters);
    }
}
