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

        public void SendWeight(double adjustedWeight)
        {
            if (!_isConnected || _serialPort == null || !_serialPort.IsOpen)
            {
                return;
            }

            try
            {
                // Send only numeric weight value (already adjusted by weight rules)
                var weightString = adjustedWeight.ToString("F2");
                _serialPort.WriteLine(weightString);
                
                // Log for debugging
                Console.WriteLine($"LED Display: Sent={adjustedWeight:F2}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending weight to LED Display: {ex.Message}");
            }
        }

        public async Task SendWeightAsync(double adjustedWeight)
        {
            await Task.Run(() => SendWeight(adjustedWeight));
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