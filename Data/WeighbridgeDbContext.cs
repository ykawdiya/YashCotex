using Microsoft.EntityFrameworkCore;
using WeighbridgeSoftwareYashCotex.Models;
using System.IO;

namespace WeighbridgeSoftwareYashCotex.Data;

public class WeighbridgeDbContext : DbContext
{
    public DbSet<Customer> Customers { get; set; }
    public DbSet<WeighmentEntry> WeighmentEntries { get; set; }
    public DbSet<Material> Materials { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<WeightAudit> WeightAudits { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                                 "WeighbridgeSoftware", "weighbridge.db");
        
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Address).IsRequired().HasMaxLength(200);
            entity.Property(c => c.PhoneNumber).IsRequired().HasMaxLength(10);
            entity.Property(c => c.VehicleNumber).IsRequired().HasMaxLength(10);
            entity.HasIndex(c => c.PhoneNumber).IsUnique();
        });
        
        modelBuilder.Entity<WeighmentEntry>(entity =>
        {
            entity.HasKey(w => w.RstNumber);
            entity.Property(w => w.VehicleNumber).IsRequired().HasMaxLength(10);
            entity.Property(w => w.PhoneNumber).IsRequired().HasMaxLength(10);
            entity.Property(w => w.Name).IsRequired().HasMaxLength(100);
            entity.Property(w => w.Address).IsRequired().HasMaxLength(200);
            entity.Property(w => w.Material).IsRequired().HasMaxLength(50);
            entity.Property(w => w.EntryWeight).HasPrecision(10, 2);
            entity.Property(w => w.ExitWeight).HasPrecision(10, 2);
        });
        
        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(m => m.Name).IsUnique();
        });
        
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(a => a.Name).IsUnique();
        });
        
        modelBuilder.Entity<WeightAudit>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.Property(w => w.WeightType).IsRequired().HasMaxLength(10);
            entity.Property(w => w.Reason).IsRequired().HasMaxLength(500);
            entity.Property(w => w.ModifiedBy).IsRequired().HasMaxLength(50);
            entity.Property(w => w.VehicleNumber).IsRequired().HasMaxLength(10);
            entity.Property(w => w.CustomerName).IsRequired().HasMaxLength(100);
            entity.Property(w => w.SystemInfo).HasMaxLength(500);
            entity.Property(w => w.Notes).HasMaxLength(1000);
            entity.Property(w => w.ApprovedBy).HasMaxLength(50);
            entity.Property(w => w.ReversedBy).HasMaxLength(50);
            entity.Property(w => w.ReversalReason).HasMaxLength(500);
            entity.Property(w => w.OriginalWeight).HasPrecision(10, 2);
            entity.Property(w => w.NewWeight).HasPrecision(10, 2);
            entity.HasIndex(w => w.RstNumber);
            entity.HasIndex(w => w.ModifiedDateTime);
        });
        
        SeedData(modelBuilder);
    }
    
    private void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Material>().HasData(
            new Material { Id = 1, Name = "Steel" },
            new Material { Id = 2, Name = "Iron Ore" },
            new Material { Id = 3, Name = "Coal" },
            new Material { Id = 4, Name = "Sand" },
            new Material { Id = 5, Name = "Gravel" },
            new Material { Id = 6, Name = "Cement" },
            new Material { Id = 7, Name = "Limestone" },
            new Material { Id = 8, Name = "Wheat" },
            new Material { Id = 9, Name = "Rice" },
            new Material { Id = 10, Name = "Other" }
        );
        
        modelBuilder.Entity<Address>().HasData(
            new Address { Id = 1, Name = "Mumbai" },
            new Address { Id = 2, Name = "Delhi" },
            new Address { Id = 3, Name = "Pune" },
            new Address { Id = 4, Name = "Nagpur" },
            new Address { Id = 5, Name = "Surat" },
            new Address { Id = 6, Name = "Local" }
        );
    }
}