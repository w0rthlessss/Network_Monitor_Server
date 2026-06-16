using Microsoft.EntityFrameworkCore;
using Network_Monitor_API.Data;
using Network_Monitor_API.DTO;
using Network_Monitor_API.Models.MainDBModels;

namespace Network_Monitor_API.Services
{
    public class UserService
    {
        private readonly MainDBContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(MainDBContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Вызывается при старте приложения: если в таблице пользователей пусто,
        // создаёт админа из учётных данных, заданных в .env (DEFAULT_ADMIN_LOGIN / DEFAULT_ADMIN_PASS).
        public async Task SeedDefaultAdminIfEmptyAsync(string? login, string? password)
        {
            if (await _context.Users.AnyAsync())
                return;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning(
                    "Users table is empty but DefaultAdmin:Login/Password is not configured — skipping admin seed.");
                return;
            }

            var admin = new User
            {
                Login = login,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = "DatabaseAdministrator",
                Status = "Offline"
            };

            await _context.Users.AddAsync(admin);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Seeded default admin user '{Login}'.", login);
        }

        public async Task<List<UserDTO>> GetAllAsync()
        {
            return await _context.Users
                .Select(u => new UserDTO
                {
                    Id = u.Id,
                    Login = u.Login,
                    Role = u.Role,
                    Status = u.Status
                })
                .ToListAsync();
        }

        public async Task<UserDTO?> GetByIdAsync(int id)
        {
            return await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new UserDTO
                {
                    Id = u.Id,
                    Login = u.Login,
                    Role = u.Role,
                    Status = u.Status
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> CreateAsync(CreateUserRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Login == request.Login))
                return false;

            var user = new User
            {
                Login = request.Login,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                Status = "Offline"
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return false;

            user.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangePasswordAsync(int id, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
