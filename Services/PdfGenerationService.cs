using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout.Borders;
using WeighbridgeSoftwareYashCotex.Models;
using System.Linq;
using IOPath = System.IO.Path;

namespace WeighbridgeSoftwareYashCotex.Services
{
    public class PdfGenerationService
    {
        private readonly CameraService? _cameraService;
        private readonly DatabaseService _databaseService;

        public PdfGenerationService(CameraService? cameraService = null, DatabaseService? databaseService = null)
        {
            _cameraService = cameraService;
            _databaseService = databaseService ?? new DatabaseService();
        }

        #region Weighment Slip Generation

        public async Task<string> GenerateWeighmentSlipAsync(WeighmentEntry entry, bool includeImages = true)
        {
            try
            {
                var filename = $"WeighmentSlip_RST{entry.RstNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var outputPath = IOPath.Combine("./PDFReports", filename);
                
                Directory.CreateDirectory("./PDFReports");

                // Capture camera images if service is available and includeImages is true
                Dictionary<CameraPosition, BitmapImage?>? cameraImages = null;
                if (includeImages && _cameraService != null)
                {
                    cameraImages = await _cameraService.CaptureAllImagesAsync();
                }

                using var writer = new PdfWriter(outputPath);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf, PageSize.A4);
                
                // Set margins
                document.SetMargins(40, 40, 40, 40);

                // Create weighment slip content
                await CreateWeighmentSlipContent(document, entry, cameraImages);

                return outputPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to generate weighment slip: {ex.Message}", ex);
            }
        }

        private async Task CreateWeighmentSlipContent(Document document, WeighmentEntry entry, Dictionary<CameraPosition, BitmapImage?>? cameraImages)
        {
            // Header with company logo and info
            await AddCompanyHeader(document);

            // Title
            AddTitle(document, "WEIGHMENT SLIP");

            // RST and basic info
            AddBasicInfo(document, entry);

            // Vehicle and customer details
            AddVehicleAndCustomerDetails(document, entry);

            // Weight details
            AddWeightDetails(document, entry);

            // Camera images if available
            if (cameraImages != null && cameraImages.Any(kvp => kvp.Value != null))
            {
                document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                AddCameraImages(document, cameraImages);
            }

            // Terms and footer
            AddTermsAndFooter(document);
        }

        private async Task AddCompanyHeader(Document document)
        {
            // Company header table
            var headerTable = new Table(UnitValue.CreatePercentArray(new[] { 20f, 60f, 20f }))
                .UseAllAvailableWidth();

            // Logo placeholder (you can add actual logo image here)
            var logoCell = new Cell()
                .Add(new Paragraph("🏭\nLOGO").SetTextAlignment(TextAlignment.CENTER).SetFontSize(12))
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.CENTER);

