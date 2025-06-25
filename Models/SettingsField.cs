using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WeighbridgeSoftwareYashCotex.Models
{
    public enum FieldType
    {
        Text,
        Password,
        Number,
        Dropdown,
        Checkbox,
        File,
        Color,
        Date,
        Time
    }

    public class SettingsField : INotifyPropertyChanged
    {
        private object? _value;
        private bool _isEnabled = true;
        private bool _isVisible = true;

        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        public string? Description { get; set; }
        public string? Tooltip { get; set; }
        public string? Placeholder { get; set; }
        public FieldType FieldType { get; set; } = FieldType.Text;
        public bool IsRequired { get; set; }
        public string? FileFilter { get; set; }
        public string? CheckboxText { get; set; }
        public List<SettingsOption>? Options { get; set; }
        public string? ValidationPattern { get; set; }
        public string? ValidationMessage { get; set; }
        public object? DefaultValue { get; set; }

        public object? Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged();
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public event EventHandler? ValueChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool Validate()
        {
            if (IsRequired && (Value == null || string.IsNullOrWhiteSpace(Value.ToString())))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(ValidationPattern) && Value != null)
            {
                var pattern = new System.Text.RegularExpressions.Regex(ValidationPattern);
                return pattern.IsMatch(Value.ToString() ?? "");
            }

            return true;
        }

        public void Reset()
        {
            Value = DefaultValue;
        }
    }

    public class SettingsOption
    {
        public string Text { get; set; } = "";
        public object Value { get; set; } = "";
        public bool IsEnabled { get; set; } = true;
    }

    public class SettingsGroup : INotifyPropertyChanged
    {
        private int _columnCount = 1;

        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public List<SettingsField> Fields { get; set; } = new();
        public bool IsExpanded { get; set; } = true;
        public bool IsVisible { get; set; } = true;

        public int ColumnCount
        {
            get => _columnCount;
            set
            {
                if (_columnCount != value)
                {
                    _columnCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool ValidateAll()
        {
            return Fields.All(f => f.Validate());
        }

        public void ResetAll()
        {
            foreach (var field in Fields)
            {
                field.Reset();
            }
        }

        public Dictionary<string, object?> GetValues()
        {
            return Fields.ToDictionary(f => f.Key, f => f.Value);
        }

        public void SetValues(Dictionary<string, object?> values)
        {
            foreach (var field in Fields)
            {
                if (values.TryGetValue(field.Key, out var value))
                {
                    field.Value = value;
                }
            }
        }
    }
}