using Microsoft.UI.Xaml;
using PaletixDesktop.Models;
using PaletixDesktop.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PaletixDesktop.ViewModels
{
    public sealed class ShellLayoutViewModel : ViewModelBase
    {
        private readonly AppServices _services;
        private ShellCategory? _activeCategory;
        private ShellSection? _activeSection;
        private bool _isCompact;
        private string _lastCommandText = "Preparat";

        public ShellLayoutViewModel(AppServices services)
        {
            _services = services;
            Shell = services.ShellViewModel;
            Notifications = services.NotificationService;
            ImportJob = services.ImportJobService;
            PendingSync = services.PendingSyncService;
            _services.NavigationService.NavigationRequested += (_, route) => SelectSectionByRoute(route);
        }

        public ShellViewModel Shell { get; }
        public NotificationService Notifications { get; }
        public ImportJobService ImportJob { get; }
        public PendingSyncService PendingSync { get; }
        public ObservableCollection<ShellCategory> Categories { get; } = new();
        public ObservableCollection<ShellSection> ActiveSections { get; } = new();
        public ObservableCollection<ShellCommand> ActiveCommands { get; } = new();

        public ShellCategory? ActiveCategory
        {
            get => _activeCategory;
            private set
            {
                if (SetProperty(ref _activeCategory, value))
                {
                    OnPropertyChanged(nameof(ActiveCategoryTitle));
                    OnPropertyChanged(nameof(ActiveCategorySubtitle));
                }
            }
        }

        public ShellSection? ActiveSection
        {
            get => _activeSection;
            private set
            {
                if (SetProperty(ref _activeSection, value))
                {
                    OnPropertyChanged(nameof(ActiveSectionTitle));
                    OnPropertyChanged(nameof(ActiveSectionSubtitle));
                }
            }
        }

        public bool IsCompact
        {
            get => _isCompact;
            set
            {
                if (SetProperty(ref _isCompact, value))
                {
                    OnPropertyChanged(nameof(SidePanelWidth));
                }
            }
        }

        public GridLength SidePanelWidth => IsCompact ? new GridLength(88) : new GridLength(280);
        public string ActiveCategoryTitle => ActiveCategory?.Title ?? "";
        public string ActiveCategorySubtitle => ActiveCategory is null ? "" : $"{ActiveCategory.Sections.Count} apartat(s) disponibles";
        public string ActiveSectionTitle => ActiveSection?.Title ?? "";
        public string ActiveSectionSubtitle => ActiveSection?.Subtitle ?? "";

        public string LastCommandText
        {
            get => _lastCommandText;
            private set => SetProperty(ref _lastCommandText, value);
        }

        public async Task InitializeAsync()
        {
            if (Categories.Count > 0)
            {
                return;
            }

            await Shell.InitializeAsync();

            foreach (var category in _services.ShellNavigationCatalog.GetVisibleCategories(_services.PermissionService))
            {
                Categories.Add(category);
            }

            SelectCategory(Categories.FirstOrDefault());
        }

        public void SelectCategory(ShellCategory? category)
        {
            if (category is null || ReferenceEquals(category, ActiveCategory))
            {
                return;
            }

            ActiveCategory = category;
            foreach (var item in Categories)
            {
                item.IsSelected = ReferenceEquals(item, category);
            }

            ActiveSections.Clear();
            foreach (var section in category.Sections)
            {
                ActiveSections.Add(section);
            }

            SelectSection(category.Sections.FirstOrDefault());
        }

        public void SelectSection(ShellSection? section)
        {
            if (section is null)
            {
                return;
            }

            foreach (var item in ActiveSections)
            {
                item.IsSelected = ReferenceEquals(item, section);
            }

            ActiveCommands.Clear();
            foreach (var command in section.Commands.Where(command => _services.PermissionService.CanAccess(section.Feature, command.Action)))
            {
                ActiveCommands.Add(command);
            }

            ActiveSection = section;
            OnPropertyChanged(nameof(ActiveCommands));
        }

        public void ExecuteShellCommand(ShellCommand command)
        {
            LastCommandText = $"{command.Label} · {ActiveSectionTitle}";
        }

        private void SelectSectionByRoute(string route)
        {
            var match = Categories
                .SelectMany(category => category.Sections.Select(section => new { category, section }))
                .FirstOrDefault(item => item.section.Route == route);

            if (match is null)
            {
                return;
            }

            SelectCategory(match.category);
            SelectSection(match.section);
        }
    }
}
