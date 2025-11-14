using Shared.DTOs;

namespace IdentityService.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<UserDto> RegisterAsync(RegisterRequest request);
        Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task<UserDto?> GetUserByIdAsync(Guid userId);
    }
}
