using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PaletixDesktop.Models;
using PaletixDesktop.ViewModels;
using PaletixDesktop.Views.Shell;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PaletixDesktop.Views.Pages
{
    public sealed partial class AdministrationUsersView : UserControl, IShellCommandHandler
    {
        private bool _loaded;
        private bool _syncingSelection;

        public AdministrationUsersView()
        {
            ViewModel = new AdminIdentityViewModel();
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        public AdminIdentityViewModel ViewModel { get; }

        public bool CanHandleShellCommand(string commandId) => commandId.StartsWith("admin-users.");

        public void HandleShellCommand(string commandId)
        {
            _ = HandleShellCommandAsync(commandId);
        }

        public async Task RefreshAsync()
        {
            await ViewModel.LoadAsync();
            SyncSelectionToControls();
        }

        private async Task HandleShellCommandAsync(string commandId)
        {
            switch (commandId)
            {
                case "admin-users.refresh":
                    await RefreshAsync();
                    break;
                case "admin-users.create":
                    ViewModel.OpenCreate();
                    break;
                case "admin-users.edit":
                    ViewModel.OpenEditSelected();
                    break;
                case "admin-users.delete":
                    if (await ConfirmDeleteAsync())
                    {
                        await ViewModel.DeleteSelectedAsync();
                    }
                    break;
            }

            SyncSelectionToControls();
        }

        private async void AdministrationUsersView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            await RefreshAsync();
        }

        private void AdminSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            if (sender.SelectedItem == UsersSelectorItem)
            {
                ViewModel.SetActiveTab(AdminIdentityTab.Users);
            }
            else if (sender.SelectedItem == RolesSelectorItem)
            {
                ViewModel.SetActiveTab(AdminIdentityTab.Roles);
            }
            else
            {
                ViewModel.SetActiveTab(AdminIdentityTab.JobTitles);
            }

            SyncSelectionToControls();
        }

        private void UsersSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncingSelection)
            {
                return;
            }

            ViewModel.SetUserSelection(UsersList.SelectedItems.OfType<AdminUserRecord>());
            SyncSelectionToControls();
        }

        private void RolesSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncingSelection)
            {
                return;
            }

            ViewModel.SetRoleSelection(RolesList.SelectedItems.OfType<AdminSimpleRecord>());
            SyncSelectionToControls();
        }

        private void JobTitlesSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncingSelection)
            {
                return;
            }

            ViewModel.SetJobTitleSelection(JobTitlesList.SelectedItems.OfType<AdminSimpleRecord>());
            SyncSelectionToControls();
        }

        private void UserCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox { Tag: AdminUserRecord user })
            {
                var selected = ViewModel.SelectedUsers.ToList();
                ToggleSelection(selected, user, user.IsSelected);
                ViewModel.SetUserSelection(selected);
                SyncSelectionToControls();
            }
        }

        private void RoleCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox { Tag: AdminSimpleRecord role })
            {
                var selected = ViewModel.SelectedRoles.ToList();
                ToggleSelection(selected, role, role.IsSelected);
                ViewModel.SetRoleSelection(selected);
                SyncSelectionToControls();
            }
        }

        private void JobTitleCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox { Tag: AdminSimpleRecord jobTitle })
            {
                var selected = ViewModel.SelectedJobTitles.ToList();
                ToggleSelection(selected, jobTitle, jobTitle.IsSelected);
                ViewModel.SetJobTitleSelection(selected);
                SyncSelectionToControls();
            }
        }

        private void UsersHeaderCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var visibleCount = ViewModel.FilteredUsers.Count;
            var selectedVisibleCount = ViewModel.SelectedUsers.Count(ViewModel.FilteredUsers.Contains);
            ViewModel.SetUserSelection(visibleCount > 0 && selectedVisibleCount == visibleCount
                ? Enumerable.Empty<AdminUserRecord>()
                : ViewModel.FilteredUsers.ToList());
            SyncSelectionToControls();
        }

        private void RolesHeaderCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var visibleCount = ViewModel.FilteredRoles.Count;
            var selectedVisibleCount = ViewModel.SelectedRoles.Count(ViewModel.FilteredRoles.Contains);
            ViewModel.SetRoleSelection(visibleCount > 0 && selectedVisibleCount == visibleCount
                ? Enumerable.Empty<AdminSimpleRecord>()
                : ViewModel.FilteredRoles.ToList());
            SyncSelectionToControls();
        }

        private void JobTitlesHeaderCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var visibleCount = ViewModel.FilteredJobTitles.Count;
            var selectedVisibleCount = ViewModel.SelectedJobTitles.Count(ViewModel.FilteredJobTitles.Contains);
            ViewModel.SetJobTitleSelection(visibleCount > 0 && selectedVisibleCount == visibleCount
                ? Enumerable.Empty<AdminSimpleRecord>()
                : ViewModel.FilteredJobTitles.ToList());
            SyncSelectionToControls();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SaveAsync();
            SyncSelectionToControls();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ClosePanel();
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(AdminIdentityViewModel.SelectedCount) or nameof(AdminIdentityViewModel.FilteredCount))
            {
                SyncSelectionToControls();
            }
        }

        private void SyncSelectionToControls()
        {
            _syncingSelection = true;
            try
            {
                UsersList.SelectedItems.Clear();
                foreach (var user in ViewModel.SelectedUsers.Where(ViewModel.FilteredUsers.Contains))
                {
                    UsersList.SelectedItems.Add(user);
                }

                RolesList.SelectedItems.Clear();
                foreach (var role in ViewModel.SelectedRoles.Where(ViewModel.FilteredRoles.Contains))
                {
                    RolesList.SelectedItems.Add(role);
                }

                JobTitlesList.SelectedItems.Clear();
                foreach (var jobTitle in ViewModel.SelectedJobTitles.Where(ViewModel.FilteredJobTitles.Contains))
                {
                    JobTitlesList.SelectedItems.Add(jobTitle);
                }

                SetHeaderCheckBox(SelectVisibleUsersCheckBox, ViewModel.FilteredUsers.Count, ViewModel.SelectedUsers.Count(ViewModel.FilteredUsers.Contains));
                SetHeaderCheckBox(SelectVisibleRolesCheckBox, ViewModel.FilteredRoles.Count, ViewModel.SelectedRoles.Count(ViewModel.FilteredRoles.Contains));
                SetHeaderCheckBox(SelectVisibleJobTitlesCheckBox, ViewModel.FilteredJobTitles.Count, ViewModel.SelectedJobTitles.Count(ViewModel.FilteredJobTitles.Contains));
            }
            finally
            {
                _syncingSelection = false;
            }
        }

        private async Task<bool> ConfirmDeleteAsync()
        {
            if (ViewModel.SelectedCount == 0)
            {
                return true;
            }

            var title = ViewModel.ActiveTab switch
            {
                AdminIdentityTab.Users => ViewModel.SelectedCount == 1 ? "Eliminar usuari" : "Eliminar usuaris",
                AdminIdentityTab.Roles => ViewModel.SelectedCount == 1 ? "Eliminar rol" : "Eliminar rols",
                _ => ViewModel.SelectedCount == 1 ? "Eliminar carrec" : "Eliminar carrecs"
            };

            var dialog = new ContentDialog
            {
                Title = title,
                Content = ViewModel.SelectedCount == 1
                    ? "Vols eliminar aquest registre?"
                    : $"Vols eliminar {ViewModel.SelectedCount} registres?",
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        private static void ToggleSelection<T>(System.Collections.Generic.List<T> selected, T item, bool isSelected)
        {
            if (isSelected)
            {
                if (!selected.Contains(item))
                {
                    selected.Add(item);
                }
            }
            else
            {
                selected.Remove(item);
            }
        }

        private static void SetHeaderCheckBox(CheckBox checkBox, int visibleCount, int selectedVisibleCount)
        {
            checkBox.IsChecked = visibleCount > 0 && selectedVisibleCount == visibleCount
                ? true
                : selectedVisibleCount == 0
                    ? false
                    : null;
        }
    }
}
