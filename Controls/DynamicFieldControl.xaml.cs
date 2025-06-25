using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using WeighbridgeSoftwareYashCotex.Models;

namespace WeighbridgeSoftwareYashCotex.Controls
{
    public partial class DynamicFieldControl : UserControl
    {
        public static readonly DependencyProperty FieldProperty =
            DependencyProperty.Register("Field", typeof(SettingsField), typeof(DynamicFieldControl),
                new PropertyMetadata(null, OnFieldChanged));

        public SettingsField Field
        {
            get { return (SettingsField)GetValue(FieldProperty); }
            set { SetValue(FieldProperty, value); }
        }

        public DynamicFieldControl()
        {
            InitializeComponent();
        }

        private static void OnFieldChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DynamicFieldControl)d;
            var field = (SettingsField)e.NewValue;
            
            if (field != null)
            {
                control.CreateFieldContent(field);
            }
        }

        private void CreateFieldContent(SettingsField field)
        {
            FrameworkElement element = field.FieldType switch
            {
                FieldType.Text => CreateTextBox(field),
                FieldType.Password => CreatePasswordBox(field),
                FieldType.Number => CreateNumberBox(field),
                FieldType.Dropdown => CreateComboBox(field),
                FieldType.Checkbox => CreateCheckBox(field),
                FieldType.File => CreateFileSelector(field),
                FieldType.Color => CreateColorPicker(field),
                _ => CreateTextBox(field)
            };

            // Bind the value
            var binding = new Binding("Value")
            {
                Source = field,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            switch (field.FieldType)
            {
                case FieldType.Text:
                case FieldType.Number:
                    ((TextBox)element).SetBinding(TextBox.TextProperty, binding);
                    break;
                case FieldType.Password:
                    // Password boxes require special handling for security
                    var passwordBox = (PasswordBox)element;
                    passwordBox.Password = field.Value?.ToString() ?? "";
                    passwordBox.PasswordChanged += (s, e) => field.Value = passwordBox.Password;
                    break;
                case FieldType.Dropdown:
                    ((ComboBox)element).SetBinding(ComboBox.SelectedValueProperty, binding);
                    break;
                case FieldType.Checkbox:
                    ((CheckBox)element).SetBinding(CheckBox.IsCheckedProperty, binding);
                    break;
            }

            FieldContent.Content = element;
        }

        private TextBox CreateTextBox(SettingsField field)
        {
            var textBox = new TextBox
            {
                Style = (Style)FindResource("ModernTextBoxStyle"),
                ToolTip = field.Tooltip
            };
            
            if (!string.IsNullOrEmpty(field.Placeholder))
            {
                // Add placeholder functionality
                textBox.Tag = field.Placeholder;
                SetPlaceholderBehavior(textBox);
            }

            return textBox;
        }

        private PasswordBox CreatePasswordBox(SettingsField field)
        {
            return new PasswordBox
            {
                Style = (Style)FindResource("ModernInputStyle"),
                ToolTip = field.Tooltip
            };
        }

        private TextBox CreateNumberBox(SettingsField field)
        {
            var textBox = CreateTextBox(field);
            
            // Add number validation
            textBox.PreviewTextInput += (s, e) =>
            {
                e.Handled = !IsTextAllowed(e.Text);
            };

            return textBox;
        }

        private ComboBox CreateComboBox(SettingsField field)
        {
            var comboBox = new ComboBox
            {
                Style = (Style)FindResource("ModernComboBoxStyle"),
                ToolTip = field.Tooltip,
                DisplayMemberPath = "Text",
                SelectedValuePath = "Value"
            };

            if (field.Options != null)
            {
                comboBox.ItemsSource = field.Options;
            }

            return comboBox;
        }

        private CheckBox CreateCheckBox(SettingsField field)
        {
            return new CheckBox
            {
                Style = (Style)FindResource("ModernCheckBoxStyle"),
                Content = field.CheckboxText ?? "",
                ToolTip = field.Tooltip
            };
        }

        private StackPanel CreateFileSelector(SettingsField field)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            
            var textBox = new TextBox
            {
                Style = (Style)FindResource("ModernTextBoxStyle"),
                IsReadOnly = true,
                MinWidth = 200,
                ToolTip = field.Tooltip
            };
            
            var button = new Button
            {
                Content = "📁 Browse...",
                Style = (Style)FindResource("ModernButtonStyle"),
                Margin = new Thickness(8, 0, 0, 0)
            };
            
            button.Click += (s, e) =>
            {
                var dialog = new OpenFileDialog
                {
                    Filter = field.FileFilter ?? "All Files (*.*)|*.*"
                };
                
                if (dialog.ShowDialog() == true)
                {
                    textBox.Text = dialog.FileName;
                    field.Value = dialog.FileName;
                }
            };
            
            panel.Children.Add(textBox);
            panel.Children.Add(button);
            
            return panel;
        }

        private Border CreateColorPicker(SettingsField field)
        {
            var border = new Border
            {
                Width = 100,
                Height = 44,
                BorderBrush = System.Windows.Media.Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Background = System.Windows.Media.Brushes.White,
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = field.Tooltip
            };
            
            border.MouseLeftButtonUp += (s, e) =>
            {
                // Simple color picker with predefined colors
                var colorWindow = new Window
                {
                    Title = "Select Color",
                    Width = 300,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(this)
                };

                var colorPanel = new WrapPanel { Margin = new Thickness(10) };
                var predefinedColors = new[]
                {
                    "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF", "#00FFFF",
                    "#FFA500", "#800080", "#008000", "#FFC0CB", "#A52A2A", "#808080",
                    "#000000", "#FFFFFF", "#C0C0C0", "#800000", "#808000", "#008080"
                };

                foreach (var colorHex in predefinedColors)
                {
                    var colorBorder = new Border
                    {
                        Width = 30,
                        Height = 30,
                        Margin = new Thickness(2),
                        Background = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex)),
                        BorderBrush = System.Windows.Media.Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Cursor = System.Windows.Input.Cursors.Hand
                    };

                    colorBorder.MouseLeftButtonUp += (cs, ce) =>
                    {
                        var selectedColor = ((System.Windows.Media.SolidColorBrush)colorBorder.Background).Color;
                        border.Background = new System.Windows.Media.SolidColorBrush(selectedColor);
                        field.Value = selectedColor.ToString();
                        colorWindow.Close();
                    };

                    colorPanel.Children.Add(colorBorder);
                }

                colorWindow.Content = colorPanel;
                colorWindow.ShowDialog();
            };
            
            return border;
        }

        private void SetPlaceholderBehavior(TextBox textBox)
        {
            var placeholder = textBox.Tag.ToString();
            
            textBox.GotFocus += (s, e) =>
            {
                if (textBox.Text == placeholder)
                {
                    textBox.Text = "";
                    textBox.Foreground = System.Windows.Media.Brushes.Black;
                }
            };
            
            textBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = placeholder;
                    textBox.Foreground = System.Windows.Media.Brushes.Gray;
                }
            };
            
            // Set initial placeholder
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = placeholder;
                textBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private static bool IsTextAllowed(string text)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(text, @"^[0-9.-]+$");
        }
    }
}