namespace Network_Monitor_API.DTO
{
    public class AlertDTO
    {
        public int Id {get; set;}
        public int ConnectionId { get; set; }

        public string Description { get; set; } = null!;

        public bool Resolved { get; set; }

        public DateTime Timestamp { get; set; }

    }
}
