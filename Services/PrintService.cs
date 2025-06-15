using WeighbridgeSoftwareYashCotex.Models;
using System.Text;
using System.IO;

namespace WeighbridgeSoftwareYashCotex.Services;

public class PrintService
{
    public void PrintReceipt(WeighmentEntry entry)
    {
        try
        {
            var receiptText = GenerateReceiptText(entry);
            
            PrintToFile(receiptText, entry.RstNumber);
            
        }
        catch (Exception ex)
        {
            throw new Exception($"Print failed: {ex.Message}");
        }
    }
    
    private string GenerateReceiptText(WeighmentEntry entry)
    {
        var sb = new StringBuilder();
        var settings = SettingsService.Instance;
        
        sb.AppendLine("".PadLeft(40, '='));
        sb.AppendLine(settings.CompanyName.PadLeft((40 + settings.CompanyName.Length) / 2));
        sb.AppendLine(settings.CompanyAddress.PadLeft((40 + settings.CompanyAddress.Length) / 2));
        sb.AppendLine($"{settings.CompanyPhone} | {settings.CompanyEmail}".PadLeft((40 + settings.CompanyPhone.Length + settings.CompanyEmail.Length + 3) / 2));
        sb.AppendLine(settings.CompanyGSTIN.PadLeft((40 + settings.CompanyGSTIN.Length) / 2));
        sb.AppendLine("".PadLeft(40, '='));
        sb.AppendLine();
        
        sb.AppendLine("WEIGHMENT RECEIPT".PadLeft(28));
        sb.AppendLine();
        
        sb.AppendLine($"RST Number    : {entry.RstNumber}");
        sb.AppendLine($"Vehicle No    : {entry.VehicleNumber}");
        sb.AppendLine($"Customer Name : {entry.Name}");
        sb.AppendLine($"Phone Number  : {entry.PhoneNumber}");
        sb.AppendLine($"Address       : {entry.Address}");
        sb.AppendLine($"Material      : {entry.Material}");
        sb.AppendLine();
        
        sb.AppendLine("WEIGHT DETAILS:");
        sb.AppendLine($"Entry Weight  : {entry.EntryWeight:F2} KG");
        sb.AppendLine($"Entry Time    : {entry.EntryDateTime:dd/MM/yyyy HH:mm}");
        
        if (entry.ExitWeight.HasValue)
        {
            sb.AppendLine($"Exit Weight   : {entry.ExitWeight:F2} KG");
            sb.AppendLine($"Exit Time     : {entry.ExitDateTime:dd/MM/yyyy HH:mm}");
            sb.AppendLine("".PadLeft(40, '-'));
            sb.AppendLine($"Gross Weight  : {entry.GrossWeight:F2} KG");
            sb.AppendLine($"Tare Weight   : {entry.TareWeight:F2} KG");
            sb.AppendLine($"NET WEIGHT    : {entry.NetWeight:F2} KG");
        }
        
        sb.AppendLine();
        sb.AppendLine("".PadLeft(40, '-'));
        sb.AppendLine($"Printed: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine("".PadLeft(40, '='));
        
        return sb.ToString();
    }
    
    private void PrintToFile(string content, int rstNumber)
    {
        var printPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                                    "WeighbridgePrints");
        Directory.CreateDirectory(printPath);
        
        var fileName = $"RST_{rstNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        var filePath = Path.Combine(printPath, fileName);
        
        File.WriteAllText(filePath, content, Encoding.UTF8);
    }
    
    public void PrintPreview(WeighmentEntry entry)
    {
        var receiptText = GenerateReceiptText(entry);
    }
}