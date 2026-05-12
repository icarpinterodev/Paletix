using PaletixDesktop.Models;
using PaletixDesktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace PaletixDesktop.ViewModels
{
    public enum PickingSessionState
    {
        Idle,
        Active,
        Paused
    }

    public sealed class PickingViewModel : ViewModelBase
    {
        private readonly PickingDataService _pickingDataService;
        private readonly ShellViewModel _shell;
        private readonly NotificationService _notifications;
        private readonly List<StockRecord> _stock = new();
        private string _searchText = "";
        private string _statusText = "Carregant preparacio...";
        private string _stateConfigurationText = "";
        private string _incidentReason = "";
        private bool _hasRequiredStates = true;
        private PickingSessionState _sessionState = PickingSessionState.Idle;

        public PickingViewModel()
            : this(
                App.CurrentServices.PickingDataService,
                App.CurrentServices.ShellViewModel,
                App.CurrentServices.NotificationService)
        {
        }

        public PickingViewModel(
            PickingDataService pickingDataService,
            ShellViewModel shell,
            NotificationService notifications)
        {
            _pickingDataService = pickingDataService;
            _shell = shell;
            _notifications = notifications;
        }

        public ObservableCollection<PickingOrderRecord> Orders { get; } = new();
        public ObservableCollection<PickingLineRecord> Lines { get; } = new();
        public ObservableCollection<PickingLineRecord> FilteredLines { get; } = new();
        public ObservableCollection<PickingLineRecord> SelectedLines { get; } = new();

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    RefreshFilter();
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public string StateConfigurationText
        {
            get => _stateConfigurationText;
            private set => SetProperty(ref _stateConfigurationText, value);
        }

        public string IncidentReason
        {
            get => _incidentReason;
            set
            {
                if (SetProperty(ref _incidentReason, value))
                {
                    RaiseActionState();
                }
            }
        }

        public bool HasRequiredStates
        {
            get => _hasRequiredStates;
            private set
            {
                if (SetProperty(ref _hasRequiredStates, value))
                {
                    RaiseActionState();
                }
            }
        }

        public PickingSessionState SessionState
        {
            get => _sessionState;
            private set
            {
                if (SetProperty(ref _sessionState, value))
                {
                    OnPropertyChanged(nameof(IsSessionActive));
                    OnPropertyChanged(nameof(IsSessionPaused));
                    OnPropertyChanged(nameof(SessionStateText));
                    RaiseActionState();
                }
            }
        }

        public bool IsSessionActive => SessionState == PickingSessionState.Active;
        public bool IsSessionPaused => SessionState == PickingSessionState.Paused;
        public string SessionStateText => SessionState switch
        {
            PickingSessionState.Active => "Picking en curs",
            PickingSessionState.Paused => "Picking pausat",
            _ => "Preparacio pendent"
        };

        public int TotalOrders => Orders.Count;
        public int TotalLines => Lines.Count;
        public int ReadyLines => Lines.Count(line => line.State is PickingLineState.Ready or PickingLineState.Prepared);
        public int AttentionLines => Lines.Count(line => line.NeedsAttention);
        public int PendingLines => Lines.Count(line => line.IsPending);
        public int SelectedCount => SelectedLines.Count;
        public string SelectedCountText => $"{SelectedCount} seleccionat(s)";
        public PickingLineRecord? SelectedLine => SelectedLines.Count == 1 ? SelectedLines[0] : null;
        public bool IsDetailPanelOpen => SelectedLine is not null;
        public bool HasSelection => SelectedLines.Count > 0;
        public bool CanStart => !IsBusy && Lines.Count > 0 && SessionState != PickingSessionState.Active;
        public bool CanPause => !IsBusy && SessionState != PickingSessionState.Idle;
        public bool CanMarkPrepared => !IsBusy && IsSessionActive && HasRequiredStates && SelectedLines.Any(CanPrepareLine);
        public bool CanRegisterIncident => !IsBusy && IsSessionActive && HasRequiredStates && SelectedLines.Count > 0 && !string.IsNullOrWhiteSpace(IncidentReason);
        public bool CanReleaseReservation => !IsBusy && IsSessionActive && SelectedLines.Any(line => line.HasReservation && line.State is not PickingLineState.Prepared);
        public string DetailActionMessage => BuildDetailActionMessage();

        public async Task LoadAsync()
        {
            IsBusy = true;
            ErrorMessage = null;
            RaiseActionState();

            try
            {
                var result = await _pickingDataService.LoadAsync();
                ApplyLoadResult(result);
                StatusText = result.HasRequiredStates
                    ? result.Message
                    : $"{result.Message} {result.StateMessage}";
                await _shell.RefreshSyncStatusAsync(result.IsOnline, result.IsOnline ? "Sincronitzat" : "Mode offline");
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                StatusText = $"No s'ha pogut carregar Picking: {ex.Message}";
                await _shell.RefreshSyncStatusAsync(false, "Mode offline");
            }
            finally
            {
                IsBusy = false;
                RaiseActionState();
            }
        }

        public void SetSelection(IEnumerable<PickingLineRecord> lines)
        {
            foreach (var line in Lines)
            {
                line.IsSelected = false;
            }

            SelectedLines.Clear();
            foreach (var line in lines.Distinct())
            {
                line.IsSelected = true;
                SelectedLines.Add(line);
            }

            IncidentReason = "";
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(SelectedCountText));
            OnPropertyChanged(nameof(SelectedLine));
            OnPropertyChanged(nameof(IsDetailPanelOpen));
            OnPropertyChanged(nameof(HasSelection));
            RaiseActionState();
        }

        public async Task StartPickingAsync()
        {
            if (Lines.Count == 0)
            {
                StatusText = "No hi ha linies de picking carregades.";
                return;
            }

            IsBusy = true;
            RaiseActionState();
            try
            {
                var result = await _pickingDataService.ReservePreparablesAsync(Lines.ToList(), _stock);
                ApplyOperationResult(result);
                await _shell.RefreshSyncStatusAsync(message: result.PendingCount == 0 ? "Sincronitzat" : "Canvis pendents");
                SessionState = PickingSessionState.Active;
                StatusText = $"{result.Message} Selecciona linies per preparar-les o registrar incidencies.";
                _notifications.Notify("Picking iniciat", StatusText, AppNotificationKind.Info, sendWindowsNotification: false);
            }
            catch (Exception ex)
            {
                StatusText = $"No s'ha pogut iniciar el picking: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                RaiseActionState();
            }
        }

        public void PausePicking()
        {
            if (SessionState == PickingSessionState.Active)
            {
                SessionState = PickingSessionState.Paused;
                StatusText = "Picking pausat. Pots refrescar i revisar, pero no marcar linies com preparades.";
            }
            else if (SessionState == PickingSessionState.Paused)
            {
                SessionState = PickingSessionState.Active;
                StatusText = "Picking reprès.";
            }
            else
            {
                StatusText = "Inicia el picking abans de pausar-lo.";
            }
        }

        public async Task MarkPreparedAsync()
        {
            if (!EnsureActiveSession())
            {
                return;
            }

            if (SelectedLines.Count == 0)
            {
                StatusText = "Selecciona una o mes linies per marcar-les com preparades.";
                return;
            }

            if (!HasRequiredStates)
            {
                StatusText = StateConfigurationText;
                return;
            }

            IsBusy = true;
            RaiseActionState();
            try
            {
                var result = await _pickingDataService.MarkPreparedAsync(SelectedLines.ToList(), Lines.ToList(), _stock);
                ApplyOperationResult(result);
                await _shell.RefreshSyncStatusAsync(message: result.PendingCount == 0 ? "Sincronitzat" : "Canvis pendents");
                StatusText = result.Message;
            }
            catch (Exception ex)
            {
                StatusText = $"No s'ha pogut marcar com preparat: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                RaiseActionState();
            }
        }

        public async Task RegisterIncidentAsync()
        {
            if (!EnsureActiveSession())
            {
                return;
            }

            if (SelectedLines.Count == 0)
            {
                StatusText = "Selecciona una linia per registrar incidencia.";
                return;
            }

            if (string.IsNullOrWhiteSpace(IncidentReason))
            {
                StatusText = "Escriu el motiu de la incidencia al panell lateral.";
                RaiseActionState();
                return;
            }

            if (!HasRequiredStates)
            {
                StatusText = StateConfigurationText;
                return;
            }

            IsBusy = true;
            RaiseActionState();
            try
            {
                var result = await _pickingDataService.RegisterIncidentAsync(SelectedLines.ToList(), Lines.ToList(), _stock, IncidentReason);
                ApplyOperationResult(result);
                await _shell.RefreshSyncStatusAsync(message: result.PendingCount == 0 ? "Sincronitzat" : "Canvis pendents");
                IncidentReason = "";
                StatusText = result.Message;
                _notifications.Notify("Incidencia de picking", StatusText, AppNotificationKind.Warning, sendWindowsNotification: false);
            }
            catch (Exception ex)
            {
                StatusText = $"No s'ha pogut registrar la incidencia: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                RaiseActionState();
            }
        }

        public async Task ReleaseReservationAsync()
        {
            if (!EnsureActiveSession())
            {
                return;
            }

            if (SelectedLines.All(line => !line.HasReservation))
            {
                StatusText = "La seleccio no te cap reserva per alliberar.";
                return;
            }

            IsBusy = true;
            RaiseActionState();
            try
            {
                var result = await _pickingDataService.ReleaseReservationsAsync(SelectedLines.ToList(), Lines.ToList(), _stock);
                ApplyOperationResult(result);
                await _shell.RefreshSyncStatusAsync(message: result.PendingCount == 0 ? "Sincronitzat" : "Canvis pendents");
                StatusText = result.Message;
            }
            catch (Exception ex)
            {
                StatusText = $"No s'ha pogut alliberar la reserva: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                RaiseActionState();
            }
        }

        private void ApplyLoadResult(PickingLoadResult result)
        {
            Orders.Clear();
            foreach (var order in result.Orders)
            {
                Orders.Add(order);
            }

            Lines.Clear();
            foreach (var line in result.Lines)
            {
                Lines.Add(line);
            }

            _stock.Clear();
            _stock.AddRange(result.Stock);
            HasRequiredStates = result.HasRequiredStates;
            StateConfigurationText = result.StateMessage;
            SetSelection(Array.Empty<PickingLineRecord>());
            RefreshFilter();
            RaiseMetrics();
        }

        private void ApplyOperationResult(PickingOperationResult result)
        {
            _stock.Clear();
            _stock.AddRange(result.Stock);
            RefreshFilter();
            RaiseMetrics();
        }

        private void RefreshFilter()
        {
            var query = SearchText.Trim().ToLowerInvariant();
            var lines = string.IsNullOrWhiteSpace(query)
                ? Lines
                : Lines.Where(line =>
                    line.OrderId.ToString(CultureInfo.InvariantCulture).Contains(query)
                    || line.ProductText.ToLowerInvariant().Contains(query)
                    || line.LocationText.ToLowerInvariant().Contains(query)
                    || line.StateText.ToLowerInvariant().Contains(query)
                    || line.SyncStateText.ToLowerInvariant().Contains(query));

            var selected = SelectedLines.ToList();
            FilteredLines.Clear();
            foreach (var line in lines)
            {
                FilteredLines.Add(line);
            }

            SetSelection(selected.Where(FilteredLines.Contains).ToList());
            RaiseMetrics();
        }

        private bool EnsureActiveSession()
        {
            if (SessionState == PickingSessionState.Active)
            {
                return true;
            }

            StatusText = SessionState == PickingSessionState.Paused
                ? "La sessio esta pausada. Repren el picking abans d'operar."
                : "Inicia el picking abans d'operar.";
            return false;
        }

        private static bool CanPrepareLine(PickingLineRecord line)
        {
            if (line.State is PickingLineState.Prepared or PickingLineState.Incident)
            {
                return false;
            }

            return line.ReservedBoxes >= line.RequestedBoxes
                || line.AvailableBoxes >= line.RequestedBoxes;
        }

        private string BuildDetailActionMessage()
        {
            if (SelectedLine is null)
            {
                return "Selecciona una linia per veure el detall operatiu.";
            }

            if (IsSessionPaused)
            {
                return "Sessio pausada: pots revisar, pero no operar.";
            }

            if (!IsSessionActive)
            {
                return "Inicia el picking per operar aquesta linia.";
            }

            if (!HasRequiredStates)
            {
                return StateConfigurationText;
            }

            if (SelectedLine.State is PickingLineState.Missing)
            {
                return "No hi ha stock suficient per marcar-la com preparada.";
            }

            if (SelectedLine.State is PickingLineState.Partial)
            {
                return "Stock parcial: registra incidencia o revisa stock abans de preparar.";
            }

            return "Linia preparada per operar.";
        }

        private void RaiseMetrics()
        {
            OnPropertyChanged(nameof(TotalOrders));
            OnPropertyChanged(nameof(TotalLines));
            OnPropertyChanged(nameof(ReadyLines));
            OnPropertyChanged(nameof(AttentionLines));
            OnPropertyChanged(nameof(PendingLines));
            RaiseActionState();
        }

        private void RaiseActionState()
        {
            OnPropertyChanged(nameof(CanStart));
            OnPropertyChanged(nameof(CanPause));
            OnPropertyChanged(nameof(CanMarkPrepared));
            OnPropertyChanged(nameof(CanRegisterIncident));
            OnPropertyChanged(nameof(CanReleaseReservation));
            OnPropertyChanged(nameof(DetailActionMessage));
        }
    }
}
