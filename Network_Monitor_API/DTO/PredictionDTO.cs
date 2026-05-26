namespace Network_Monitor_API.DTO
{
    public class PredictionDTO
    {
        public int Id {get; set;}
        public int ConnectionId { get; set; }
        public string Model { get; set; } = null!;
        public bool Result { get; set; }
        public double Confidence { get; set; }
        public string TopFeature { get; set; } = null!;
        public DateTime Timestamp { get; set; }
    }
}
