namespace Network_Monitor_API.DTO
{
    public class CreateSystemUsageRequest
    {
        public DateTime Timestamp { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double NetworkUsage { get; set; }
        public int ActiveConnections { get; set; }
    }
}
