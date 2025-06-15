using System;
using System.ComponentModel.DataAnnotations;

namespace WeighbridgeSoftwareYashCotex.Models
{
    public class WeightAudit
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int RstNumber { get; set; }
        
        [Required]
        public string WeightType { get; set; } = string.Empty; // "Entry" or "Exit"
        
        [Required]
        public double OriginalWeight { get; set; }
        
        [Required]
        public double NewWeight { get; set; }
        
        public double WeightDifference => NewWeight - OriginalWeight;
        
        [Required]
        public string Reason { get; set; } = string.Empty;
        
        [Required]
        public string ModifiedBy { get; set; } = string.Empty; // Username of Super Admin
        
        [Required]
        public DateTime ModifiedDateTime { get; set; }
        
        public string? ApprovedBy { get; set; } // For additional approval if needed
        
        public DateTime? ApprovedDateTime { get; set; }
        
        public bool IsApproved { get; set; } = true; // Auto-approved for Super Admin
        
        [Required]
        public string VehicleNumber { get; set; } = string.Empty;
        
        [Required]
        public string CustomerName { get; set; } = string.Empty;
        
        public string? Notes { get; set; }
        
        // System fields
        public string SystemInfo { get; set; } = string.Empty; // IP, Computer name, etc.
        
        public bool IsReversed { get; set; } = false; // If the change was reversed
        
        public DateTime? ReversedDateTime { get; set; }
        
        public string? ReversedBy { get; set; }
        
        public string? ReversalReason { get; set; }
    }
}