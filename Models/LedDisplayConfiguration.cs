using System;

namespace WeighbridgeSoftwareYashCotex.Models
{
    public class LedDisplayConfiguration
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "LED Display";
        public bool Enabled { get; set; } = true;
        public string ComPort { get; set; } = "COM1";
        public int BaudRate { get; set; } = 9600;
        public string Protocol { get; set; } = "Standard ASCII";
        public int UpdateFrequency { get; set; } = 500;
        public int DataBits { get; set; } = 8;
        public string Parity { get; set; } = "None";
        public string StopBits { get; set; } = "1";
    }
}