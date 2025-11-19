using IdentityService.Entities;
using IdentityService.Repositories;
using Repositories.Entities.Enums;
using Shared.DTOs;
using Shared.Utilities;
using BCrypt.Net;

namespace IdentityService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtTokenGenerator _tokenGenerator;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            JwtTokenGenerator tokenGenerator,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _tokenGenerator = tokenGenerator;
            _logger = logger;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid username or password");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Account is inactive");
            }

            // Generate tokens
            var accessToken = _tokenGenerator.GenerateAccessToken(
                user.UserId,
                user.Username,
                user.Email,
                user.Role.ToString());

            var refreshToken = _tokenGenerator.GenerateRefreshToken();

            // Save refresh token
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User {Username} logged in successfully", user.Username);

            return new LoginResponse
            {
                Token = accessToken,
                RefreshToken = refreshToken,
                User = MapToUserDto(user),
                ExpiresAt = DateTime.UtcNow.AddHours(2)
            };
        }

        public async Task<UserDto> RegisterAsync(RegisterRequest request)
        {
            // Check if user already exists
            if (await _userRepository.UsernameExistsAsync(request.Username))
            {
                throw new InvalidOperationException("Username already exists");
            }

            if (await _userRepository.EmailExistsAsync(request.Email))
            {
                throw new InvalidOperationException("Email already exists");
            }

            // Parse role
            if (!Enum.TryParse<UserRole>(request.Role, out var role))
            {
                throw new ArgumentException("Invalid role");
            }

            // Create new user
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(user);

            _logger.LogInformation("New user registered: {Username}", user.Username);

            return MapToUserDto(user);
        }

        public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken);

            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Invalid or expired refresh token");
            }

            // Generate new tokens
            var accessToken = _tokenGenerator.GenerateAccessToken(
                user.UserId,
                user.Username,
                user.Email,
                user.Role.ToString());

            var newRefreshToken = _tokenGenerator.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userRepository.UpdateAsync(user);

            return new LoginResponse
            {
                Token = accessToken,
                RefreshToken = newRefreshToken,
                User = MapToUserDto(user),
                ExpiresAt = DateTime.UtcNow.AddHours(2)
            };
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return user != null ? MapToUserDto(user) : null;
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
