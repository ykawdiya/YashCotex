namespace WeighbridgeSoftwareYashCotex.Models;

public class WeighmentEntry
{
    public int RstNumber { get; set; }
    public int Id { get; set; }
    public string VehicleNumber { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    
    public double EntryWeight { get; set; }
    public DateTime EntryDateTime { get; set; }
    
    public double? ExitWeight { get; set; }
    public DateTime? ExitDateTime { get; set; }
    
    public double GrossWeight => Math.Max(EntryWeight, ExitWeight ?? 0);
    public double TareWeight => Math.Min(EntryWeight, ExitWeight ?? EntryWeight);
    public double NetWeight => Math.Abs((ExitWeight ?? 0) - EntryWeight);
    
    public bool IsCompleted => ExitWeight.HasValue;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}