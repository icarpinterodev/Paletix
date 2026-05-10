using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PaletixDesktop.Models
{
    public sealed class ShellCategory : INotifyPropertyChanged
    {
        private bool _isSelected;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Id { get; init; } = "";
        public string Title { get; init; } = "";
        public string Glyph { get; init; } = "";
        public IReadOnlyList<ShellSection> Sections { get; init; } = new List<ShellSection>();

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;
                OnPropertyChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
