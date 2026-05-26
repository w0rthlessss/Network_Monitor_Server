namespace Network_Monitor_API.Models.MainDBModels
{
    public class Alert
    {
        public int Id { get; set; }

        public int ConnectionId { get; set; }

        public string Description { get; set; } = null!;

        public bool Resolved { get; set; }

        public DateTime Timestamp { get; set; }

        public virtual Connection Connection { get; set; } = null!;
    }
}
