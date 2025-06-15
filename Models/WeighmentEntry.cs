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

public class ExitData
{
    public int RstNumber { get; set; }
    public string VehicleNumber { get; set; } = string.Empty;
    public double ExitWeight { get; set; }
    public DateTime ExitDateTime { get; set; }
    public double NetWeight { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CameraConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Protocol { get; set; } = "http";
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 80;
    public string StreamPath { get; set; } = "/mjpeg/1";
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    
    public string GetFullUrl()
    {
        var path = string.IsNullOrEmpty(StreamPath) ? "" : StreamPath;
        if (!string.IsNullOrEmpty(path) && !path.StartsWith("/"))
        {
            path = "/" + path;
        }
        
        return Protocol.ToLower() switch
        {
            "http" => $"http://{IpAddress}:{Port}{path}",
            "https" => $"https://{IpAddress}:{Port}{path}",
            "rtsp" => $"rtsp://{IpAddress}:{Port}{path}",
            "tcp" => $"tcp://{IpAddress}:{Port}",
            _ => $"http://{IpAddress}:{Port}{path}"
        };
    }
}