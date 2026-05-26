namespace Network_Monitor_API.Models.MainDBModels
{
    public class Prediction
    {
        public int Id { get; set; }
        public int ConnectionId { get; set; }
        public int ModelId { get; set; }
        public bool Result { get; set; }
        public double Confidence { get; set; }
        public string TopFeature { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public virtual Connection Connection { get; set; } = null!;
        public virtual Model Model { get; set; } = null!;

    }
}
