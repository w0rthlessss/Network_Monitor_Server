using Microsoft.EntityFrameworkCore;
using Network_Monitor_API.Data;
using Network_Monitor_API.DTO;
using Network_Monitor_API.Models.MainDBModels;

namespace Network_Monitor_API.Services
{
    public class UserService
    {
        private readonly MainDBContext _context;

        public UserService(MainDBContext context)
        {
            _context = context;
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
