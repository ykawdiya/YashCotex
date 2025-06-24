using System;
using System.Collections.Generic;
using WeighbridgeSoftwareYashCotex.Services;

namespace WeighbridgeSoftwareYashCotex.Models
{
    public class RstTemplateRow
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; } = "";
        public string Alignment { get; set; } = "Left"; // Left, Center, Right
        public int ColumnCount { get; set; } = 1;
        public List<RstTemplateColumn> Columns { get; set; } = new();
        public bool IsMultiColumn => ColumnCount > 1;
    }

    public class RstTemplateColumn
    {
        public string Content { get; set; } = "";
        public string Alignment { get; set; } = "Left";
        public int Width { get; set; } = 20; // Character width
    }

    public class RstTemplate
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Default RST Template";
        public List<RstTemplateRow> Rows { get; set; } = new();
        public int TotalWidth { get; set; } = 80; // Characters per line
        
        public string GeneratePreview(WeighmentEntry? sampleData = null)
        {
            var preview = "";
            
            foreach (var row in Rows)
            {
                if (row.IsMultiColumn)
                {
                    preview += GenerateMultiColumnLine(row, sampleData);
                }
                else
                {
                    preview += GenerateSingleLine(row.Content, row.Alignment, sampleData);
                }
                preview += "\n";
            }
            
            return preview;
        }
        
        private string GenerateSingleLine(string content, string alignment, WeighmentEntry? sampleData)
        {
            var processedContent = ProcessPlaceholders(content, sampleData);
            
            return alignment switch
            {
                "Center" => processedContent.PadLeft((TotalWidth + processedContent.Length) / 2).PadRight(TotalWidth),
                "Right" => processedContent.PadLeft(TotalWidth),
                _ => processedContent.PadRight(TotalWidth)
            };
        }
        
        private string GenerateMultiColumnLine(RstTemplateRow row, WeighmentEntry? sampleData)
        {
            var line = "";
            var availableWidth = TotalWidth;
            var columnWidth = availableWidth / row.ColumnCount;
            
            for (int i = 0; i < row.Columns.Count && i < row.ColumnCount; i++)
            {
                var column = row.Columns[i];
                var processedContent = ProcessPlaceholders(column.Content, sampleData);
                var width = Math.Min(column.Width, columnWidth);
                
                var columnText = column.Alignment switch
                {
                    "Center" => processedContent.PadLeft((width + processedContent.Length) / 2).PadRight(width),
                    "Right" => processedContent.PadLeft(width),
                    _ => processedContent.PadRight(width)
                };
                
                line += columnText;
            }
            
            return line.PadRight(TotalWidth);
        }
        
        private string ProcessPlaceholders(string content, WeighmentEntry? data)
        {
            if (string.IsNullOrEmpty(content)) return "";
            
            var result = content;
            
            // Company placeholders
            var settings = SettingsService.Instance;
            result = result.Replace("{COMPANY_NAME}", settings.CompanyName ?? "");
            result = result.Replace("{COMPANY_ADDRESS}", $"{settings.CompanyAddressLine1} {settings.CompanyAddressLine2}".Trim());
            result = result.Replace("{COMPANY_PHONE}", settings.CompanyPhone ?? "");
            result = result.Replace("{COMPANY_EMAIL}", settings.CompanyEmail ?? "");
            result = result.Replace("{COMPANY_GST}", settings.CompanyGSTIN ?? "");
            
            // Date/time placeholders
            var now = DateTime.Now;
            result = result.Replace("{CURRENT_DATE}", now.ToString("dd/MM/yyyy"));
            result = result.Replace("{CURRENT_TIME}", now.ToString("HH:mm:ss"));
            
            // Formatting placeholders
            result = result.Replace("{LINE_SEPARATOR}", new string('-', 80));
            
            // Sample data placeholders
            if (data != null)
            {
                result = result.Replace("{RST_NUMBER}", data.RstNumber.ToString());
                result = result.Replace("{VEHICLE_NUMBER}", data.VehicleNumber ?? "");
                result = result.Replace("{CUSTOMER_NAME}", data.Name ?? "");
                result = result.Replace("{CUSTOMER_PHONE}", data.PhoneNumber ?? "");
                result = result.Replace("{CUSTOMER_ADDRESS}", data.Address ?? "");
                result = result.Replace("{MATERIAL}", data.Material ?? "");
                result = result.Replace("{ENTRY_WEIGHT}", data.EntryWeight.ToString("F2"));
                result = result.Replace("{EXIT_WEIGHT}", data.ExitWeight?.ToString("F2") ?? "Pending");
                result = result.Replace("{NET_WEIGHT}", data.IsCompleted ? data.NetWeight.ToString("F2") : "Pending");
                result = result.Replace("{ENTRY_DATE}", data.EntryDateTime.ToString("dd/MM/yyyy"));
                result = result.Replace("{ENTRY_TIME}", data.EntryDateTime.ToString("HH:mm:ss"));
                result = result.Replace("{EXIT_DATE}", data.ExitDateTime?.ToString("dd/MM/yyyy") ?? "Pending");
                result = result.Replace("{EXIT_TIME}", data.ExitDateTime?.ToString("HH:mm:ss") ?? "Pending");
            }
            else
            {
                // Use sample data for preview
                result = result.Replace("{RST_NUMBER}", "12345");
                result = result.Replace("{VEHICLE_NUMBER}", "MH12AB1234");
                result = result.Replace("{CUSTOMER_NAME}", "Sample Customer");
                result = result.Replace("{CUSTOMER_PHONE}", "+91-9876543210");
                result = result.Replace("{CUSTOMER_ADDRESS}", "Sample Address, City");
                result = result.Replace("{MATERIAL}", "Cotton");
                result = result.Replace("{ENTRY_WEIGHT}", "15000.50");
                result = result.Replace("{EXIT_WEIGHT}", "14500.25");
                result = result.Replace("{NET_WEIGHT}", "500.25");
                result = result.Replace("{ENTRY_DATE}", DateTime.Now.ToString("dd/MM/yyyy"));
                result = result.Replace("{ENTRY_TIME}", DateTime.Now.ToString("HH:mm:ss"));
                result = result.Replace("{EXIT_DATE}", DateTime.Now.AddHours(2).ToString("dd/MM/yyyy"));
                result = result.Replace("{EXIT_TIME}", DateTime.Now.AddHours(2).ToString("HH:mm:ss"));
            }
            
            return result;
        }
    }
}