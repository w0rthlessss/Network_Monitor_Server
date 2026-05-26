namespace Network_Monitor_API.Models.MainDBModels
{
    public class User
    {
        public int Id { get; set; }

        public string Login { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public string Role { get; set; } = null!;

        public string Status { get; set; } = null!;

        public virtual ICollection<Model> Models { get; set; } = new List<Model>();
    }
}
