using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Network_Monitor_API.Services;

namespace Network_Monitor_API.Hubs
{
    [Authorize]
    public class MonitorHub : Hub
    {
        private readonly AuthService _authService;

        public MonitorHub(AuthService authService)
        {
            _authService = authService;
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var login = Context.User?.Identity?.Name;
            if (login != null)
                await _authService.LogoutAsync(login);

            await base.OnDisconnectedAsync(exception);
        }
    }
}
