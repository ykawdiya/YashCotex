using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WeighbridgeSoftwareYashCotex.Models;
using Microsoft.EntityFrameworkCore;
using WeighbridgeSoftwareYashCotex.Data;

namespace WeighbridgeSoftwareYashCotex.Services
{
    public enum TwoFactorMethod
    {
        TOTP,           // Time-based One-Time Password (Google Authenticator)
        Email,          // Email verification code
        SMS,            // SMS verification code (simulated)
        BackupCodes     // Static backup codes
    }

    public class TwoFactorAuthService : IDisposable
    {
        private readonly WeighbridgeDbContext _context;
        private readonly Dictionary<string, string> _pendingCodes;
        private readonly Dictionary<string, DateTime> _codeExpiry;
        private readonly Random _random;

        public TwoFactorAuthService()
        {
            _context = new WeighbridgeDbContext();
            _pendingCodes = new Dictionary<string, string>();
            _codeExpiry = new Dictionary<string, DateTime>();
            _random = new Random();
        }

        #region TOTP (Time-based One-Time Password) Implementation

        public string GenerateSecretKey()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var secret = new StringBuilder();
            
            for (int i = 0; i < 32; i++)
            {
                secret.Append(chars[_random.Next(chars.Length)]);
            }
            
            return secret.ToString();
        }

        public string GenerateQrCodeUrl(string username, string secretKey, string issuer = "Weighbridge System")
        {
            var encodedIssuer = Uri.EscapeDataString(issuer);
            var encodedUsername = Uri.EscapeDataString(username);
            
            return $"otpauth://totp/{encodedIssuer}:{encodedUsername}?secret={secretKey}&issuer={encodedIssuer}";
        }

