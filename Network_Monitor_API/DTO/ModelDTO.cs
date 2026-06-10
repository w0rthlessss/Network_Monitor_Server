namespace Network_Monitor_API.DTO
{
    public class ModelDTO
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string Metrics { get; set; } = null!;
        public string ModelPath { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}
