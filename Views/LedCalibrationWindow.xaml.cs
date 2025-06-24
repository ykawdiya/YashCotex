using System;
using System.Windows;

namespace WeighbridgeSoftwareYashCotex.Views
{
    public partial class LedCalibrationWindow : Window
    {
        public double CalculatedAdjustment { get; private set; }

        public LedCalibrationWindow()
        {
            InitializeComponent();
            CalculateAdjustment();
        }

        private void DisplayedWeightTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CalculateAdjustment();
        }

        private void CalculateAdjustment()
        {
            try
            {
                if (double.TryParse(ActualWeightTextBox?.Text, out var actualWeight) &&
                    double.TryParse(DisplayedWeightTextBox?.Text, out var displayedWeight))
                {
                    // Adjustment = What we want to show - What currently shows
                    CalculatedAdjustment = actualWeight - displayedWeight;
                    
                    if (CalculatedAdjustmentTextBox != null)
                        CalculatedAdjustmentTextBox.Text = CalculatedAdjustment.ToString("F2");
                    
                    if (ResultPreviewTextBox != null)
                        ResultPreviewTextBox.Text = $"{actualWeight:F2} KG";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating adjustment: {ex.Message}");
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!double.TryParse(ActualWeightTextBox.Text, out var actualWeight) ||
                    !double.TryParse(DisplayedWeightTextBox.Text, out var displayedWeight))
                {
                    MessageBox.Show("Please enter valid numeric values for both weights.", 
                                   "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (actualWeight <= 0)
                {
                    MessageBox.Show("Actual weight must be greater than zero.", 
                                   "Invalid Weight", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                CalculateAdjustment();
                
                var confirmResult = MessageBox.Show(
                    $"Calibration Summary:\n\n" +
                    $"Reference Weight: {actualWeight:F2} KG\n" +
                    $"Current Display: {displayedWeight:F2} KG\n" +
                    $"Calculated Adjustment: {CalculatedAdjustment:F2} KG\n\n" +
                    $"After applying this adjustment, the LED will show {actualWeight:F2} KG " +
                    $"when the actual weight is {actualWeight:F2} KG.\n\n" +
                    $"Apply this calibration?",
                    "Confirm Calibration", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);

                if (confirmResult == MessageBoxResult.Yes)
                {
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during calibration: {ex.Message}", 
                               "Calibration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ActualWeightTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CalculateAdjustment();
        }
    }
}