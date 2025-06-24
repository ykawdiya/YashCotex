using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;

namespace WeighbridgeSoftwareYashCotex.Services
{
    public class DotMatrixPrintService
    {
        private string _textToPrint = "";
        private int _charactersPerLine = 80;

        public bool PrintText(string text, int charactersPerLine = 80)
        {
            try
            {
                _textToPrint = text;
                _charactersPerLine = charactersPerLine;

                var printDocument = new PrintDocument();
                printDocument.PrintPage += PrintDocument_PrintPage;
                
                // Set up for dot matrix printing
                printDocument.DefaultPageSettings.Margins = new Margins(50, 50, 50, 50);
                
                printDocument.Print();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Print error: {ex.Message}");
                return false;
            }
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            try
            {
                // Use a monospace font suitable for dot matrix printing
                var font = new Font("Courier New", 10, FontStyle.Regular);
                var brush = new SolidBrush(Color.Black);
                
                var lines = _textToPrint.Split('\n');
                var yPosition = e.MarginBounds.Top;
                var lineHeight = font.GetHeight(e.Graphics);
                
                foreach (var line in lines)
                {
                    if (yPosition + lineHeight > e.MarginBounds.Bottom)
                        break; // Page is full
                        
                    // Ensure line doesn't exceed character limit
                    var printLine = line.Length > _charactersPerLine 
                        ? line.Substring(0, _charactersPerLine) 
                        : line;
                    
                    e.Graphics.DrawString(printLine, font, brush, e.MarginBounds.Left, yPosition);
                    yPosition += (int)lineHeight;
                }
                
                e.HasMorePages = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Print page error: {ex.Message}");
            }
        }

        public bool SaveToFile(string text, string filePath, int charactersPerLine = 80)
        {
            try
            {
                var lines = text.Split('\n');
                var processedLines = new string[lines.Length];
                
                for (int i = 0; i < lines.Length; i++)
                {
                    // Ensure each line doesn't exceed character limit
                    processedLines[i] = lines[i].Length > charactersPerLine 
                        ? lines[i].Substring(0, charactersPerLine) 
                        : lines[i];
                }
                
                File.WriteAllLines(filePath, processedLines);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save to file error: {ex.Message}");
                return false;
            }
        }
    }
}