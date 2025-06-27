using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using WeighbridgeSoftwareYashCotex.Models;

namespace WeighbridgeSoftwareYashCotex.Controls
{
    public partial class SimpleFieldControl : UserControl
    {
        public static readonly DependencyProperty FieldProperty =
            DependencyProperty.Register("Field", typeof(SettingsField), typeof(SimpleFieldControl),
                new PropertyMetadata(null, OnFieldChanged));

        public SettingsField Field
        {
            get { return (SettingsField)GetValue(FieldProperty); }
            set { SetValue(FieldProperty, value); }
        }

        public SimpleFieldControl()
        {
            InitializeComponent();
        }

        private static void OnFieldChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SimpleFieldControl)d;
            var field = (SettingsField)e.NewValue;
            
            if (field != null)
            {
                Console.WriteLine($"🔧 SimpleFieldControl: Creating field '{field.Key}' with value '{field.Value}'");
                control.SetupField(field);
            }
        }

        private void SetupField(SettingsField field)
        {
            // Set label
            FieldLabel.Text = field.Label;
            if (field.IsRequired)
            {
                FieldLabel.Text += " *";
            }
            
            // Set description
            FieldDescription.Text = field.Description ?? "";
            FieldDescription.Visibility = string.IsNullOrEmpty(field.Description) ? 
                Visibility.Collapsed : Visibility.Visible;

            // Create and setup input control
            FrameworkElement inputElement = field.FieldType switch
            {
                FieldType.Text => CreateSimpleTextBox(field),
                FieldType.Password => CreateSimplePasswordBox(field),
                FieldType.Number => CreateSimpleTextBox(field),
                FieldType.Dropdown => CreateSimpleComboBox(field),
                FieldType.Checkbox => CreateSimpleCheckBox(field),
                FieldType.File => CreateSimpleFileSelector(field),
                _ => CreateSimpleTextBox(field)
            };

            InputContent.Content = inputElement;
            Console.WriteLine($"✅ SimpleFieldControl: Field '{field.Key}' created with input type {inputElement.GetType().Name}");
        }

        private TextBox CreateSimpleTextBox(SettingsField field)
        {
            var textBox = new TextBox
            {
                FontSize = 14,
                Padding = new Thickness(8),
                BorderThickness = new Thickness(0),
                Background = System.Windows.Media.Brushes.Transparent,
                IsEnabled = field.IsEnabled
            };

            // Set initial value directly
            if (field.Value != null)
            {
                textBox.Text = field.Value.ToString();
                Console.WriteLine($"   📝 TextBox value set to: '{textBox.Text}'");
            }

            // Set up two-way binding manually
            textBox.TextChanged += (s, e) =>
            {
                field.Value = textBox.Text;
                Console.WriteLine($"   📝 TextBox changed: '{textBox.Text}' -> Field.Value: '{field.Value}'");
            };

            // Update textbox when field changes
            field.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Value" && textBox.Text != field.Value?.ToString())
                {
                    textBox.Text = field.Value?.ToString() ?? "";
                }
            };

            return textBox;
        }

        private PasswordBox CreateSimplePasswordBox(SettingsField field)
        {
            var passwordBox = new PasswordBox
            {
                FontSize = 14,
                Padding = new Thickness(8),
                BorderThickness = new Thickness(0),
                Background = System.Windows.Media.Brushes.Transparent,
                IsEnabled = field.IsEnabled
            };

            // Set initial value
            if (field.Value != null)
            {
                passwordBox.Password = field.Value.ToString();
            }

            // Manual binding
            passwordBox.PasswordChanged += (s, e) => field.Value = passwordBox.Password;

            return passwordBox;
        }

        private ComboBox CreateSimpleComboBox(SettingsField field)
        {
            var comboBox = new ComboBox
            {
                FontSize = 14,
                Padding = new Thickness(8),
                BorderThickness = new Thickness(0),
                Background = System.Windows.Media.Brushes.Transparent,
                IsEnabled = field.IsEnabled,
                DisplayMemberPath = "Text",
                SelectedValuePath = "Value"
            };

            // Set items
            if (field.Options != null)
            {
                comboBox.ItemsSource = field.Options;
                
                // Set initial selection
                if (field.Value != null)
                {
                    var selectedOption = field.Options.FirstOrDefault(o => 
                        o.Value?.ToString() == field.Value.ToString());
                    if (selectedOption != null)
                    {
                        comboBox.SelectedItem = selectedOption;
                        Console.WriteLine($"   📋 ComboBox selection set to: '{selectedOption.Text}'");
                    }
                }
            }

            // Manual binding
            comboBox.SelectionChanged += (s, e) =>
            {
                if (comboBox.SelectedItem is SettingsOption option)
                {
                    field.Value = option.Value;
                    Console.WriteLine($"   📋 ComboBox changed: '{option.Text}' -> Field.Value: '{field.Value}'");
                }
            };

            return comboBox;
        }

        private CheckBox CreateSimpleCheckBox(SettingsField field)
        {
            var checkBox = new CheckBox
            {
                Content = field.CheckboxText ?? "",
                FontSize = 14,
                IsEnabled = field.IsEnabled
            };

            // Set initial value
            if (field.Value != null)
            {
                if (field.Value is bool boolValue)
                {
                    checkBox.IsChecked = boolValue;
                }
                else if (bool.TryParse(field.Value.ToString(), out bool parsedValue))
                {
                    checkBox.IsChecked = parsedValue;
                }
                Console.WriteLine($"   ☑️ CheckBox checked state set to: {checkBox.IsChecked}");
            }

            // Manual binding
            checkBox.Checked += (s, e) => 
            {
                field.Value = true;
                Console.WriteLine($"   ☑️ CheckBox checked -> Field.Value: true");
            };
            
            checkBox.Unchecked += (s, e) => 
            {
                field.Value = false;
                Console.WriteLine($"   ☑️ CheckBox unchecked -> Field.Value: false");
            };

            return checkBox;
        }

        private StackPanel CreateSimpleFileSelector(SettingsField field)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            
            var textBox = new TextBox
            {
                FontSize = 14,
                Padding = new Thickness(8),
                BorderThickness = new Thickness(0),
                Background = System.Windows.Media.Brushes.Transparent,
                IsReadOnly = true,
                MinWidth = 200,
                IsEnabled = field.IsEnabled
            };
            
            var button = new Button
            {
                Content = "Browse...",
                Margin = new Thickness(8, 0, 0, 0),
                Padding = new Thickness(12, 4, 12, 4),
                IsEnabled = field.IsEnabled
            };

            // Set initial value
            if (field.Value != null)
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
    }
}