        public bool ValidateTOTPCode(string secretKey, string code)
        {
            try
            {
                if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(code) || code.Length != 6)
                    return false;

                var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var timeStep = unixTime / 30; // 30-second time window

                // Check current time step and previous/next for clock drift tolerance
                for (int i = -1; i <= 1; i++)
                {
                    var testTimeStep = timeStep + i;
                    var expectedCode = GenerateTOTPCode(secretKey, testTimeStep);
                    
                    if (expectedCode == code)
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateTOTPCode(string secretKey, long timeStep)
        {
            var secretBytes = Base32Decode(secretKey);
            var timeBytes = BitConverter.GetBytes(timeStep);
            
            if (BitConverter.IsLittleEndian)
                Array.Reverse(timeBytes);

            using var hmac = new HMACSHA1(secretBytes);
            var hash = hmac.ComputeHash(timeBytes);
            
            var offset = hash[hash.Length - 1] & 0x0F;
            var truncated = ((hash[offset] & 0x7F) << 24) |
                           ((hash[offset + 1] & 0xFF) << 16) |
                           ((hash[offset + 2] & 0xFF) << 8) |
                           (hash[offset + 3] & 0xFF);
            
            var code = truncated % 1000000;
            return code.ToString("D6");
        }

        private byte[] Base32Decode(string input)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var result = new List<byte>();
            
            input = input.ToUpper().Replace(" ", "").Replace("-", "");
            
            for (int i = 0; i < input.Length; i += 8)
            {
                var block = input.Substring(i, Math.Min(8, input.Length - i)).PadRight(8, '=');
                var values = new int[8];
                
                for (int j = 0; j < 8; j++)
                {
                    if (block[j] == '=') break;
                    values[j] = chars.IndexOf(block[j]);
                }
                
                var bytes = new byte[]
                {
                    (byte)((values[0] << 3) | (values[1] >> 2)),
                    (byte)((values[1] << 6) | (values[2] << 1) | (values[3] >> 4)),
                    (byte)((values[3] << 4) | (values[4] >> 1)),
                    (byte)((values[4] << 7) | (values[5] << 2) | (values[6] >> 3)),
                    (byte)((values[6] << 5) | values[7])
                };
                
                for (int j = 0; j < bytes.Length && i * 5 / 8 + j < input.Length * 5 / 8; j++)
                {
                    result.Add(bytes[j]);
                }
            }
            
            return result.ToArray();
        }

        #endregion

        #region Email/SMS Verification

        public async Task<string> GenerateVerificationCodeAsync(string identifier, TwoFactorMethod method)
        {
            var code = _random.Next(100000, 999999).ToString();
            var key = $"{identifier}_{method}";
            
            _pendingCodes[key] = code;
            _codeExpiry[key] = DateTime.Now.AddMinutes(5); // 5-minute expiry
            
            // Simulate sending code
            switch (method)
            {
                case TwoFactorMethod.Email:
                    await SendEmailCodeAsync(identifier, code);
                    break;
                case TwoFactorMethod.SMS:
                    await SendSMSCodeAsync(identifier, code);
                    break;
            }
            
            return code;
        }

        public bool ValidateVerificationCode(string identifier, string code, TwoFactorMethod method)
        {
            var key = $"{identifier}_{method}";
            
            if (!_pendingCodes.ContainsKey(key) || !_codeExpiry.ContainsKey(key))
                return false;
                
            if (DateTime.Now > _codeExpiry[key])
            {
                _pendingCodes.Remove(key);
                _codeExpiry.Remove(key);
                return false;
            }
            
            var isValid = _pendingCodes[key] == code;
            
            if (isValid)
            {
                _pendingCodes.Remove(key);
                _codeExpiry.Remove(key);
            }
            
            return isValid;
        }

        private async Task SendEmailCodeAsync(string email, string code)
        {
            // Simulate email sending delay
            await Task.Delay(100);
            
            // In a real implementation, you would integrate with an email service
            System.Diagnostics.Debug.WriteLine($"[2FA EMAIL] Code {code} sent to {email}");
        }

        private async Task SendSMSCodeAsync(string phoneNumber, string code)
        {
            // Simulate SMS sending delay
            await Task.Delay(100);
            
            // In a real implementation, you would integrate with an SMS service
            System.Diagnostics.Debug.WriteLine($"[2FA SMS] Code {code} sent to {phoneNumber}");
        }

        #endregion

        #region Backup Codes

        public List<string> GenerateBackupCodes(int count = 10)
        {
            var codes = new List<string>();
            
            for (int i = 0; i < count; i++)
            {
                var code = GenerateBackupCode();
                codes.Add(code);
            }
            
            return codes;
        }

        private string GenerateBackupCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var code = new StringBuilder();
            
            for (int i = 0; i < 8; i++)
            {
                code.Append(chars[_random.Next(chars.Length)]);
                if (i == 3) code.Append("-"); // Format: XXXX-XXXX
            }
            
            return code.ToString();
        }

