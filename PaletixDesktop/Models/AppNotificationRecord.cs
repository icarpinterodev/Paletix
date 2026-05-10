using System;
using PaletixDesktop.ViewModels;

namespace PaletixDesktop.Models
{
    public enum AppNotificationKind
    {
        Info,
        Success,
        Warning,
        Error
    }

    public sealed class AppNotificationRecord : ViewModelBase
    {
        private bool _isRead;

        public string Title { get; init; } = "";
        public string Message { get; init; } = "";
        public AppNotificationKind Kind { get; init; }
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;

        public bool IsRead
        {
            get => _isRead;
            set => SetProperty(ref _isRead, value);
        }

        public string TimeText => CreatedAt.ToLocalTime().ToString("HH:mm");
    }
}
