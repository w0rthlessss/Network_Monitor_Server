namespace Network_Monitor_API.DTO
{
    public class SystemUsageDTO
    {
        public int Id {get; set;}
        public DateTime Timestamp { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double NetworkUsage { get; set; }
        public int ActiveConnections { get; set; }
    }
}
