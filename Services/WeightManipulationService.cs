using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WeighbridgeSoftwareYashCotex.Models;
using WeighbridgeSoftwareYashCotex.Data;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace WeighbridgeSoftwareYashCotex.Services
{
    public class WeightManipulationService : IDisposable
    {
        private readonly WeighbridgeDbContext _context;
        private readonly AuthenticationService _authService;

        public WeightManipulationService(AuthenticationService authService)
        {
            _context = new WeighbridgeDbContext();
            _authService = authService;
        }

        #region Weight Modification

        public async Task<WeightModificationResult> ModifyEntryWeightAsync(int rstNumber, double newWeight, string reason, string modifiedBy)
        {
            try
            {
                // Verify Super Admin permissions
                if (!await VerifySuperAdminAccess(modifiedBy))
                {
                    return new WeightModificationResult
                    {
                        Success = false,
                        Message = "Access denied. Super Admin privileges required for weight modification."
                    };
                }

                var entry = await _context.WeighmentEntries.FirstOrDefaultAsync(w => w.RstNumber == rstNumber);
                if (entry == null)
                {
                    return new WeightModificationResult
                    {
                        Success = false,
                        Message = $"Entry with RST {rstNumber} not found."
                    };
                }

                // Validate weight modification rules
                var validationResult = ValidateWeightModification(entry.EntryWeight, newWeight, "Entry");
                if (!validationResult.IsValid)
                {
                    return new WeightModificationResult
                    {
                        Success = false,
                        Message = validationResult.ErrorMessage
                    };
                }

                // Create audit record
                var audit = new WeightAudit
                {
                    RstNumber = rstNumber,
                    WeightType = "Entry",
                    OriginalWeight = entry.EntryWeight,
                    NewWeight = newWeight,
                    Reason = reason,
                    ModifiedBy = modifiedBy,
                    ModifiedDateTime = DateTime.Now,
                    VehicleNumber = entry.VehicleNumber,
                    CustomerName = entry.Name,
                    SystemInfo = GetSystemInfo(),
                    IsApproved = true
                };

                // Update the weight
                entry.EntryWeight = newWeight;
                entry.LastUpdated = DateTime.Now;
                
                // Add audit record
                _context.WeightAudits.Add(audit);
                
                await _context.SaveChangesAsync();

                return new WeightModificationResult
                {
                    Success = true,
                    Message = $"Entry weight successfully modified from {audit.OriginalWeight:F2} KG to {newWeight:F2} KG",
                    AuditId = audit.Id,
                    WeightDifference = audit.WeightDifference
                };
            }
            catch (Exception ex)
            {
                return new WeightModificationResult
                {
                    Success = false,
                    Message = $"Error modifying entry weight: {ex.Message}"
                };
            }
        }

        public async Task<WeightModificationResult> ModifyExitWeightAsync(int rstNumber, double newWeight, string reason, string modifiedBy)
        {
            try
            {
                // Verify Super Admin permissions
                if (!await VerifySuperAdminAccess(modifiedBy))
                {
                    return new WeightModificationResult
                    {
                        Success = false,
                        Message = "Access denied. Super Admin privileges required for weight modification."
                    };
                }

                var entry = await _context.WeighmentEntries.FirstOrDefaultAsync(w => w.RstNumber == rstNumber);
                if (entry == null)
                {
                    return new WeightModificationResult
                    {
                        Success = false,
                        Message = $"Entry with RST {rstNumber} not found."
                    };
                }

                if (!entry.ExitWeight.HasValue)
                {
                    return new WeightModificationResult
                    {
                        Success = false,
                        Message = "Cannot modify exit weight: Exit weight not recorded yet."
                    };
                }

                // Validate weight modification rules
                var validationResult = ValidateWeightModification(entry.ExitWeight.Value, newWeight, "Exit");
                if (!validationResult.IsValid)
                {
                    return new WeightModificationResult
                    {
                        Success = false,
                        Message = validationResult.ErrorMessage
                    };
                }

                // Create audit record
                var audit = new WeightAudit
                {
                    RstNumber = rstNumber,
                    WeightType = "Exit",
                    OriginalWeight = entry.ExitWeight.Value,
                    NewWeight = newWeight,
                    Reason = reason,
                    ModifiedBy = modifiedBy,
                    ModifiedDateTime = DateTime.Now,
                    VehicleNumber = entry.VehicleNumber,
                    CustomerName = entry.Name,
                    SystemInfo = GetSystemInfo(),
                    IsApproved = true
                };

                // Update the weight
                entry.ExitWeight = newWeight;
                entry.LastUpdated = DateTime.Now;
                
                // Add audit record
                _context.WeightAudits.Add(audit);
                
                await _context.SaveChangesAsync();

                return new WeightModificationResult
                {
                    Success = true,
                    Message = $"Exit weight successfully modified from {audit.OriginalWeight:F2} KG to {newWeight:F2} KG",
                    AuditId = audit.Id,
                    WeightDifference = audit.WeightDifference
                };
            }
            catch (Exception ex)
            {
                return new WeightModificationResult
                {
                    Success = false,
                    Message = $"Error modifying exit weight: {ex.Message}"
                };
            }
        }

        #endregion

        #region Weight Validation Rules

        private WeightValidationResult ValidateWeightModification(double originalWeight, double newWeight, string weightType)
        {
            // Rule 1: Weight cannot be negative
            if (newWeight < 0)
            {
                return new WeightValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Weight cannot be negative."
                };
            }

            // Rule 2: Weight cannot exceed maximum scale capacity (typically 100 tons = 100,000 KG)
            if (newWeight > 100000)
            {
                return new WeightValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Weight exceeds maximum scale capacity (100,000 KG)."
                };
            }

            // Rule 3: Weight difference cannot exceed 50% of original weight (prevents extreme modifications)
            var maxAllowedDifference = originalWeight * 0.5;
            var actualDifference = Math.Abs(newWeight - originalWeight);
            
            if (actualDifference > maxAllowedDifference && originalWeight > 0)
            {
                return new WeightValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Weight modification exceeds 50% limit. Maximum allowed change: ±{maxAllowedDifference:F2} KG"
                };
            }

            // Rule 4: Minimum weight for loaded vehicles (prevents unrealistic weights)
            if (newWeight < 500 && newWeight > 0) // Less than 500 KG seems unrealistic for a vehicle
            {
                return new WeightValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Weight too low. Minimum vehicle weight should be at least 500 KG."
                };
            }

            return new WeightValidationResult { IsValid = true };
        }

        #endregion

        #region Audit and History

        public async Task<List<WeightAudit>> GetWeightAuditHistoryAsync(int? rstNumber = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.WeightAudits.AsQueryable();

            if (rstNumber.HasValue)
            {
                query = query.Where(a => a.RstNumber == rstNumber.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.ModifiedDateTime >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(a => a.ModifiedDateTime <= toDate.Value);
            }

            return await query.OrderByDescending(a => a.ModifiedDateTime).ToListAsync();
        }

        public async Task<List<WeightAudit>> GetRecentModificationsAsync(int limit = 50)
        {
            return await _context.WeightAudits
                .OrderByDescending(a => a.ModifiedDateTime)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<WeightAuditSummary> GetAuditSummaryAsync(DateTime fromDate, DateTime toDate)
        {
            var audits = await _context.WeightAudits
                .Where(a => a.ModifiedDateTime >= fromDate && a.ModifiedDateTime <= toDate)
                .ToListAsync();

            return new WeightAuditSummary
            {
                TotalModifications = audits.Count,
                EntryWeightModifications = audits.Count(a => a.WeightType == "Entry"),
                ExitWeightModifications = audits.Count(a => a.WeightType == "Exit"),
                TotalWeightIncreased = audits.Where(a => a.WeightDifference > 0).Sum(a => a.WeightDifference),
                TotalWeightDecreased = Math.Abs(audits.Where(a => a.WeightDifference < 0).Sum(a => a.WeightDifference)),
                UniqueRstModified = audits.Select(a => a.RstNumber).Distinct().Count(),
                ModificationsByUser = audits.GroupBy(a => a.ModifiedBy)
                    .ToDictionary(g => g.Key, g => g.Count()),
                DateRange = $"{fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}"
            };
        }

        #endregion

        #region Reversal Operations

        public async Task<WeightModificationResult> ReverseWeightModificationAsync(int auditId, string reason, string reversedBy)
        {
            try
            {
                // Verify Super Admin permissions
                if (!await VerifySuperAdminAccess(reversedBy))
                {
                    return new WeightModificationResult
                    {
                        Success = false,
                        Message = "Access denied. Super Admin privileges required for weight reversal."
                    };
                }

                var audit = await _context.WeightAudits.FirstOrDefaultAsync(a => a.Id == auditId);
                if (audit == null)
                {
                    return new WeightModificationResult
                    {
                        Success = false,
                        Message = "Audit record not found."
                    };
                }

                if (audit.IsReversed)
                {
                    return new WeightModificationResult
                    {
                        Success = false,
                        Message = "This modification has already been reversed."
                    };
                }

                var entry = await _context.WeighmentEntries.FirstOrDefaultAsync(w => w.RstNumber == audit.RstNumber);
                if (entry == null)
                {
                    return new WeightModificationResult
                    {
                        Success = false,
                        Message = "Original weighment entry not found."
                    };
                }

                // Reverse the weight change
                if (audit.WeightType == "Entry")
                {
                    entry.EntryWeight = audit.OriginalWeight;
                }
                else if (audit.WeightType == "Exit")
                {
                    entry.ExitWeight = audit.OriginalWeight;
                }

                // Mark audit as reversed
                audit.IsReversed = true;
                audit.ReversedDateTime = DateTime.Now;
                audit.ReversedBy = reversedBy;
                audit.ReversalReason = reason;

                entry.LastUpdated = DateTime.Now;

                await _context.SaveChangesAsync();

                return new WeightModificationResult
                {
                    Success = true,
                    Message = $"Weight modification reversed successfully. {audit.WeightType} weight restored to {audit.OriginalWeight:F2} KG",
                    AuditId = audit.Id
                };
            }
            catch (Exception ex)
            {
                return new WeightModificationResult
                {
                    Success = false,
                    Message = $"Error reversing weight modification: {ex.Message}"
                };
            }
        }

        #endregion

        #region Helper Methods

        private async Task<bool> VerifySuperAdminAccess(string username)
        {
            try
            {
                // For now, we'll implement a simple check
                // In a real implementation, this would check against the user database
                return username == "SuperAdmin" || username.Contains("admin", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private string GetSystemInfo()
        {
            try
            {
                var computerName = Environment.MachineName;
                var userName = Environment.UserName;
                var osVersion = Environment.OSVersion.ToString();
                
                return $"Computer: {computerName}, User: {userName}, OS: {osVersion}";
            }
            catch
            {
                return "System info unavailable";
            }
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }

    #region Result Classes

    public class WeightModificationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? AuditId { get; set; }
        public double WeightDifference { get; set; }
    }

    public class WeightValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class WeightAuditSummary
    {
        public int TotalModifications { get; set; }
        public int EntryWeightModifications { get; set; }
        public int ExitWeightModifications { get; set; }
        public double TotalWeightIncreased { get; set; }
        public double TotalWeightDecreased { get; set; }
        public int UniqueRstModified { get; set; }
        public Dictionary<string, int> ModificationsByUser { get; set; } = new();
        public string DateRange { get; set; } = string.Empty;
    }

    #endregion
}