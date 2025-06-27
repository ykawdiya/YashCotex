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

    // Material CRUD Operations
    public List<Material> GetAllMaterials()
    {
        return _context.Materials
            .OrderBy(m => m.Name)
            .ToList();
    }

    public List<Material> GetActiveMaterials()
    {
        return _context.Materials
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .ToList();
    }

    public Material? GetMaterialById(int id)
    {
        return _context.Materials.FirstOrDefault(m => m.Id == id);
    }

    public bool CreateMaterial(string name)
    {
        try
        {
            // Check if material already exists
            if (_context.Materials.Any(m => m.Name.ToLower() == name.ToLower()))
            {
                return false; // Material already exists
            }

            var material = new Material
            {
                Name = name.Trim(),
                IsActive = true,
                CreatedDate = DateTime.Now
            };

            _context.Materials.Add(material);
            _context.SaveChanges();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool UpdateMaterial(int id, string name)
    {
        try
        {
            var material = _context.Materials.FirstOrDefault(m => m.Id == id);
            if (material == null) return false;

            // Check if another material with same name exists
            if (_context.Materials.Any(m => m.Name.ToLower() == name.ToLower() && m.Id != id))
            {
                return false; // Another material with same name exists
            }

            material.Name = name.Trim();
            _context.SaveChanges();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool DeleteMaterial(int id)
    {
        try
        {
            var material = _context.Materials.FirstOrDefault(m => m.Id == id);
            if (material == null) return false;

            // Soft delete - set IsActive to false
            material.IsActive = false;
            _context.SaveChanges();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Address CRUD Operations
    public List<Address> GetAllAddresses()
    {
        return _context.Addresses
            .OrderBy(a => a.Name)
            .ToList();
    }

    public List<Address> GetActiveAddresses()
    {
        return _context.Addresses
            .Where(a => a.IsActive)
            .OrderBy(a => a.Name)
            .ToList();
    }

    public Address? GetAddressById(int id)
    {
        return _context.Addresses.FirstOrDefault(a => a.Id == id);
    }

    public bool CreateAddress(string name)
    {
        try
        {
            // Check if address already exists
            if (_context.Addresses.Any(a => a.Name.ToLower() == name.ToLower()))
            {
                return false; // Address already exists
            }

            var address = new Address
            {
                Name = name.Trim(),
                IsActive = true,
                CreatedDate = DateTime.Now
            };

            _context.Addresses.Add(address);
            _context.SaveChanges();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool UpdateAddress(int id, string name)
    {
        try
        {
            var address = _context.Addresses.FirstOrDefault(a => a.Id == id);
            if (address == null) return false;

            // Check if another address with same name exists
            if (_context.Addresses.Any(a => a.Name.ToLower() == name.ToLower() && a.Id != id))
            {
                return false; // Another address with same name exists
            }

            address.Name = name.Trim();
            _context.SaveChanges();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool DeleteAddress(int id)
    {
        try
        {
            var address = _context.Addresses.FirstOrDefault(a => a.Id == id);
            if (address == null) return false;

            // Soft delete - set IsActive to false
            address.IsActive = false;
            _context.SaveChanges();
            return true;
        }
        catch
        {
            return false;
        }
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