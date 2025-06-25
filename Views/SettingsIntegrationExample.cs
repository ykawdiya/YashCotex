using System.Windows;
using WeighbridgeSoftwareYashCotex.Views;

namespace WeighbridgeSoftwareYashCotex.Examples
{
    /// <summary>
    /// Example of how to integrate the ModernSettingsControl into your MainWindow
    /// </summary>
    public static class SettingsIntegrationExample
    {
        /// <summary>
        /// Example method showing how to replace the old settings with the new modern settings
        /// Call this from your MainWindow's SettingsButton_Clicked method
        /// </summary>
        public static void ShowModernSettings(MainWindow mainWindow)
        {
            try
            {
                // Create the modern settings control
                var modernSettings = new ModernSettingsControl();
                
                // Handle the FormCompleted event
                modernSettings.FormCompleted += (sender, message) =>
                {
                    // Update status and return to home
                    if (mainWindow != null)
                    {
                        var latestOperationProperty = mainWindow.GetType().GetProperty("LatestOperation");
                        if (latestOperationProperty != null)
                        {
                            var latestOperationControl = latestOperationProperty.GetValue(mainWindow);
                            if (latestOperationControl != null)
                            {
                                var textProperty = latestOperationControl.GetType().GetProperty("Text");
                                textProperty?.SetValue(latestOperationControl, message);
                            }
                        }
                        
                        // Call ShowHome method via reflection
                        var showHomeMethod = mainWindow.GetType().GetMethod("ShowHome", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        showHomeMethod?.Invoke(mainWindow, null);
                    }
                };

                // Get the FullScreenFormPresenter from MainWindow
                var fullScreenPresenterProperty = mainWindow.GetType().GetProperty("FullScreenFormPresenter", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (fullScreenPresenterProperty?.GetValue(mainWindow) is System.Windows.Controls.ContentPresenter presenter)
                {
                    // Hide cameras and live weight
                    HideCamerasAndLiveWeight(mainWindow);
                    
                    // Show modern settings in full-screen
                    presenter.Content = modernSettings;
                    presenter.Visibility = Visibility.Visible;
                    
                    // Update status
                    UpdateLatestOperation(mainWindow, "Modern Settings opened");
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error opening modern settings: {ex.Message}", "Settings Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void HideCamerasAndLiveWeight(MainWindow mainWindow)
        {
            try
            {
                // Hide camera grids
                var leftCamerasGrid = mainWindow.FindName("LeftCamerasGrid") as System.Windows.UIElement;
                var rightCamerasGrid = mainWindow.FindName("RightCamerasGrid") as System.Windows.UIElement;
                var liveWeightPanel = mainWindow.FindName("LiveWeightPanel") as System.Windows.UIElement;
                
                if (leftCamerasGrid != null) leftCamerasGrid.Visibility = Visibility.Collapsed;
                if (rightCamerasGrid != null) rightCamerasGrid.Visibility = Visibility.Collapsed;
                if (liveWeightPanel != null) liveWeightPanel.Visibility = Visibility.Collapsed;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error hiding UI elements: {ex.Message}");
            }
        }

        private static void UpdateLatestOperation(MainWindow mainWindow, string message)
        {
            try
            {
                var latestOperation = mainWindow.FindName("LatestOperation") as System.Windows.Controls.TextBlock;
                if (latestOperation != null)
                {
                    latestOperation.Text = message;
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating latest operation: {ex.Message}");
            }
        }
    }
}