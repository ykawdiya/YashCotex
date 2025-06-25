using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using WeighbridgeSoftwareYashCotex.Models;
using WeighbridgeSoftwareYashCotex.Converters;

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
                // Set the DataContext so the XAML bindings work
                control.DataContext = field;
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

            // Set up binding and initial values based on field type
            SetupFieldBinding(element, field);

            FieldContent.Content = element;
        }

        private void SetupFieldBinding(FrameworkElement element, SettingsField field)
        {
            // Common binding setup
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
                    var textBox = (TextBox)element;
                    textBox.SetBinding(TextBox.TextProperty, binding);
                    
                    // Force initial value to be visible
                    textBox.Loaded += (s, e) =>
                    {
                        if (field.Value != null && !string.IsNullOrEmpty(field.Value.ToString()))
                        {
                            textBox.Text = field.Value.ToString();
                            textBox.Foreground = System.Windows.Media.Brushes.Black;
                        }
                    };
                    break;

                case FieldType.Password:
                    var passwordBox = (PasswordBox)element;
                    // Set initial value immediately
                    passwordBox.Password = field.Value?.ToString() ?? "";
                    
                    // Set up two-way binding manually for PasswordBox
                    passwordBox.PasswordChanged += (s, e) => field.Value = passwordBox.Password;
                    
                    // Update password when field value changes
                    field.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == "Value" && passwordBox.Password != field.Value?.ToString())
                        {
                            passwordBox.Password = field.Value?.ToString() ?? "";
                        }
                    };
                    break;

                case FieldType.Dropdown:
                    var comboBox = (ComboBox)element;
                    comboBox.SetBinding(ComboBox.SelectedValueProperty, binding);
                    
                    // Ensure selection is visible after loading
                    comboBox.Loaded += (s, e) =>
                    {
                        if (field.Value != null)
                        {
                            comboBox.SelectedValue = field.Value;
                            // If SelectedValue doesn't work, try finding by string match
                            if (comboBox.SelectedItem == null && field.Options != null)
                            {
                                var matchingOption = field.Options.FirstOrDefault(o => 
                                    o.Value?.ToString() == field.Value.ToString());
                                if (matchingOption != null)
                                {
                                    comboBox.SelectedItem = matchingOption;
                                }
                            }
                        }
                    };
                    break;

                case FieldType.Checkbox:
                    var checkBox = (CheckBox)element;
                    
                    // Set up boolean conversion binding
                    var checkBoxBinding = new Binding("Value")
                    {
                        Source = field,
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                        Converter = new BooleanConverter()
                    };
                    checkBox.SetBinding(CheckBox.IsCheckedProperty, checkBoxBinding);
                    
                    // Force initial checked state
                    checkBox.Loaded += (s, e) =>
                    {
                        if (field.Value != null)
                        {
                            if (bool.TryParse(field.Value.ToString(), out bool isChecked))
                            {
                                checkBox.IsChecked = isChecked;
                            }
                            else
                            {
                                checkBox.IsChecked = field.Value as bool? ?? false;
                            }
                        }
                    };
                    break;

                case FieldType.File:
                    var filePanel = (StackPanel)element;
                    var fileTextBox = (TextBox)filePanel.Children[0];
                    fileTextBox.SetBinding(TextBox.TextProperty, binding);
                    
                    // Set initial file path
                    fileTextBox.Loaded += (s, e) =>
                    {
                        if (field.Value != null)
                        {
                            fileTextBox.Text = field.Value.ToString();
                        }
                    };
                    break;

                case FieldType.Color:
                    var colorBorder = (Border)element;
                    // Set initial color if available
                    if (field.Value != null)
                    {
                        try
                        {
                            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(field.Value.ToString());
                            colorBorder.Background = new System.Windows.Media.SolidColorBrush(color);
                        }
                        catch
                        {
                            // Use default color if parsing fails
                        }
                    }
                    break;
            }
        }

        private TextBox CreateTextBox(SettingsField field)
        {
            var textBox = new TextBox
            {
                Style = (Style)FindResource("ModernTextBoxStyle"),
                ToolTip = field.Tooltip,
                IsEnabled = field.IsEnabled
            };
            
            // Bind IsEnabled to field's IsEnabled property
            var enabledBinding = new Binding("IsEnabled")
            {
                Source = field,
                Mode = BindingMode.OneWay
            };
            textBox.SetBinding(TextBox.IsEnabledProperty, enabledBinding);
            
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
            var passwordBox = new PasswordBox
            {
                Style = (Style)FindResource("ModernInputStyle"),
                ToolTip = field.Tooltip,
                IsEnabled = field.IsEnabled
            };
            
            // Bind IsEnabled to field's IsEnabled property
            var enabledBinding = new Binding("IsEnabled")
            {
                Source = field,
                Mode = BindingMode.OneWay
            };
            passwordBox.SetBinding(PasswordBox.IsEnabledProperty, enabledBinding);
            
            return passwordBox;
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
                SelectedValuePath = "Value",
                IsEnabled = field.IsEnabled
            };

            // Bind IsEnabled to field's IsEnabled property
            var enabledBinding = new Binding("IsEnabled")
            {
                Source = field,
                Mode = BindingMode.OneWay
            };
            comboBox.SetBinding(ComboBox.IsEnabledProperty, enabledBinding);

            if (field.Options != null)
            {
                comboBox.ItemsSource = field.Options;
                
                // Pre-select the default value if it exists
                if (field.Value != null)
                {
                    var matchingOption = field.Options.FirstOrDefault(o => 
                        o.Value?.ToString() == field.Value.ToString());
                    if (matchingOption != null)
                    {
                        comboBox.SelectedItem = matchingOption;
                    }
                }
            }

            return comboBox;
        }

        private CheckBox CreateCheckBox(SettingsField field)
        {
            var checkBox = new CheckBox
            {
                Style = (Style)FindResource("ModernCheckBoxStyle"),
                Content = field.CheckboxText ?? "",
                ToolTip = field.Tooltip,
                IsEnabled = field.IsEnabled
            };
            
            // Bind IsEnabled to field's IsEnabled property
            var enabledBinding = new Binding("IsEnabled")
            {
                Source = field,
                Mode = BindingMode.OneWay
            };
            checkBox.SetBinding(CheckBox.IsEnabledProperty, enabledBinding);
            
            // Set initial checked state
            if (field.Value != null)
            {
                if (bool.TryParse(field.Value.ToString(), out bool isChecked))
                {
                    checkBox.IsChecked = isChecked;
                }
                else if (field.Value is bool boolValue)
                {
                    checkBox.IsChecked = boolValue;
                }
            }
            
            return checkBox;
        }

        private StackPanel CreateFileSelector(SettingsField field)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            
            var textBox = new TextBox
            {
                Style = (Style)FindResource("ModernTextBoxStyle"),
                IsReadOnly = true,
                MinWidth = 200,
                ToolTip = field.Tooltip,
                IsEnabled = field.IsEnabled
            };
            
            var button = new Button
            {
                Content = "📁 Browse...",
                Style = (Style)FindResource("ModernButtonStyle"),
                Margin = new Thickness(8, 0, 0, 0),
                IsEnabled = field.IsEnabled
            };
            
            // Bind IsEnabled to field's IsEnabled property for both controls
            var enabledBinding = new Binding("IsEnabled")
            {
                Source = field,
                Mode = BindingMode.OneWay
            };
            textBox.SetBinding(TextBox.IsEnabledProperty, enabledBinding);
            button.SetBinding(Button.IsEnabledProperty, enabledBinding);
            
            // Set initial file path if exists
            if (field.Value != null && !string.IsNullOrEmpty(field.Value.ToString()))
            {
                textBox.Text = field.Value.ToString();
            }
            
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
            bool isPlaceholderShown = false;
            
            // Check if textBox already has a real value
            bool hasRealValue = !string.IsNullOrEmpty(textBox.Text) && textBox.Text != placeholder;
            
            textBox.GotFocus += (s, e) =>
            {
                if (isPlaceholderShown && textBox.Text == placeholder)
                {
                    textBox.Text = "";
                    textBox.Foreground = System.Windows.Media.Brushes.Black;
                    isPlaceholderShown = false;
                }
            };
            
            textBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = placeholder;
                    textBox.Foreground = System.Windows.Media.Brushes.Gray;
                    isPlaceholderShown = true;
                }
                else
                {
                    textBox.Foreground = System.Windows.Media.Brushes.Black;
                    isPlaceholderShown = false;
                }
            };
            
            // Set initial placeholder only if no real value exists
            if (!hasRealValue && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = placeholder;
                textBox.Foreground = System.Windows.Media.Brushes.Gray;
                isPlaceholderShown = true;
            }
            else if (hasRealValue)
            {
                textBox.Foreground = System.Windows.Media.Brushes.Black;
                isPlaceholderShown = false;
            }
        }

        private static bool IsTextAllowed(string text)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(text, @"^[0-9.-]+$");
        }
    }
}