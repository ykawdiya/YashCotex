using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using WeighbridgeSoftwareYashCotex.Models;

namespace WeighbridgeSoftwareYashCotex.Services
{
    public class GoogleSheetsService : IDisposable
    {
        private SheetsService? _sheetsService;
        private readonly DatabaseService _databaseService;
        private bool _isConfigured = false;
        private string _spreadsheetId = string.Empty;
        private string _serviceAccountKeyPath = string.Empty;
        
        public bool IsConfigured => _isConfigured;
        public string SpreadsheetId => _spreadsheetId;
        
        public event EventHandler<string>? SyncStatusChanged;
        public event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;

        public GoogleSheetsService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        #region Configuration

        public async Task<bool> ConfigureAsync(string serviceAccountKeyPath, string spreadsheetId)
        {
            try
            {
                if (!File.Exists(serviceAccountKeyPath))
                {
                    throw new FileNotFoundException("Service account key file not found");
                }

                _serviceAccountKeyPath = serviceAccountKeyPath;
                _spreadsheetId = spreadsheetId;

                // Initialize Google Sheets service
                var credential = GoogleCredential.FromFile(serviceAccountKeyPath)
                    .CreateScoped(SheetsService.Scope.Spreadsheets);

                _sheetsService = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Weighbridge Software - Yash Cotex"
                });

                // Test connection by reading spreadsheet metadata
                var spreadsheet = await _sheetsService.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
                
                _isConfigured = true;
                SyncStatusChanged?.Invoke(this, $"Connected to spreadsheet: {spreadsheet.Properties.Title}");
                
