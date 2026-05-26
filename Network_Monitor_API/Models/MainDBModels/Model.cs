namespace Network_Monitor_API.Models.MainDBModels
{
    public class Model
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Metrics { get; set; } = null!;
        public string ModelPath { get; set; } = null!;

        public virtual User User { get; set; } = null!;
        public virtual ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
    }
}
