using System;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;

namespace WeighbridgeSoftwareYashCotex.Services
{
    public class LedDisplayService : IDisposable
    {
        private SerialPort? _serialPort;
        private bool _isConnected = false;

        public bool TestDisplay(string comPort, int baudRate, double weight)
        {
            try
            {
                using var testPort = new SerialPort(comPort, baudRate, Parity.None, 8, StopBits.One);
                testPort.ReadTimeout = 2000;
                testPort.WriteTimeout = 2000;
                
                testPort.Open();
                
                // Send test weight data in standard ASCII format
                var weightString = FormatWeight(weight, "####.## KG");
                testPort.WriteLine(weightString);
                
                testPort.Close();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LED Display test failed: {ex.Message}");
                return false;
            }
        }

        public bool Connect(string comPort, int baudRate)
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                }

                _serialPort = new SerialPort(comPort, baudRate, Parity.None, 8, StopBits.One);
                _serialPort.ReadTimeout = 1000;
                _serialPort.WriteTimeout = 1000;
                
                _serialPort.Open();
                _isConnected = true;
                
                Console.WriteLine($"LED Display connected on {comPort} at {baudRate} baud");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to LED Display: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        public void SendWeight(double weight, double adjustment, string format = "####.## KG")
        {
            if (!_isConnected || _serialPort == null || !_serialPort.IsOpen)
            {
                return;
            }

            try
            {
                // Apply adjustment - this is the key requirement
                // The displayed weight includes adjustment without operators knowing
                var adjustedWeight = weight + adjustment;
                
                var weightString = FormatWeight(adjustedWeight, format);
                _serialPort.WriteLine(weightString);
                
                // Log for debugging (but don't show to operators)
                Console.WriteLine($"LED Display: Raw={weight:F2}, Adjustment={adjustment:F2}, Displayed={adjustedWeight:F2}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending weight to LED Display: {ex.Message}");
            }
        }

        public async Task SendWeightAsync(double weight, double adjustment, string format = "####.## KG")
        {
            await Task.Run(() => SendWeight(weight, adjustment, format));
        }

        private string FormatWeight(double weight, string format)
        {
            return format switch
            {
                "####.## KG" => $"{weight:0000.00} KG",
                "######.# KG" => $"{weight:000000.0} KG", 
                "##### KG" => $"{weight:00000} KG",
                "####.## T" => $"{weight / 1000:0000.00} T",
                _ => $"{weight:F2} KG"
            };
        }

        public void Disconnect()
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
                _isConnected = false;
                Console.WriteLine("LED Display disconnected");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting LED Display: {ex.Message}");
            }
        }

        public bool IsConnected => _isConnected && _serialPort?.IsOpen == true;

        public void Dispose()
        {
            Disconnect();
            _serialPort?.Dispose();
        }
    }
}