                return true;
            }
            catch (Google.GoogleApiException gex)
            {
                var fullMessage = $"Google API error ({gex.HttpStatusCode}): {gex.Message}";
                if (gex.Error != null)
                {
                    fullMessage += $"\nDetails: {gex.Error.Message}";
                }

                _isConfigured = false;
                SyncStatusChanged?.Invoke(this, fullMessage);
                return false;
            }
            catch (Exception ex)
            {
                _isConfigured = false;
                SyncStatusChanged?.Invoke(this, $"Unknown error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                if (!_isConfigured || _sheetsService == null)
                {
                    SyncStatusChanged?.Invoke(this, "Service not configured");
                    return false;
                }

                var spreadsheet = await _sheetsService.Spreadsheets.Get(_spreadsheetId).ExecuteAsync();
                SyncStatusChanged?.Invoke(this, $"✅ Connection successful: {spreadsheet.Properties.Title}");
                return true;
            }
            catch (Exception ex)
            {
                SyncStatusChanged?.Invoke(this, $"❌ Connection failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Sheet Setup

        public async Task<bool> SetupWorksheetsAsync()
        {
            try
            {
                if (!_isConfigured || _sheetsService == null)
                    return false;

                var spreadsheet = await _sheetsService.Spreadsheets.Get(_spreadsheetId).ExecuteAsync();
                var existingSheets = spreadsheet.Sheets.Select(s => s.Properties.Title).ToList();

                var requiredSheets = new List<(string name, string[] headers)>
                {
                    ("RST_Records", new[] { "RST", "Date", "Vehicle", "Customer", "Phone", "Material", "Address", "Entry_Weight", "Exit_Weight", "Net_Weight", "Entry_Time", "Exit_Time", "Status" }),
                    ("Sync_Log", new[] { "Timestamp", "Operation", "Records_Count", "Status", "Details" }),
                    ("Materials", new[] { "Material_Name", "Description", "Unit", "Category", "Active" }),
                    ("Addresses", new[] { "Address", "City", "State", "PIN", "Active" }),
                    ("Config", new[] { "Setting", "Value", "Description", "Last_Updated" })
                };

                var requests = new List<Request>();

                foreach (var (name, headers) in requiredSheets)
                {
                    if (!existingSheets.Contains(name))
                    {
                        // Create new sheet
                        requests.Add(new Request
                        {
                            AddSheet = new AddSheetRequest
                            {
                                Properties = new SheetProperties
                                {
                                    Title = name,
                                    GridProperties = new GridProperties
                                    {
                                        FrozenRowCount = 1 // Freeze header row
                                    }
                                }
                            }
                        });
                    }
                }

                if (requests.Any())
                {
                    var batchUpdateRequest = new BatchUpdateSpreadsheetRequest
                    {
                        Requests = requests
                    };

                    await _sheetsService.Spreadsheets.BatchUpdate(batchUpdateRequest, _spreadsheetId).ExecuteAsync();
                }

                // Add headers to new sheets
                foreach (var (name, headers) in requiredSheets)
                {
                    if (!existingSheets.Contains(name))
                    {
                        await WriteHeadersAsync(name, headers);
                    }
                }

                SyncStatusChanged?.Invoke(this, "Worksheets setup completed");
                return true;
            }
            catch (Exception ex)
            {
                SyncStatusChanged?.Invoke(this, $"Worksheet setup failed: {ex.Message}");
                return false;
            }
        }

        private async Task WriteHeadersAsync(string sheetName, string[] headers)
        {
            var range = $"{sheetName}!A1:{GetColumnLetter(headers.Length)}1";
            var valueRange = new ValueRange
            {
                Values = new List<IList<object>> { headers.Cast<object>().ToList() }
            };

            var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, _spreadsheetId, range);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await updateRequest.ExecuteAsync();

            // Format headers
            await FormatHeadersAsync(sheetName, headers.Length);
        }

        private async Task FormatHeadersAsync(string sheetName, int columnCount)
        {
            try
            {
                var requests = new List<Request>
                {
                    new Request
                    {
                        RepeatCell = new RepeatCellRequest
                        {
                            Range = new GridRange
                            {
                                SheetId = await GetSheetIdAsync(sheetName),
                                StartRowIndex = 0,
                                EndRowIndex = 1,
                                StartColumnIndex = 0,
                                EndColumnIndex = columnCount
                            },
                            Cell = new CellData
                            {
                                UserEnteredFormat = new CellFormat
                                {
                                    BackgroundColor = new Color { Red = 0.2f, Green = 0.3f, Blue = 0.5f },
                                    TextFormat = new TextFormat 
                                    { 
                                        Bold = true, 
                                        ForegroundColor = new Color { Red = 1f, Green = 1f, Blue = 1f } 
                                    }
                                }
                            },
                            Fields = "userEnteredFormat(backgroundColor,textFormat)"
                        }
                    }
                };

                var batchUpdateRequest = new BatchUpdateSpreadsheetRequest { Requests = requests };
                await _sheetsService.Spreadsheets.BatchUpdate(batchUpdateRequest, _spreadsheetId).ExecuteAsync();
            }
            catch (Exception ex)
            {
                // Log but don't fail on formatting errors
                System.Diagnostics.Debug.WriteLine($"Header formatting failed: {ex.Message}");
            }
        }

        #endregion

        #region Data Sync

        public async Task<SyncResult> SyncAllDataAsync()
        {
            var result = new SyncResult();
            
            try
            {
                if (!_isConfigured)
                {
                    result.Success = false;
                    result.Message = "Google Sheets not configured";
                    return result;
                }

                SyncStatusChanged?.Invoke(this, "Starting full sync...");
                
                // Setup worksheets if needed
                await SetupWorksheetsAsync();

                // Sync RST records
                var rstResult = await SyncRstRecordsAsync();
                result.RstRecordsSynced = rstResult.recordsSynced;
                result.Errors.AddRange(rstResult.errors);

                // Sync materials
                var materialsResult = await SyncMaterialsAsync();
                result.MaterialsSynced = materialsResult.recordsSynced;
                result.Errors.AddRange(materialsResult.errors);

                // Sync addresses
                var addressesResult = await SyncAddressesAsync();
                result.AddressesSynced = addressesResult.recordsSynced;
                result.Errors.AddRange(addressesResult.errors);

                // Log sync operation
                await LogSyncOperationAsync("FULL_SYNC", result.TotalRecordsSynced, result.Success);

                result.Success = result.Errors.Count == 0;
                result.Message = result.Success 
                    ? $"Sync completed: {result.TotalRecordsSynced} records"
                    : $"Sync completed with {result.Errors.Count} errors";

                SyncStatusChanged?.Invoke(this, result.Message);
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Sync failed: {ex.Message}";
                result.Errors.Add(ex.Message);
                
                SyncStatusChanged?.Invoke(this, result.Message);
                return result;
            }
        }

        public async Task<(int recordsSynced, List<string> errors)> SyncRstRecordsAsync()
        {
            var errors = new List<string>();
            var recordsSynced = 0;

            try
            {
                SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs("Syncing RST records...", 0));

                var allRecords = _databaseService.GetAllWeighments();
                
                if (!allRecords.Any())
                {
                    return (0, errors);
                }

                // Clear existing data (except headers)
                await ClearSheetDataAsync("RST_Records");

                var values = new List<IList<object>>();
                
                for (int i = 0; i < allRecords.Count; i++)
                {
                    var record = allRecords[i];
                    
                    values.Add(new List<object>
                    {
                        record.RstNumber,
                        record.EntryDateTime.ToString("dd/MM/yyyy"),
                        record.VehicleNumber,
                        record.Name,
                        record.PhoneNumber,
                        record.Material,
                        record.Address,
                        record.EntryWeight,
                        record.ExitWeight?.ToString() ?? "",
                        record.NetWeight.ToString("F2"),
                        record.EntryDateTime.ToString("dd/MM/yyyy HH:mm:ss"),
                        record.ExitDateTime?.ToString("dd/MM/yyyy HH:mm:ss") ?? "",
                        record.ExitDateTime.HasValue ? "Completed" : "Pending"
                    });

                    recordsSynced++;
                    
                    var progress = (i + 1) * 100 / allRecords.Count;
                    SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs($"Syncing RST records... {i + 1}/{allRecords.Count}", progress));
                }

                if (values.Any())
                {
                    var range = $"RST_Records!A2:{GetColumnLetter(13)}{values.Count + 1}";
                    var valueRange = new ValueRange { Values = values };

                    var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, _spreadsheetId, range);
                    updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                    await updateRequest.ExecuteAsync();
                }

                SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs("RST records sync completed", 100));
            }
            catch (Exception ex)
            {
                errors.Add($"RST sync error: {ex.Message}");
            }

            return (recordsSynced, errors);
        }

        public async Task<(int recordsSynced, List<string> errors)> SyncMaterialsAsync()
        {
            var errors = new List<string>();
            var recordsSynced = 0;

            try
            {
                var materials = _databaseService.GetMaterials();
                
                if (!materials.Any())
                {
                    return (0, errors);
                }

                await ClearSheetDataAsync("Materials");

                var values = materials.Select(m => (IList<object>)new List<object>
                {
                    m,
                    $"Material: {m}",
                    "KG",
                    "Raw Material",
                    "TRUE"
                }).ToList();

                if (values.Any())
                {
                    var range = $"Materials!A2:{GetColumnLetter(5)}{values.Count + 1}";
                    var valueRange = new ValueRange { Values = values };

                    var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, _spreadsheetId, range);
                    updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                    await updateRequest.ExecuteAsync();
                    
                    recordsSynced = values.Count;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Materials sync error: {ex.Message}");
            }

            return (recordsSynced, errors);
        }

        public async Task<(int recordsSynced, List<string> errors)> SyncAddressesAsync()
        {
            var errors = new List<string>();
            var recordsSynced = 0;

            try
            {
                var addresses = _databaseService.GetAddresses();
                
                if (!addresses.Any())
                {
                    return (0, errors);
                }

                await ClearSheetDataAsync("Addresses");

                var values = addresses.Select(a => (IList<object>)new List<object>
                {
                    a,
                    ExtractCity(a),
                    "Punjab",
                    "160059",
                    "TRUE"
                }).ToList();

                if (values.Any())
                {
                    var range = $"Addresses!A2:{GetColumnLetter(5)}{values.Count + 1}";
                    var valueRange = new ValueRange { Values = values };

                    var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, _spreadsheetId, range);
                    updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                    await updateRequest.ExecuteAsync();
                    
                    recordsSynced = values.Count;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Addresses sync error: {ex.Message}");
            }

            return (recordsSynced, errors);
        }

        private async Task LogSyncOperationAsync(string operation, int recordsCount, bool success)
        {
            try
            {
                var values = new List<IList<object>>
                {
                    new List<object>
                    {
                        DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                        operation,
                        recordsCount,
                        success ? "SUCCESS" : "FAILED",
                        $"Synced {recordsCount} records"
                    }
                };

                // Append to sync log
                var range = "Sync_Log!A:E";
                var valueRange = new ValueRange { Values = values };

                var appendRequest = _sheetsService.Spreadsheets.Values.Append(valueRange, _spreadsheetId, range);
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
                await appendRequest.ExecuteAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Log sync operation failed: {ex.Message}");
            }
        }

        #endregion

        #region Data Import

        public async Task<ImportResult> ImportRstDataAsync()
        {
            var result = new ImportResult();

            try
            {
                if (!_isConfigured || _sheetsService == null)
                {
                    result.Success = false;
                    result.Message = "Service not configured";
                    return result;
                }

                SyncStatusChanged?.Invoke(this, "Importing RST data from Google Sheets...");

                var range = "RST_Records!A2:M";
                var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
                var response = await request.ExecuteAsync();

                if (response.Values == null || !response.Values.Any())
                {
                    result.Message = "No data found in RST_Records sheet";
                    return result;
                }

                var importedCount = 0;
                var skippedCount = 0;
                var errorCount = 0;

                foreach (var row in response.Values)
                {
                    try
                    {
                        if (row.Count < 5) continue; // Skip incomplete rows

                        var rstNumber = Convert.ToInt32(row[0]);
                        var existingEntry = _databaseService.GetWeighmentByRst(rstNumber);

                        if (existingEntry != null)
                        {
                            skippedCount++;
                            continue; // Skip existing records
                        }

                        var entry = new WeighmentEntry
                        {
                            RstNumber = rstNumber,
                            VehicleNumber = row[2]?.ToString() ?? "",
                            Name = row[3]?.ToString() ?? "",
                            PhoneNumber = row[4]?.ToString() ?? "",
                            Material = row[5]?.ToString() ?? "",
                            Address = row[6]?.ToString() ?? "",
                            EntryWeight = Convert.ToDouble(row[7] ?? "0"),
                            EntryDateTime = DateTime.ParseExact(row[10]?.ToString() ?? DateTime.Now.ToString(), "dd/MM/yyyy HH:mm:ss", null)
                        };

                        if (row.Count > 8 && !string.IsNullOrEmpty(row[8]?.ToString()))
                        {
                            entry.ExitWeight = Convert.ToDouble(row[8]);
                            entry.ExitDateTime = DateTime.ParseExact(row[11]?.ToString() ?? DateTime.Now.ToString(), "dd/MM/yyyy HH:mm:ss", null);
                        }

                        _databaseService.SaveEntry(entry);
                        importedCount++;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        result.Errors.Add($"Row import error: {ex.Message}");
                    }
                }

                result.RecordsImported = importedCount;
                result.RecordsSkipped = skippedCount;
                result.Success = errorCount == 0;
                result.Message = $"Import completed: {importedCount} imported, {skippedCount} skipped, {errorCount} errors";

                SyncStatusChanged?.Invoke(this, result.Message);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Import failed: {ex.Message}";
                result.Errors.Add(ex.Message);
            }

            return result;
        }

        #endregion

        #region Helper Methods

        private async Task ClearSheetDataAsync(string sheetName)
        {
            try
            {
                var range = $"{sheetName}!A2:Z";
                var clearRequest = _sheetsService.Spreadsheets.Values.Clear(new ClearValuesRequest(), _spreadsheetId, range);
                await clearRequest.ExecuteAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clear sheet data failed: {ex.Message}");
            }
        }

        private async Task<int?> GetSheetIdAsync(string sheetName)
        {
            try
            {
                var spreadsheet = await _sheetsService.Spreadsheets.Get(_spreadsheetId).ExecuteAsync();
                var sheet = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.Title == sheetName);
                return sheet?.Properties.SheetId;
            }
            catch
            {
                return null;
            }
        }

        private string GetColumnLetter(int columnNumber)
        {
            string columnName = "";
            while (columnNumber > 0)
            {
                int modulo = (columnNumber - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                columnNumber = (columnNumber - modulo) / 26;
            }
            return columnName;
        }

        private string ExtractCity(string address)
        {
            // Simple city extraction - in real implementation, use proper parsing
            var parts = address.Split(',');
            return parts.Length > 0 ? parts[0].Trim() : address;
        }

        #endregion

        public void Dispose()
        {
            _sheetsService?.Dispose();
        }
    }

    #region Result Classes

    public class SyncResult
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = "";
        public int RstRecordsSynced { get; set; }
        public int MaterialsSynced { get; set; }
        public int AddressesSynced { get; set; }
        public List<string> Errors { get; set; } = new();
        
        public int TotalRecordsSynced => RstRecordsSynced + MaterialsSynced + AddressesSynced;
    }

    public class ImportResult
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = "";
        public int RecordsImported { get; set; }
        public int RecordsSkipped { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class SyncProgressEventArgs : EventArgs
    {
        public string Message { get; }
        public int ProgressPercentage { get; }

        public SyncProgressEventArgs(string message, int progressPercentage)
        {
            Message = message;
            ProgressPercentage = progressPercentage;
        }
    }

    #endregion
}