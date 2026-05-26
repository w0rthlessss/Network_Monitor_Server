namespace Network_Monitor_API.DTO
{
    public class CreateConnectionRequest
    {
        public DateTime Timestamp { get; set; }
        public int SrcIP { get; set; }
        public int DstIP { get; set; }
        public int SrcPort { get; set; }
        public int DstPort { get; set; }
        public string Protocol { get; set; } = null!;
        public string Service { get; set; } = null!;
        public double Duration { get; set; }
        public long SrcBytes { get; set; }
        public long DstBytes { get; set; }
        public string Traits { get; set; } = null!;
    }

    public class CreatePredictionRequest
    {
        public int ModelId { get; set; }
        public bool Result { get; set; }
        public double Confidence { get; set; }
        public string TopFeature { get; set; } = null!;
        public DateTime Timestamp { get; set; }
    }

    public class TrafficRecordRequest
    {
        public CreateConnectionRequest Connection { get; set; } = null!;
        public CreatePredictionRequest Prediction { get; set; } = null!;
        public string? AlertDescription { get; set; }
    }
}
