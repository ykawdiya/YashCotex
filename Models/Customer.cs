namespace WeighbridgeSoftwareYashCotex.Models;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string VehicleNumber { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}