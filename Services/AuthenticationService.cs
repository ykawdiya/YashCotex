using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using WeighbridgeSoftwareYashCotex.Models;

namespace WeighbridgeSoftwareYashCotex.Services
{
    public class AuthenticationService : IDisposable
    {
        private readonly DatabaseService _databaseService;
        private readonly List<User> _users;
        private readonly List<UserSession> _activeSessions;
        private readonly Dictionary<int, System.Timers.Timer> _sessionTimers;
        private readonly Dictionary<int, System.Timers.Timer> _privilegeTimers;
        private readonly Dictionary<int, UserRole> _escalatedPrivileges;
        
        public User? CurrentUser { get; private set; }
        public UserRole CurrentRole => _escalatedPrivileges.ContainsKey(CurrentUser?.UserId ?? 0) 
            ? _escalatedPrivileges[CurrentUser.UserId] 
            : CurrentUser?.Role ?? UserRole.User;
        
        public event EventHandler<User>? UserLoggedIn;
        public event EventHandler? UserLoggedOut;
        public event EventHandler<UserRole>? PrivilegeEscalated;
        public event EventHandler? PrivilegeExpired;
        public event EventHandler<string>? SessionExpired;

        public AuthenticationService()
        {
            _databaseService = new DatabaseService();
            _users = new List<User>();
            _activeSessions = new List<UserSession>();
            _sessionTimers = new Dictionary<int, System.Timers.Timer>();
            _privilegeTimers = new Dictionary<int, System.Timers.Timer>();
            _escalatedPrivileges = new Dictionary<int, UserRole>();
            
            
            InitializeDefaultUsers();
        }

        private void InitializeDefaultUsers()
        {
            // Create default users if they don't exist
            var defaultUsers = new List<User>
            {
                new User
                {
                    UserId = 1,
                    Username = "admin",
                    FullName = "System Administrator",
                    Role = UserRole.SuperAdmin,
                    Email = "admin@yashcotex.com",
                    IsActive = true,
                    CreatedDate = DateTime.Now.AddDays(-30)
                },
                new User
                {
                    UserId = 2,
                    Username = "manager",
                    FullName = "Operations Manager",
                    Role = UserRole.Admin,
                    Email = "manager@yashcotex.com",
                    IsActive = true,
                    CreatedDate = DateTime.Now.AddDays(-15)
                },
                new User
                {
                    UserId = 3,
                    Username = "operator1",
                    FullName = "Weighbridge Operator",
                    Role = UserRole.User,
                    Email = "operator1@yashcotex.com",
                    IsActive = true,
                    CreatedDate = DateTime.Now.AddDays(-7)
                }
            };

            foreach (var user in defaultUsers)
            {
                var (hash, salt) = HashPassword("password123"); // Default password
                user.PasswordHash = hash;
                user.Salt = salt;
                user.LastPasswordChange = DateTime.Now.AddDays(-30);
                _users.Add(user);
            }
        }

        #region Authentication Methods

        public async Task<LoginResult> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = _users.FirstOrDefault(u => u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase));
                
                if (user == null)
                {
                    return new LoginResult
                    {
                        IsSuccess = false,
                        Message = "Invalid username or password"
                    };
                }

                // Check if account is locked
                if (user.LockoutUntil.HasValue && user.LockoutUntil > DateTime.Now)
                {
                    var remaining = user.LockoutUntil.Value - DateTime.Now;
                    return new LoginResult
                    {
                        IsSuccess = false,
                        IsLockedOut = true,
                        Message = $"Account is locked. Try again in {remaining.Minutes} minutes.",
                        LockoutDuration = remaining
                    };
                }

                // Check if account is active
                if (!user.IsActive)
                {
                    return new LoginResult
                    {
                        IsSuccess = false,
                        Message = "Account is disabled"
                    };
                }

