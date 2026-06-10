namespace Network_Monitor_API.DTO
{
    public class UserDTO
    {
        public int Id { get; set; }
        public string Login { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string Status { get; set; } = null!;
    }

    public class CreateUserRequest
    {
        public string Login { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Role { get; set; } = null!;
    }
}
