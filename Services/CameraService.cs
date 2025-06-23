using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WeighbridgeSoftwareYashCotex.Services
{
    public class CameraService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly List<CameraConfiguration> _cameras;
        private bool _isMonitoring = false;
        private System.Timers.Timer? _refreshTimer;

        public event EventHandler<CameraImageEventArgs>? ImageUpdated;
        public event EventHandler<CameraStatusEventArgs>? StatusChanged;

        public bool IsMonitoring => _isMonitoring;
        public IReadOnlyList<CameraConfiguration> Cameras => _cameras.AsReadOnly();

        public CameraService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _cameras = new List<CameraConfiguration>();
            
            // Initialize with default camera configurations
            InitializeDefaultCameras();
        }

        #region Camera Configuration

        private void InitializeDefaultCameras()
        {
            _cameras.AddRange(new[]
            {
                new CameraConfiguration
                {
                    Id = 1,
                    Name = "Entry Camera",
                    IpAddress = "192.168.1.101",
                    Port = 80,
                    Username = "admin",
                    Password = "admin123",
                    StreamUrl = "http://192.168.1.101/mjpeg/1",
                    IsEnabled = true,
                    Position = CameraPosition.Entry
                },
                new CameraConfiguration
                {
                    Id = 2,
                    Name = "Exit Camera", 
                    IpAddress = "192.168.1.102",
                    Port = 80,
                    Username = "admin",
                    Password = "admin123",
                    StreamUrl = "http://192.168.1.102/mjpeg/1",
                    IsEnabled = true,
                    Position = CameraPosition.Exit
                },
                new CameraConfiguration
                {
                    Id = 3,
                    Name = "Left Side Camera",
                    IpAddress = "192.168.1.103",
                    Port = 80,
                    Username = "admin",
                    Password = "admin123",
                    StreamUrl = "http://192.168.1.103/mjpeg/1",
                    IsEnabled = true,
                    Position = CameraPosition.LeftSide
                },
                new CameraConfiguration
                {
                    Id = 4,
                    Name = "Right Side Camera",
                    IpAddress = "192.168.1.104",
                    Port = 80,
                    Username = "admin",
                    Password = "admin123",
                    StreamUrl = "http://192.168.1.104/mjpeg/1",
                    IsEnabled = true,
                    Position = CameraPosition.RightSide
                }
            });
        }

        public void UpdateCameraConfiguration(CameraConfiguration config)
        {
            var existingCamera = _cameras.FirstOrDefault(c => c.Id == config.Id);
            if (existingCamera != null)
            {
                existingCamera.Name = config.Name;
                existingCamera.IpAddress = config.IpAddress;
                existingCamera.Port = config.Port;
                existingCamera.Username = config.Username;
                existingCamera.Password = config.Password;
                existingCamera.StreamUrl = config.StreamUrl;
                existingCamera.IsEnabled = config.IsEnabled;
                existingCamera.Position = config.Position;
                
                // Update stream URL if needed
                if (string.IsNullOrEmpty(existingCamera.StreamUrl))
                {
                    existingCamera.StreamUrl = GenerateStreamUrl(existingCamera);
                }
            }
        }

        public CameraConfiguration? GetCamera(int cameraId)
        {
            return _cameras.FirstOrDefault(c => c.Id == cameraId);
        }

        public CameraConfiguration? GetCameraByPosition(CameraPosition position)
        {
            return _cameras.FirstOrDefault(c => c.Position == position && c.IsEnabled);
        }

        private string GenerateStreamUrl(CameraConfiguration camera)
        {
            // Generate common IP camera stream URLs
            var baseUrl = $"http://{camera.IpAddress}:{camera.Port}";
            
            // Try common stream endpoints
            var commonEndpoints = new[]
            {
                "/mjpeg/1",
                "/video.mjpeg",
                "/videostream.cgi",
                "/cgi-bin/mjpg/video.cgi",
                "/axis-cgi/mjpg/video.cgi"
            };

            return baseUrl + commonEndpoints[0]; // Default to first option
        }

        #endregion

        #region Camera Monitoring

        public async Task<bool> StartMonitoringAsync()
        {
            if (_isMonitoring) return true;

            try
            {
                _isMonitoring = true;
                StatusChanged?.Invoke(this, new CameraStatusEventArgs("Starting camera monitoring..."));

                // Test all enabled cameras
                var testResults = await TestAllCamerasAsync();
                var workingCameras = testResults.Count(r => r.Success);
                
                StatusChanged?.Invoke(this, new CameraStatusEventArgs(
                    $"Camera monitoring started: {workingCameras}/{_cameras.Count(c => c.IsEnabled)} cameras online"));

                // Start refresh timer for live feeds
                _refreshTimer = new System.Timers.Timer(1000); // 1 second refresh
                _refreshTimer.Elapsed += async (sender, e) => await RefreshCameraFeeds();
                _refreshTimer.Start();

                return true;
            }
            catch (Exception ex)
            {
                _isMonitoring = false;
                StatusChanged?.Invoke(this, new CameraStatusEventArgs($"Failed to start monitoring: {ex.Message}"));
                return false;
            }
        }

        public void StopMonitoring()
        {
            _isMonitoring = false;
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            _refreshTimer = null;
            
            StatusChanged?.Invoke(this, new CameraStatusEventArgs("Camera monitoring stopped"));
        }

        private async Task RefreshCameraFeeds()
        {
            if (!_isMonitoring) return;

            var tasks = _cameras.Where(c => c.IsEnabled).Select(async camera =>
            {
                try
                {
                    var image = await CaptureImageAsync(camera);
                    if (image != null)
                    {
                        ImageUpdated?.Invoke(this, new CameraImageEventArgs(camera.Id, image));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Camera {camera.Id} refresh failed: {ex.Message}");
                }
            });

            await Task.WhenAll(tasks);
        }

        #endregion

        #region Camera Operations

        public async Task<List<CameraTestResult>> TestAllCamerasAsync()
        {
            var results = new List<CameraTestResult>();
            
            StatusChanged?.Invoke(this, new CameraStatusEventArgs("Testing camera connections..."));

            var tasks = _cameras.Where(c => c.IsEnabled).Select(async camera =>
            {
                var result = await TestCameraConnectionAsync(camera);
                results.Add(result);
                return result;
            });

            await Task.WhenAll(tasks);
            return results;
        }

        public async Task<CameraTestResult> TestCameraConnectionAsync(CameraConfiguration camera)
        {
            try
            {
                StatusChanged?.Invoke(this, new CameraStatusEventArgs($"Testing {camera.Name}..."));

                // Test basic connectivity
                var pingResult = await PingCameraAsync(camera);
                if (!pingResult)
                {
                    return new CameraTestResult
                    {
                        CameraId = camera.Id,
                        CameraName = camera.Name,
                        Success = false,
                        Message = "Camera not reachable"
                    };
                }

                // Test stream access
                var streamResult = await TestStreamAccessAsync(camera);
                if (!streamResult)
                {
                    return new CameraTestResult
                    {
                        CameraId = camera.Id,
                        CameraName = camera.Name,
                        Success = false,
                        Message = "Stream not accessible"
                    };
                }

                return new CameraTestResult
                {
                    CameraId = camera.Id,
                    CameraName = camera.Name,
                    Success = true,
                    Message = "Camera online and streaming"
                };
            }
            catch (Exception ex)
            {
                return new CameraTestResult
                {
                    CameraId = camera.Id,
                    CameraName = camera.Name,
                    Success = false,
                    Message = $"Test failed: {ex.Message}"
                };
            }
        }

        private async Task<bool> PingCameraAsync(CameraConfiguration camera)
        {
            try
            {
                var url = $"http://{camera.IpAddress}:{camera.Port}";
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                
                if (!string.IsNullOrEmpty(camera.Username) && !string.IsNullOrEmpty(camera.Password))
                {
                    var authValue = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{camera.Username}:{camera.Password}"));
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
                }

                using var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TestStreamAccessAsync(CameraConfiguration camera)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, camera.StreamUrl);
                
                if (!string.IsNullOrEmpty(camera.Username) && !string.IsNullOrEmpty(camera.Password))
                {
                    var authValue = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{camera.Username}:{camera.Password}"));
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
                }

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<BitmapImage?> CaptureImageAsync(CameraConfiguration camera)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, camera.StreamUrl);
                
                if (!string.IsNullOrEmpty(camera.Username) && !string.IsNullOrEmpty(camera.Password))
                {
                    var authValue = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{camera.Username}:{camera.Password}"));
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
                }

                using var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    return CreateBitmapImageFromBytes(imageBytes);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Image capture failed for {camera.Name}: {ex.Message}");
            }

            return null;
        }

        public async Task<BitmapImage?> CaptureImageByPositionAsync(CameraPosition position)
        {
            var camera = GetCameraByPosition(position);
            if (camera == null) return null;

            return await CaptureImageAsync(camera);
        }

        public async Task<Dictionary<CameraPosition, BitmapImage?>> CaptureAllImagesAsync()
        {
            var results = new Dictionary<CameraPosition, BitmapImage?>();
            
            var tasks = _cameras.Where(c => c.IsEnabled).Select(async camera =>
            {
                var image = await CaptureImageAsync(camera);
                lock (results)
                {
                    results[camera.Position] = image;
                }
            });

            await Task.WhenAll(tasks);
            return results;
        }

        private BitmapImage CreateBitmapImageFromBytes(byte[] imageBytes)
        {
            var bitmap = new BitmapImage();
            using var stream = new MemoryStream(imageBytes);
            
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            
            return bitmap;
        }

        #endregion

        #region Snapshot Management

        public async Task<string?> SaveSnapshotAsync(CameraConfiguration camera, string directory)
        {
            try
            {
                var image = await CaptureImageAsync(camera);
                if (image == null) return null;

                Directory.CreateDirectory(directory);
                var fileName = $"{camera.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                var filePath = Path.Combine(directory, fileName);

                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                
                using var fileStream = new FileStream(filePath, FileMode.Create);
                encoder.Save(fileStream);

                return filePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Snapshot save failed: {ex.Message}");
                return null;
            }
        }

        public async Task<Dictionary<CameraPosition, string?>> SaveAllSnapshotsAsync(string directory)
        {
            var results = new Dictionary<CameraPosition, string?>();
            
            Directory.CreateDirectory(directory);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            
            var tasks = _cameras.Where(c => c.IsEnabled).Select(async camera =>
            {
                try
                {
                    var image = await CaptureImageAsync(camera);
                    if (image != null)
                    {
                        var fileName = $"{camera.Position}_{timestamp}.jpg";
                        var filePath = Path.Combine(directory, fileName);

                        var encoder = new JpegBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(image));
                        
                        using var fileStream = new FileStream(filePath, FileMode.Create);
                        encoder.Save(fileStream);

                        lock (results)
                        {
                            results[camera.Position] = filePath;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Snapshot failed for {camera.Name}: {ex.Message}");
                    lock (results)
                    {
                        results[camera.Position] = null;
                    }
                }
            });

            await Task.WhenAll(tasks);
            return results;
        }

        #endregion

        public void Dispose()
        {
            StopMonitoring();
            _httpClient?.Dispose();
        }
    }

    #region Data Classes

    public class CameraConfiguration
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public int Port { get; set; } = 80;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string StreamUrl { get; set; } = "";
        public string Protocol { get; set; } = "http";
        public string StreamPath { get; set; } = "/mjpeg/1";
        public bool IsEnabled { get; set; } = true;
        public CameraPosition Position { get; set; }
        public int RefreshRate { get; set; } = 1000; // milliseconds
    }

    public enum CameraPosition
    {
        Entry,
        Exit,
        LeftSide,
        RightSide
    }

    public class CameraTestResult
    {
        public int CameraId { get; set; }
        public string CameraName { get; set; } = "";
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    public class CameraImageEventArgs : EventArgs
    {
        public int CameraId { get; }
        public BitmapImage Image { get; }

        public CameraImageEventArgs(int cameraId, BitmapImage image)
        {
            CameraId = cameraId;
            Image = image;
        }
    }

    public class CameraStatusEventArgs : EventArgs
    {
        public string Message { get; }

        public CameraStatusEventArgs(string message)
        {
            Message = message;
        }
    }

    #endregion
}