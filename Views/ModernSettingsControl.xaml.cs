using System;
using System.Windows;
using System.Windows.Controls;
using WeighbridgeSoftwareYashCotex.ViewModels;

namespace WeighbridgeSoftwareYashCotex.Views
{
    public partial class ModernSettingsControl : UserControl, IDisposable
    {
        public event EventHandler<string>? FormCompleted;

        public ModernSettingsControl()
        {
            InitializeComponent();
            this.Loaded += ModernSettingsControl_Loaded;
        }

        private void ModernSettingsControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize any additional setup here
            if (DataContext is ModernSettingsViewModel viewModel)
            {
                // Subscribe to view model events if needed
            }
        }

        public void Dispose()
        {
            // Clean up resources
            if (DataContext is IDisposable disposableViewModel)
            {
                disposableViewModel.Dispose();
            }
        }
    }
}