namespace Network_Monitor_API.Models.MainDBModels
{
    public class Connection
    {
        public int Id { get; set; }
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
        public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();
        public virtual ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
    }
}
