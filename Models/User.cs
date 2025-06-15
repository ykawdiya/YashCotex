using System;

namespace WeighbridgeSoftwareYashCotex.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastLogin { get; set; }
        public DateTime? LastPasswordChange { get; set; }
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutUntil { get; set; }
        public string? RecoveryEmail { get; set; }
    }

    public enum UserRole
    {
        User = 1,
        Admin = 2,
        SuperAdmin = 3
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; } = false;
    }

    public class LoginResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public User? User { get; set; }
        public bool IsLockedOut { get; set; } = false;
        public TimeSpan? LockoutDuration { get; set; }
    }

    public class UserSession
    {
        public int SessionId { get; set; }
        public int UserId { get; set; }
        public string SessionToken { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    public class PrivilegeEscalationRequest
    {
        public UserRole RequiredRole { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; } = DateTime.Now;
    }
}