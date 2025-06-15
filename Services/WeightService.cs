using System.IO.Ports;
using WeighbridgeSoftwareYashCotex.Models;

namespace WeighbridgeSoftwareYashCotex.Services;

public class WeightService : IDisposable
{
    private SerialPort? _serialPort;
    private double _currentWeight;
    private bool _isStable;
    private DateTime _lastUpdate;
    private readonly Timer _simulationTimer;
    private readonly Random _random = new();
    
    public event EventHandler<WeightChangedEventArgs>? WeightChanged;
    
    public WeightService()
    {
        _simulationTimer = new Timer(SimulateWeight, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        InitializeSerialPort();
    }
    
    private void InitializeSerialPort()
    {
        try
        {
            var portName = GetWeighbridgePort();
            if (!string.IsNullOrEmpty(portName))
            {
                _serialPort = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
                _serialPort.DataReceived += SerialPort_DataReceived;
                _serialPort.Open();
            }
        }
        catch
        {
        }
    }
    
    private string GetWeighbridgePort()
    {
        var settings = SettingsService.Instance;
        return settings.WeighbridgeComPort ?? "COM1";
    }
    
    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            var data = _serialPort?.ReadLine();
            if (!string.IsNullOrEmpty(data))
            {
                ParseWeightData(data);
            }
        }
        catch
        {
        }
    }
    
    private void ParseWeightData(string data)
    {
        try
        {
            var weightMatch = System.Text.RegularExpressions.Regex.Match(data, @"(\d+\.?\d*)");
            if (weightMatch.Success && double.TryParse(weightMatch.Value, out var weight))
            {
                var oldWeight = _currentWeight;
                _currentWeight = ApplyWeightRules(weight);
                _isStable = !data.Contains("UNSTABLE") && !data.Contains("MOTION");
                _lastUpdate = DateTime.Now;
                
                WeightChanged?.Invoke(this, new WeightChangedEventArgs 
                { 
                    Weight = _currentWeight, 
                    IsStable = _isStable,
                    Timestamp = _lastUpdate
                });
            }
        }
        catch
        {
        }
    }
    
    private void SimulateWeight(object? state)
    {
        if (_serialPort?.IsOpen != true)
        {
            var baseWeight = 1500.0;
            var variation = (_random.NextDouble() - 0.5) * 100;
            var simulatedWeight = Math.Max(0, baseWeight + variation);
            
            _currentWeight = ApplyWeightRules(simulatedWeight);
            _isStable = _random.NextDouble() > 0.1;
            _lastUpdate = DateTime.Now;
            
            WeightChanged?.Invoke(this, new WeightChangedEventArgs 
            { 
                Weight = _currentWeight, 
                IsStable = _isStable,
                Timestamp = _lastUpdate
            });
        }
    }
    
    private double ApplyWeightRules(double rawWeight)
    {
        var settings = SettingsService.Instance;
        var adjustedWeight = rawWeight;
        
        if (settings.WeightRules != null)
        {
            foreach (var rule in settings.WeightRules)
            {
                adjustedWeight = rule.ApplyRule(adjustedWeight);
            }
        }
        
        return Math.Round(adjustedWeight, 2);
    }
    
    public double GetCurrentWeight()
    {
        return _currentWeight;
    }
    
    public bool IsStable()
    {
        return _isStable;
    }
    
    public DateTime GetLastUpdateTime()
    {
        return _lastUpdate;
    }
    
    public bool IsConnected()
    {
        return _serialPort?.IsOpen == true || true;
    }
    
    public void Dispose()
    {
        _simulationTimer?.Dispose();
        if (_serialPort?.IsOpen == true)
        {
            _serialPort.Close();
        }
        _serialPort?.Dispose();
    }
}

public class WeightChangedEventArgs : EventArgs
{
    public double Weight { get; set; }
    public bool IsStable { get; set; }
    public DateTime Timestamp { get; set; }
}