        public bool ValidateBackupCode(string username, string code)
        {
            try
            {
                // In a real implementation, you would check against stored backup codes
                // For now, we'll simulate validation
                return !string.IsNullOrEmpty(code) && code.Length == 9 && code.Contains("-");
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region User 2FA Management

        public async Task<bool> EnableTwoFactorAsync(string username, TwoFactorMethod method, string secret = "")
        {
            try
            {
                // In a real implementation, you would store this in the database
                // For now, we'll simulate the operation
                await Task.Delay(100);
                
                System.Diagnostics.Debug.WriteLine($"[2FA] Enabled {method} for user {username}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DisableTwoFactorAsync(string username)
        {
            try
            {
                // In a real implementation, you would update the database
                await Task.Delay(100);
                
                System.Diagnostics.Debug.WriteLine($"[2FA] Disabled for user {username}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<TwoFactorStatus> GetTwoFactorStatusAsync(string username)
        {
            try
            {
                // In a real implementation, you would query the database
                await Task.Delay(50);
                
                // For Super Admin, we'll simulate that 2FA is available
                if (username.Contains("admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new TwoFactorStatus
                    {
                        IsEnabled = true,
                        Method = TwoFactorMethod.TOTP,
                        BackupCodesRemaining = 8,
                        LastUsed = DateTime.Now.AddDays(-2)
                    };
                }
                
                return new TwoFactorStatus
                {
                    IsEnabled = false,
                    Method = TwoFactorMethod.TOTP,
                    BackupCodesRemaining = 0,
                    LastUsed = null
                };
            }
            catch
            {
                return new TwoFactorStatus { IsEnabled = false };
            }
        }

        #endregion

        #region Two-Factor Challenge

        public async Task<TwoFactorChallenge> InitiateTwoFactorChallengeAsync(string username)
        {
            try
            {
                var status = await GetTwoFactorStatusAsync(username);
                
                if (!status.IsEnabled)
                {
                    return new TwoFactorChallenge
                    {
                        Success = false,
                        Message = "Two-factor authentication is not enabled for this user."
                    };
                }

                var challengeId = Guid.NewGuid().ToString();
                
                // Store challenge for validation
                _pendingCodes[challengeId] = username;
                _codeExpiry[challengeId] = DateTime.Now.AddMinutes(10);

                switch (status.Method)
                {
                    case TwoFactorMethod.Email:
                        await GenerateVerificationCodeAsync(username, TwoFactorMethod.Email);
                        break;
                    case TwoFactorMethod.SMS:
                        await GenerateVerificationCodeAsync(username, TwoFactorMethod.SMS);
                        break;
                }

                return new TwoFactorChallenge
                {
                    Success = true,
                    ChallengeId = challengeId,
                    Method = status.Method,
                    Message = $"Two-factor authentication code required. Method: {status.Method}"
                };
            }
            catch (Exception ex)
            {
                return new TwoFactorChallenge
                {
                    Success = false,
                    Message = $"Failed to initiate 2FA challenge: {ex.Message}"
                };
            }
        }

        public async Task<TwoFactorVerificationResult> VerifyTwoFactorChallengeAsync(string challengeId, string code, string secretKey = "")
        {
            try
            {
                if (!_pendingCodes.ContainsKey(challengeId) || !_codeExpiry.ContainsKey(challengeId))
                {
                    return new TwoFactorVerificationResult
                    {
                        Success = false,
                        Message = "Invalid or expired challenge."
                    };
                }

                if (DateTime.Now > _codeExpiry[challengeId])
                {
                    _pendingCodes.Remove(challengeId);
                    _codeExpiry.Remove(challengeId);
                    return new TwoFactorVerificationResult
                    {
                        Success = false,
                        Message = "Challenge expired. Please try again."
                    };
                }

                var username = _pendingCodes[challengeId];
                var status = await GetTwoFactorStatusAsync(username);
                
                bool isValid = false;

                switch (status.Method)
                {
                    case TwoFactorMethod.TOTP:
                        isValid = ValidateTOTPCode(secretKey, code);
                        break;
                    case TwoFactorMethod.Email:
                        isValid = ValidateVerificationCode(username, code, TwoFactorMethod.Email);
                        break;
                    case TwoFactorMethod.SMS:
                        isValid = ValidateVerificationCode(username, code, TwoFactorMethod.SMS);
                        break;
                    case TwoFactorMethod.BackupCodes:
                        isValid = ValidateBackupCode(username, code);
                        break;
                }

                if (isValid)
                {
                    _pendingCodes.Remove(challengeId);
                    _codeExpiry.Remove(challengeId);
                    
                    return new TwoFactorVerificationResult
                    {
                        Success = true,
                        Username = username,
                        Message = "Two-factor authentication successful."
                    };
                }
                else
                {
                    return new TwoFactorVerificationResult
                    {
                        Success = false,
                        Message = "Invalid verification code."
                    };
                }
            }
            catch (Exception ex)
            {
                return new TwoFactorVerificationResult
                {
                    Success = false,
                    Message = $"Verification failed: {ex.Message}"
                };
            }
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }

    #region Supporting Classes

    public class TwoFactorStatus
    {
        public bool IsEnabled { get; set; }
        public TwoFactorMethod Method { get; set; }
        public int BackupCodesRemaining { get; set; }
        public DateTime? LastUsed { get; set; }
    }

    public class TwoFactorChallenge
    {
        public bool Success { get; set; }
        public string? ChallengeId { get; set; }
        public TwoFactorMethod Method { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class TwoFactorVerificationResult
    {
        public bool Success { get; set; }
        public string? Username { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    #endregion
}