                // Verify password
                if (!VerifyPassword(request.Password, user.PasswordHash, user.Salt))
                {
                    user.FailedLoginAttempts++;
                    
                    // Lock account after 5 failed attempts
                    if (user.FailedLoginAttempts >= 5)
                    {
                        user.LockoutUntil = DateTime.Now.AddMinutes(30);
                        return new LoginResult
                        {
                            IsSuccess = false,
                            IsLockedOut = true,
                            Message = "Too many failed attempts. Account locked for 30 minutes.",
                            LockoutDuration = TimeSpan.FromMinutes(30)
                        };
                    }

                    return new LoginResult
                    {
                        IsSuccess = false,
                        Message = $"Invalid username or password. {5 - user.FailedLoginAttempts} attempts remaining."
                    };
                }


                // Successful login
                user.FailedLoginAttempts = 0;
                user.LockoutUntil = null;
                user.LastLogin = DateTime.Now;
                
                CurrentUser = user;
                CreateUserSession(user);
                StartSessionTimer(user);
                
                UserLoggedIn?.Invoke(this, user);

                return new LoginResult
                {
                    IsSuccess = true,
                    Message = "Login successful",
                    User = user
                };
            }
            catch (Exception ex)
            {
                return new LoginResult
                {
                    IsSuccess = false,
                    Message = $"Login error: {ex.Message}"
                };
            }
        }

        public void Logout()
        {
            try
            {
                if (CurrentUser != null)
                {
                    // Clear active session
                    var session = _activeSessions.FirstOrDefault(s => s.UserId == CurrentUser.UserId && s.IsActive);
                    if (session != null)
                    {
                        session.IsActive = false;
                    }

                    // Stop timers
                    if (_sessionTimers.ContainsKey(CurrentUser.UserId))
                    {
                        _sessionTimers[CurrentUser.UserId].Stop();
                        _sessionTimers[CurrentUser.UserId].Dispose();
                        _sessionTimers.Remove(CurrentUser.UserId);
                    }

                    // Clear privilege escalation
                    ClearPrivilegeEscalation(CurrentUser.UserId);

                    CurrentUser = null;
                    UserLoggedOut?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logout error: {ex.Message}");
            }
        }

        #endregion

        #region Privilege Escalation

        public async Task<bool> RequestPrivilegeEscalationAsync(PrivilegeEscalationRequest request)
        {
            try
            {
                if (CurrentUser == null)
                    return false;

                // Check if user can escalate to requested role
                if (CurrentUser.Role < request.RequiredRole)
                    return false;


                // Grant privilege escalation
                _escalatedPrivileges[CurrentUser.UserId] = request.RequiredRole;
                
                // Set timeout based on role
                var timeoutMinutes = request.RequiredRole == UserRole.SuperAdmin ? 1 : 5;
                StartPrivilegeTimer(CurrentUser.UserId, timeoutMinutes);
                
                PrivilegeEscalated?.Invoke(this, request.RequiredRole);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Privilege escalation error: {ex.Message}");
                return false;
            }
        }

        public void ClearPrivilegeEscalation(int userId)
        {
            try
            {
                if (_escalatedPrivileges.ContainsKey(userId))
                {
                    _escalatedPrivileges.Remove(userId);
                }

                if (_privilegeTimers.ContainsKey(userId))
                {
                    _privilegeTimers[userId].Stop();
                    _privilegeTimers[userId].Dispose();
                    _privilegeTimers.Remove(userId);
                }

                PrivilegeExpired?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clear privilege escalation error: {ex.Message}");
            }
        }

        public bool HasPermission(UserRole requiredRole)
        {
            return CurrentRole >= requiredRole;
        }

        #endregion

        #region User Management

        public async Task<bool> CreateUserAsync(User user, string password)
        {
            try
            {
                if (_users.Any(u => u.Username.Equals(user.Username, StringComparison.OrdinalIgnoreCase)))
                    return false;

                user.UserId = _users.Count > 0 ? _users.Max(u => u.UserId) + 1 : 1;
                var (hash, salt) = HashPassword(password);
                user.PasswordHash = hash;
                user.Salt = salt;
                user.CreatedDate = DateTime.Now;
                user.LastPasswordChange = DateTime.Now;

                _users.Add(user);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Create user error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                var existingUser = _users.FirstOrDefault(u => u.UserId == user.UserId);
                if (existingUser == null)
                    return false;

                existingUser.FullName = user.FullName;
                existingUser.Email = user.Email;
                existingUser.Role = user.Role;
                existingUser.IsActive = user.IsActive;
                existingUser.RecoveryEmail = user.RecoveryEmail;

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update user error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = _users.FirstOrDefault(u => u.UserId == userId);
                if (user == null)
                    return false;

                if (!VerifyPassword(currentPassword, user.PasswordHash, user.Salt))
                    return false;

                var (hash, salt) = HashPassword(newPassword);
                user.PasswordHash = hash;
                user.Salt = salt;
                user.LastPasswordChange = DateTime.Now;

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Change password error: {ex.Message}");
                return false;
            }
        }

        public List<User> GetAllUsers()
        {
            return _users.Where(u => u.IsActive).ToList();
        }

        public User? GetUserById(int userId)
        {
            return _users.FirstOrDefault(u => u.UserId == userId);
        }

        #endregion

        // Simple validation for demonstration purposes
        public async Task<bool> GetUserByUsernameAsync(string username)
        {
            try
            {
                await Task.Delay(50); // Simulate async operation
                return !string.IsNullOrEmpty(username) && username.Contains("admin", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        #region Session Management

        private void CreateUserSession(User user)
        {
            var session = new UserSession
            {
                SessionId = _activeSessions.Count + 1,
                UserId = user.UserId,
                SessionToken = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddHours(8), // 8 hour session
                IsActive = true
            };

            _activeSessions.Add(session);
        }

        private void StartSessionTimer(User user)
        {
            var timeoutMinutes = user.Role switch
            {
                UserRole.SuperAdmin => 60, // 1 hour for super admin
                UserRole.Admin => 120,     // 2 hours for admin
                UserRole.User => 480,      // 8 hours for user
                _ => 480
            };

            var timer = new System.Timers.Timer(timeoutMinutes * 60 * 1000); // Convert to milliseconds
            timer.Elapsed += (sender, e) => HandleSessionTimeout(user.UserId);
            timer.AutoReset = false;
            timer.Start();

            _sessionTimers[user.UserId] = timer;
        }

        private void StartPrivilegeTimer(int userId, int timeoutMinutes)
        {
            var timer = new System.Timers.Timer(timeoutMinutes * 60 * 1000); // Convert to milliseconds
            timer.Elapsed += (sender, e) => ClearPrivilegeEscalation(userId);
            timer.AutoReset = false;
            timer.Start();

            _privilegeTimers[userId] = timer;
        }

        private void HandleSessionTimeout(int userId)
        {
            try
            {
                var user = _users.FirstOrDefault(u => u.UserId == userId);
                if (user != null && CurrentUser?.UserId == userId)
                {
                    SessionExpired?.Invoke(this, $"Session expired for user: {user.Username}");
                    Logout();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Session timeout error: {ex.Message}");
            }
        }

        #endregion

        #region Password Hashing

        private (string hash, string salt) HashPassword(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            var saltBytes = new byte[32];
            rng.GetBytes(saltBytes);
            var salt = Convert.ToBase64String(saltBytes);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256);
            var hash = Convert.ToBase64String(pbkdf2.GetBytes(32));

            return (hash, salt);
        }

        private bool VerifyPassword(string password, string hash, string salt)
        {
            try
            {
                var saltBytes = Convert.FromBase64String(salt);
                using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256);
                var computedHash = Convert.ToBase64String(pbkdf2.GetBytes(32));
                return computedHash == hash;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        public void Dispose()
        {
            try
            {
                foreach (var timer in _sessionTimers.Values)
                {
                    timer?.Stop();
                    timer?.Dispose();
                }
                _sessionTimers.Clear();

                foreach (var timer in _privilegeTimers.Values)
                {
                    timer?.Stop();
                    timer?.Dispose();
                }
                _privilegeTimers.Clear();

                _databaseService?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Authentication service disposal error: {ex.Message}");
            }
        }
    }
}