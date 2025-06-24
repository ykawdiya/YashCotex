using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WeighbridgeSoftwareYashCotex.Models;

namespace WeighbridgeSoftwareYashCotex.Services
{
    public class MultiLedDisplayService : IDisposable
    {
        private readonly Dictionary<string, LedDisplayService> _activeDisplays = new();
        private readonly SettingsService _settingsService;

        public MultiLedDisplayService()
        {
            _settingsService = SettingsService.Instance;
        }

        public void InitializeDisplays()
        {
            try
            {
                // Dispose existing connections
                foreach (var display in _activeDisplays.Values)
                {
                    display.Dispose();
                }
                _activeDisplays.Clear();

                // Initialize enabled displays
                var enabledDisplays = _settingsService.LedDisplays?.Where(d => d.Enabled) ?? new List<LedDisplayConfiguration>();
                
                foreach (var config in enabledDisplays)
                {
                    try
                    {
                        var ledService = new LedDisplayService();
                        if (ledService.Connect(config.ComPort, config.BaudRate))
                        {
                            _activeDisplays[config.Id] = ledService;
                            Console.WriteLine($"LED Display '{config.Name}' connected on {config.ComPort}");
                        }
                        else
                        {
                            Console.WriteLine($"Failed to connect LED Display '{config.Name}' on {config.ComPort}");
                            ledService.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error initializing LED Display '{config.Name}': {ex.Message}");
                    }
                }

                Console.WriteLine($"Initialized {_activeDisplays.Count} LED displays");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing LED displays: {ex.Message}");
            }
        }

        public void SendWeightToAllDisplays(double rawWeight)
        {
            if (_activeDisplays.Count == 0)
                return;

            try
            {
                // Apply weight rules to get adjusted weight
                var adjustedWeight = ApplyWeightRules(rawWeight);

                // Send to all connected displays
                foreach (var display in _activeDisplays.Values)
                {
                    display.SendWeight(adjustedWeight);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending weight to LED displays: {ex.Message}");
            }
        }

        public async Task SendWeightToAllDisplaysAsync(double rawWeight)
        {
            if (_activeDisplays.Count == 0)
                return;

            try
            {
                // Apply weight rules to get adjusted weight
                var adjustedWeight = ApplyWeightRules(rawWeight);

                // Send to all connected displays concurrently
                var tasks = _activeDisplays.Values.Select(display => display.SendWeightAsync(adjustedWeight));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending weight to LED displays: {ex.Message}");
            }
        }

        private double ApplyWeightRules(double rawWeight)
        {
            try
            {
                // Apply weight rules from settings
                var weightRules = _settingsService.WeightRules ?? new List<WeightRule>();
                
                double adjustedWeight = rawWeight;
                
                foreach (var rule in weightRules)
                {
                    // Apply rule using the existing ApplyRule method
                    if (IsRuleApplicable(rule, rawWeight))
                    {
                        if (rule.IsPercentage)
                        {
                            adjustedWeight += (rawWeight * rule.AdjustmentValue / 100);
                        }
                        else
                        {
                            adjustedWeight += rule.AdjustmentValue;
                        }
                        
                        Console.WriteLine($"Applied weight rule '{rule.Name}': {rawWeight:F2} -> {adjustedWeight:F2}");
                    }
                }
                
                return adjustedWeight;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying weight rules: {ex.Message}");
                return rawWeight; // Return original weight if rules fail
            }
        }

        private bool IsRuleApplicable(WeightRule rule, double weight)
        {
            try
            {
                // For now, apply all rules. 
                // Rule conditions can be expanded based on the Condition property
                return !string.IsNullOrEmpty(rule.Name);
            }
            catch
            {
                return false;
            }
        }

        public void RefreshConnections()
        {
            InitializeDisplays();
        }

        public int ConnectedDisplayCount => _activeDisplays.Count;

        public List<string> GetConnectedDisplays()
        {
            var configs = _settingsService.LedDisplays ?? new List<LedDisplayConfiguration>();
            return _activeDisplays.Keys
                .Select(id => configs.FirstOrDefault(c => c.Id == id)?.Name ?? "Unknown")
                .ToList();
        }

        public void Dispose()
        {
            foreach (var display in _activeDisplays.Values)
            {
                display.Dispose();
            }
            _activeDisplays.Clear();
        }
    }
}