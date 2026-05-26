namespace Network_Monitor_API.DTO
{
    public class ConnectionDTO
    {
        public int Id {get; set;}
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
        public bool PredictionResult { get; set; }
        public double PredictionConfidence { get; set; }
    }
}
