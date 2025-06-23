using Microsoft.EntityFrameworkCore;
using WeighbridgeSoftwareYashCotex.Data;
using WeighbridgeSoftwareYashCotex.Models;

namespace WeighbridgeSoftwareYashCotex.Services;

public class DatabaseService : IDisposable
{
    private readonly WeighbridgeDbContext _context;
    
    public DatabaseService()
    {
        try
        {
            Console.WriteLine("Creating WeighbridgeDbContext...");
            _context = new WeighbridgeDbContext();
            Console.WriteLine("WeighbridgeDbContext created, ensuring database...");
            
            // Test if we can access the database
            Console.WriteLine("Testing database connection...");
            var canConnect = _context.Database.CanConnect();
            Console.WriteLine($"Database can connect: {canConnect}");
            
            _context.Database.EnsureCreated();
            Console.WriteLine("Database ensured successfully");
            
            // Test a simple query to make sure everything works
            Console.WriteLine("Testing database query...");
            var entryCount = _context.WeighmentEntries.Count();
            Console.WriteLine($"Database test successful - found {entryCount} entries");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DatabaseService constructor failed: {ex.Message}");
            Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }
    
    public int GetNextRstNumber()
    {
        var lastEntry = _context.WeighmentEntries
            .OrderByDescending(w => w.RstNumber)
            .FirstOrDefault();
        
        return (lastEntry?.RstNumber ?? 0) + 1;
    }
    
    public int GetNextId()
    {
        var lastCustomer = _context.Customers
            .OrderByDescending(c => c.Id)
            .FirstOrDefault();
        
        return (lastCustomer?.Id ?? 0) + 1;
    }
    
    public List<string> GetAddresses()
    {
        return _context.Customers
            .Select(c => c.Address)
            .Distinct()
            .Where(a => !string.IsNullOrEmpty(a))
            .OrderBy(a => a)
            .ToList();
    }
    
    public List<string> GetMaterials()
    {
        return _context.Materials
            .Select(m => m.Name)
            .OrderBy(m => m)
            .ToList();
    }
    
    public Customer? GetCustomerByVehicleNumber(string vehicleNumber)
    {
        return _context.Customers
            .FirstOrDefault(c => c.VehicleNumber == vehicleNumber);
    }
    
    public Customer? GetCustomerByPhoneNumber(string phoneNumber)
    {
        return _context.Customers
            .FirstOrDefault(c => c.PhoneNumber == phoneNumber);
    }
    
    public WeighmentEntry? GetWeighmentByRst(int rstNumber)
    {
        return _context.WeighmentEntries
            .FirstOrDefault(w => w.RstNumber == rstNumber);
    }
    
    public WeighmentEntry? GetLatestIncompleteWeighmentByVehicle(string vehicleNumber)
    {
        return _context.WeighmentEntries
            .Where(w => w.VehicleNumber == vehicleNumber && !w.ExitWeight.HasValue)
            .OrderByDescending(w => w.EntryDateTime)
            .FirstOrDefault();
    }
    
    public WeighmentEntry? GetLatestIncompleteWeighmentByPhone(string phoneNumber)
    {
        return _context.WeighmentEntries
            .Where(w => w.PhoneNumber == phoneNumber && !w.ExitWeight.HasValue)
            .OrderByDescending(w => w.EntryDateTime)
            .FirstOrDefault();
    }
    
    public void SaveEntry(WeighmentEntry entry)
    {
        _context.WeighmentEntries.Add(entry);
        
        var existingCustomer = GetCustomerByPhoneNumber(entry.PhoneNumber);
        if (existingCustomer != null)
        {
            existingCustomer.Name = entry.Name;
            existingCustomer.Address = entry.Address;
            existingCustomer.VehicleNumber = entry.VehicleNumber;
            existingCustomer.LastUpdated = DateTime.Now;
            _context.Customers.Update(existingCustomer);
        }
        else
        {
            var newCustomer = new Customer
            {
                Id = entry.Id,
                Name = entry.Name,
                Address = entry.Address,
                PhoneNumber = entry.PhoneNumber,
                VehicleNumber = entry.VehicleNumber
            };
            _context.Customers.Add(newCustomer);
        }
        
        _context.SaveChanges();
    }
    
    public void UpdateExitWeight(int rstNumber, double exitWeight)
    {
        var entry = _context.WeighmentEntries
            .FirstOrDefault(w => w.RstNumber == rstNumber);
        
        if (entry != null)
        {
            entry.ExitWeight = exitWeight;
            entry.ExitDateTime = DateTime.Now;
            entry.LastUpdated = DateTime.Now;
            _context.SaveChanges();
        }
    }
    
    public void DeleteEntry(int rstNumber)
    {
        var entry = _context.WeighmentEntries
            .FirstOrDefault(w => w.RstNumber == rstNumber);
        
        if (entry != null)
        {
            _context.WeighmentEntries.Remove(entry);
            _context.SaveChanges();
        }
    }
    
    public void DeleteExitWeight(int rstNumber)
    {
        var entry = _context.WeighmentEntries
            .FirstOrDefault(w => w.RstNumber == rstNumber);
        
        if (entry != null)
        {
            entry.ExitWeight = null;
            entry.ExitDateTime = null;
            entry.LastUpdated = DateTime.Now;
            _context.SaveChanges();
        }
    }
    
    public WeighmentEntry? GetEntryByVehicleNumber(string vehicleNumber)
    {
        return _context.WeighmentEntries
            .Where(w => w.VehicleNumber == vehicleNumber && !w.ExitDateTime.HasValue)
            .OrderByDescending(w => w.EntryDateTime)
            .FirstOrDefault();
    }
    
    public void UpdateEntryExit(WeighmentEntry entry)
    {
        _context.WeighmentEntries.Update(entry);
        _context.SaveChanges();
    }
    
    public List<WeighmentEntry> GetAllWeighments()
    {
        return _context.WeighmentEntries
            .OrderByDescending(w => w.EntryDateTime)
            .ToList();
    }

    public List<string> GetRecentVehicleNumbers(string partialNumber, int limit = 5)
    {
        return _context.WeighmentEntries
            .Where(w => w.VehicleNumber.StartsWith(partialNumber))
            .Select(w => w.VehicleNumber)
            .Distinct()
            .OrderByDescending(v => _context.WeighmentEntries
                .Where(e => e.VehicleNumber == v)
                .Max(e => e.EntryDateTime))
            .Take(limit)
            .ToList();
    }

    public WeighmentEntry? GetLastEntryForVehicle(string vehicleNumber)
    {
        return _context.WeighmentEntries
            .Where(w => w.VehicleNumber == vehicleNumber)
            .OrderByDescending(w => w.EntryDateTime)
            .FirstOrDefault();
    }

    public List<WeighmentEntry> GetVehicleHistory(string vehicleNumber, int limit = 5)
    {
        return _context.WeighmentEntries
            .Where(w => w.VehicleNumber == vehicleNumber)
            .OrderByDescending(w => w.EntryDateTime)
            .Take(limit)
            .ToList();
    }
    
    public List<WeighmentEntry> GetWeighmentsByDateRange(DateTime startDate, DateTime endDate)
    {
        return _context.WeighmentEntries
            .Where(w => w.EntryDateTime >= startDate && w.EntryDateTime <= endDate)
            .OrderBy(w => w.EntryDateTime)
            .ToList();
    }
    
    public void Dispose()
    {
        _context?.Dispose();
    }
}