            // Company info
            var companyInfo = new Cell()
                .Add(new Paragraph("YASH COTEX PRIVATE LIMITED")
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                    .SetFontSize(18)
                    .SetTextAlignment(TextAlignment.CENTER))
                .Add(new Paragraph("Industrial Weighbridge Solutions")
                    .SetFontSize(12)
                    .SetTextAlignment(TextAlignment.CENTER))
                .Add(new Paragraph("Address: Industrial Area, Mohali, Punjab - 160059")
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.CENTER))
                .Add(new Paragraph("Phone: +91-9876543210 | Email: info@yashcotex.com")
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.CENTER))
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.CENTER);

            // Date and time
            var dateTimeCell = new Cell()
                .Add(new Paragraph($"Date: {DateTime.Now:dd/MM/yyyy}")
                    .SetFontSize(10))
                .Add(new Paragraph($"Time: {DateTime.Now:HH:mm:ss}")
                    .SetFontSize(10))
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT);

            headerTable.AddCell(logoCell);
            headerTable.AddCell(companyInfo);
            headerTable.AddCell(dateTimeCell);

            document.Add(headerTable);
            document.Add(new Paragraph("\n"));
        }

        private void AddTitle(Document document, string title)
        {
            var titleParagraph = new Paragraph(title)
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                .SetFontSize(20)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20);

            document.Add(titleParagraph);
        }

        private void AddBasicInfo(Document document, WeighmentEntry entry)
        {
            var basicInfoTable = new Table(UnitValue.CreatePercentArray(new[] { 25f, 25f, 25f, 25f }))
                .UseAllAvailableWidth()
                .SetMarginBottom(15);

            // RST Number
            basicInfoTable.AddCell(CreateInfoCell("RST Number:", $"{entry.RstNumber}", true));
            
            // Status
            var status = entry.ExitDateTime.HasValue ? "COMPLETED" : "PENDING";
            basicInfoTable.AddCell(CreateInfoCell("Status:", status, entry.ExitDateTime.HasValue));
            
            // Entry Date
            basicInfoTable.AddCell(CreateInfoCell("Entry Date:", entry.EntryDateTime.ToString("dd/MM/yyyy"), false));
            
            // Entry Time
            basicInfoTable.AddCell(CreateInfoCell("Entry Time:", entry.EntryDateTime.ToString("HH:mm:ss"), false));

            document.Add(basicInfoTable);
        }

        private void AddVehicleAndCustomerDetails(Document document, WeighmentEntry entry)
        {
            var detailsTable = new Table(UnitValue.CreatePercentArray(new[] { 50f, 50f }))
                .UseAllAvailableWidth()
                .SetMarginBottom(15);

            // Left column - Vehicle details
            var vehicleCell = new Cell()
                .Add(new Paragraph("VEHICLE DETAILS")
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                    .SetFontSize(12)
                    .SetMarginBottom(10))
                .Add(new Paragraph($"Vehicle Number: {entry.VehicleNumber}")
                    .SetFontSize(10))
                .Add(new Paragraph($"Material: {entry.Material}")
                    .SetFontSize(10))
                .Add(new Paragraph($"Destination: {entry.Address}")
                    .SetFontSize(10))
                .SetPadding(10)
                .SetBorder(new SolidBorder(1));

            // Right column - Customer details
            var customerCell = new Cell()
                .Add(new Paragraph("CUSTOMER DETAILS")
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                    .SetFontSize(12)
                    .SetMarginBottom(10))
                .Add(new Paragraph($"Name: {entry.Name}")
                    .SetFontSize(10))
                .Add(new Paragraph($"Phone: {entry.PhoneNumber}")
                    .SetFontSize(10))
                .Add(new Paragraph($"Created: {entry.CreatedDate:dd/MM/yyyy HH:mm}")
                    .SetFontSize(10))
                .SetPadding(10)
                .SetBorder(new SolidBorder(1));

            detailsTable.AddCell(vehicleCell);
            detailsTable.AddCell(customerCell);

            document.Add(detailsTable);
        }

        private void AddWeightDetails(Document document, WeighmentEntry entry)
        {
            var weightTable = new Table(UnitValue.CreatePercentArray(new[] { 25f, 25f, 25f, 25f }))
                .UseAllAvailableWidth()
                .SetMarginBottom(20);

            // Header row
            weightTable.AddHeaderCell(CreateHeaderCell("WEIGHT TYPE"));
            weightTable.AddHeaderCell(CreateHeaderCell("WEIGHT (KG)"));
            weightTable.AddHeaderCell(CreateHeaderCell("DATE"));
            weightTable.AddHeaderCell(CreateHeaderCell("TIME"));

            // Entry weight row
            weightTable.AddCell(CreateWeightCell("Entry Weight", false));
            weightTable.AddCell(CreateWeightCell($"{entry.EntryWeight:F2}", false));
            weightTable.AddCell(CreateWeightCell(entry.EntryDateTime.ToString("dd/MM/yyyy"), false));
            weightTable.AddCell(CreateWeightCell(entry.EntryDateTime.ToString("HH:mm:ss"), false));

            // Exit weight row
            if (entry.ExitDateTime.HasValue)
            {
                weightTable.AddCell(CreateWeightCell("Exit Weight", false));
                weightTable.AddCell(CreateWeightCell($"{entry.ExitWeight:F2}", false));
                weightTable.AddCell(CreateWeightCell(entry.ExitDateTime.Value.ToString("dd/MM/yyyy"), false));
                weightTable.AddCell(CreateWeightCell(entry.ExitDateTime.Value.ToString("HH:mm:ss"), false));

                // Net weight row (highlighted)
                weightTable.AddCell(CreateWeightCell("Net Weight", true));
                weightTable.AddCell(CreateWeightCell($"{entry.NetWeight:F2}", true));
                weightTable.AddCell(CreateWeightCell("Calculated", true));
                weightTable.AddCell(CreateWeightCell("Auto", true));
            }
            else
            {
                weightTable.AddCell(CreateWeightCell("Exit Weight", false));
                weightTable.AddCell(CreateWeightCell("PENDING", false));
                weightTable.AddCell(CreateWeightCell("-", false));
                weightTable.AddCell(CreateWeightCell("-", false));
            }

            document.Add(weightTable);
        }

        private void AddCameraImages(Document document, Dictionary<CameraPosition, BitmapImage?> cameraImages)
        {
            // Camera images title
            AddTitle(document, "CAMERA CAPTURES");

            var imageTable = new Table(UnitValue.CreatePercentArray(new[] { 50f, 50f }))
                .UseAllAvailableWidth()
                .SetMarginBottom(20);

            var imagePositions = new[]
            {
                (CameraPosition.Entry, "Entry Camera"),
                (CameraPosition.Exit, "Exit Camera"),
                (CameraPosition.LeftSide, "Left Side Camera"),
                (CameraPosition.RightSide, "Right Side Camera")
            };

            foreach (var (position, title) in imagePositions)
            {
                var imageCell = new Cell();
                
                if (cameraImages.ContainsKey(position) && cameraImages[position] != null)
                {
                    try
                    {
                        // Convert BitmapImage to byte array for iText
                        var imageBytes = BitmapImageToByteArray(cameraImages[position]!);
                        var imageData = ImageDataFactory.Create(imageBytes);
                        var image = new Image(imageData)
                            .SetWidth(200)
                            .SetHeight(150)
                            .SetHorizontalAlignment(HorizontalAlignment.CENTER);

                        imageCell.Add(new Paragraph(title)
                            .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                            .SetFontSize(10)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetMarginBottom(5));
                        imageCell.Add(image);
                        imageCell.Add(new Paragraph($"Captured: {DateTime.Now:HH:mm:ss}")
                            .SetFontSize(8)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetMarginTop(5));
                    }
                    catch (Exception ex)
                    {
                        // If image conversion fails, show placeholder
                        imageCell.Add(CreateImagePlaceholder(title, $"Image Error: {ex.Message}"));
                    }
                }
                else
                {
                    // No image available
                    imageCell.Add(CreateImagePlaceholder(title, "No image captured"));
                }

                imageCell.SetPadding(10)
                    .SetBorder(new SolidBorder(1))
                    .SetTextAlignment(TextAlignment.CENTER);

                imageTable.AddCell(imageCell);

                // Add row break after every 2 images
                if (Array.IndexOf(imagePositions, (position, title)) % 2 == 1)
                {
                    // Complete the row if we have odd number of images
                }
            }

            document.Add(imageTable);
        }

        private Paragraph CreateImagePlaceholder(string title, string message)
        {
            return new Paragraph()
                .Add(new Paragraph(title)
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.CENTER))
                .Add(new Paragraph("\n📷\n")
                    .SetFontSize(24)
                    .SetTextAlignment(TextAlignment.CENTER))
                .Add(new Paragraph(message)
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.CENTER))
                .SetHeight(150)
                .SetTextAlignment(TextAlignment.CENTER);
        }

        private void AddTermsAndFooter(Document document)
        {
            // Terms and conditions
            var termsTitle = new Paragraph("TERMS & CONDITIONS")
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                .SetFontSize(12)
                .SetMarginTop(20)
                .SetMarginBottom(10);

            var termsList = new List()
                .Add(new ListItem("This weighment slip is computer generated and does not require signature."))
                .Add(new ListItem("Weight measurement is accurate as per calibrated weighbridge."))
                .Add(new ListItem("Any disputes should be reported within 24 hours."))
                .Add(new ListItem("Camera captures are taken for security and verification purposes."))
                .SetFontSize(9);

            document.Add(termsTitle);
            document.Add(termsList);

            // Footer
            var footer = new Paragraph($"\nGenerated by Weighbridge Software v1.0 | Printed on: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                .SetFontSize(8)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(20);

            document.Add(footer);
        }

        #endregion

        #region Report Generation

        public async Task<string> GenerateDailyReportAsync(DateTime date)
        {
            try
            {
                var entries = _databaseService.GetWeighmentsByDateRange(date.Date, date.Date.AddDays(1).AddTicks(-1));
                
                var filename = $"DailyReport_{date:yyyyMMdd}.pdf";
                var outputPath = IOPath.Combine("./PDFReports", filename);
                
                Directory.CreateDirectory("./PDFReports");

                using var writer = new PdfWriter(outputPath);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf, PageSize.A4.Rotate()); // Landscape for table
                
                document.SetMargins(30, 30, 30, 30);

                await CreateDailyReportContent(document, entries, date);

                return outputPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to generate daily report: {ex.Message}", ex);
            }
        }

        private async Task CreateDailyReportContent(Document document, List<WeighmentEntry> entries, DateTime date)
        {
            // Header
            await AddCompanyHeader(document);

            // Report title
            AddTitle(document, $"DAILY WEIGHMENT REPORT - {date:dd/MM/yyyy}");

            // Summary statistics
            AddDailyReportSummary(document, entries);

            // Detailed entries table
            AddDetailedEntriesTable(document, entries);

            // Footer
            AddReportFooter(document, entries.Count);
        }

        private void AddDailyReportSummary(Document document, List<WeighmentEntry> entries)
        {
            var completedEntries = entries.Where(e => e.ExitDateTime.HasValue).ToList();
            var pendingEntries = entries.Where(e => !e.ExitDateTime.HasValue).ToList();
            var totalWeight = completedEntries.Sum(e => e.NetWeight);

            var summaryTable = new Table(UnitValue.CreatePercentArray(new[] { 25f, 25f, 25f, 25f }))
                .UseAllAvailableWidth()
                .SetMarginBottom(20);

            summaryTable.AddCell(CreateSummaryCell("Total Entries", entries.Count.ToString()));
            summaryTable.AddCell(CreateSummaryCell("Completed", completedEntries.Count.ToString()));
            summaryTable.AddCell(CreateSummaryCell("Pending", pendingEntries.Count.ToString()));
            summaryTable.AddCell(CreateSummaryCell("Total Net Weight", $"{totalWeight:F2} KG"));

            document.Add(summaryTable);
        }

        private void AddDetailedEntriesTable(Document document, List<WeighmentEntry> entries)
        {
            var entriesTable = new Table(UnitValue.CreatePercentArray(new[] { 8f, 15f, 15f, 12f, 15f, 10f, 10f, 15f }))
                .UseAllAvailableWidth()
                .SetFontSize(8);

            // Headers
            string[] headers = { "RST", "Vehicle", "Customer", "Material", "Entry Time", "Entry Wt", "Exit Wt", "Net Wt" };
            foreach (var header in headers)
            {
                entriesTable.AddHeaderCell(CreateHeaderCell(header));
            }

            // Data rows
            foreach (var entry in entries.OrderBy(e => e.RstNumber))
            {
                entriesTable.AddCell(CreateDataCell(entry.RstNumber.ToString()));
                entriesTable.AddCell(CreateDataCell(entry.VehicleNumber));
                entriesTable.AddCell(CreateDataCell(entry.Name));
                entriesTable.AddCell(CreateDataCell(entry.Material));
                entriesTable.AddCell(CreateDataCell(entry.EntryDateTime.ToString("HH:mm")));
                entriesTable.AddCell(CreateDataCell($"{entry.EntryWeight:F1}"));
                entriesTable.AddCell(CreateDataCell(entry.ExitWeight?.ToString("F1") ?? "Pending"));
                entriesTable.AddCell(CreateDataCell(entry.ExitDateTime.HasValue ? $"{entry.NetWeight:F1}" : "-"));
            }

            document.Add(entriesTable);
        }

        private void AddReportFooter(Document document, int totalEntries)
        {
            var footer = new Paragraph($"\nTotal Records: {totalEntries} | Report Generated: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(20);

            document.Add(footer);
        }

        #endregion

        #region Helper Methods

        private Cell CreateInfoCell(string label, string value, bool highlight)
        {
            var cell = new Cell()
                .Add(new Paragraph(label)
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                    .SetFontSize(9)
                    .SetMarginBottom(2))
                .Add(new Paragraph(value)
                    .SetFontSize(11))
                .SetPadding(8)
                .SetBorder(new SolidBorder(1));

            if (highlight)
            {
                cell.SetBackgroundColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY);
            }

            return cell;
        }

        private Cell CreateHeaderCell(string text)
        {
            return new Cell()
                .Add(new Paragraph(text)
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                    .SetFontSize(10))
                .SetBackgroundColor(iText.Kernel.Colors.ColorConstants.GRAY)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(5)
                .SetBorder(new SolidBorder(1));
        }

        private Cell CreateWeightCell(string text, bool highlight)
        {
            var cell = new Cell()
                .Add(new Paragraph(text).SetFontSize(10))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(5)
                .SetBorder(new SolidBorder(1));

            if (highlight)
            {
                cell.SetBackgroundColor(iText.Kernel.Colors.ColorConstants.YELLOW)
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD));
            }

            return cell;
        }

        private Cell CreateSummaryCell(string label, string value)
        {
            return new Cell()
                .Add(new Paragraph(label)
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.CENTER))
                .Add(new Paragraph(value)
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.CENTER))
                .SetPadding(10)
                .SetBorder(new SolidBorder(1))
                .SetTextAlignment(TextAlignment.CENTER);
        }

        private Cell CreateDataCell(string text)
        {
            return new Cell()
                .Add(new Paragraph(text).SetFontSize(8))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(3)
                .SetBorder(new SolidBorder(0.5f));
        }

        private byte[] BitmapImageToByteArray(BitmapImage bitmapImage)
        {
            try
            {
                using var stream = new MemoryStream();
                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to convert image to byte array: {ex.Message}", ex);
            }
        }

        #endregion
    }
}