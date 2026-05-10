using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using PaletixDesktop.Models;
using PaletixDesktop.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace PaletixDesktop.Services
{
    public sealed class NotificationService : ViewModelBase
    {
        public ObservableCollection<AppNotificationRecord> Items { get; } = new();

        public int UnreadCount => Items.Count(item => !item.IsRead);
        public bool HasUnread => UnreadCount > 0;
        public bool HasNotifications => Items.Count > 0;
        public string UnreadCountText => UnreadCount == 0 ? "" : UnreadCount.ToString();

        public void Notify(
            string title,
            string message,
            AppNotificationKind kind = AppNotificationKind.Info,
            bool sendWindowsNotification = true)
        {
            var item = new AppNotificationRecord
            {
                Title = title,
                Message = message,
                Kind = kind
            };

            Items.Insert(0, item);
            while (Items.Count > 30)
            {
                Items.RemoveAt(Items.Count - 1);
            }

            OnPropertyChanged(nameof(UnreadCount));
            OnPropertyChanged(nameof(HasUnread));
            OnPropertyChanged(nameof(HasNotifications));
            OnPropertyChanged(nameof(UnreadCountText));

            if (sendWindowsNotification)
            {
                TryShowWindowsNotification(title, message);
            }
        }

        public void MarkAllRead()
        {
            foreach (var item in Items)
            {
                item.IsRead = true;
            }

            OnPropertyChanged(nameof(UnreadCount));
            OnPropertyChanged(nameof(HasUnread));
            OnPropertyChanged(nameof(UnreadCountText));
        }

        private static void TryShowWindowsNotification(string title, string message)
        {
            try
            {
                var notification = new AppNotificationBuilder()
                    .AddText(title)
                    .AddText(message)
                    .BuildNotification();

                AppNotificationManager.Default.Show(notification);
            }
            catch (Exception)
            {
                // Some unpackaged/dev launches cannot display Windows toast notifications.
            }
        }
    }
}
