using System.Text.Json;

namespace Network_Monitor_API.DTO
{
    public class TrainModelRequest
    {
        public int? ModelId { get; set; }
        public Dictionary<string, JsonElement> Hyperparameters { get; set; } = new();
    }

    public class InternalCreateModelRequest
    {
        public int UserId { get; set; }
        public string Metrics { get; set; } = null!;
        public string ModelPath { get; set; } = null!;
    }

    public class InternalUpdateModelRequest
    {
        public string Metrics { get; set; } = null!;
        public string ModelPath { get; set; } = null!;
    }
}
