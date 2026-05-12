using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace PaletixDesktop.Models
{
    public enum PickingLineState
    {
        Ready,
        Partial,
        Missing,
        Prepared,
        Incident
    }

    public enum PickingLineSyncState
    {
        Synced,
        Pending,
        Error
    }

    public sealed class PickingOrderRecord
    {
        public int Id { get; init; }
        public int IdClient { get; init; }
        public string Estat { get; init; } = "";
        public DateOnly DeliveryDate { get; init; }
        public int LineCount { get; init; }
        public int MissingCount { get; init; }
        public string DeliveryDateText => DeliveryDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        public string Summary => $"Comanda {Id} · Client {IdClient}";
        public string Detail => $"{LineCount} linia(es) · {MissingCount} amb falta";
    }

    public sealed class PickingLineRecord : INotifyPropertyChanged
    {
        private bool _isSelected;
        private PickingLineState _state;
        private PickingLineSyncState _syncState;
        private string _syncMessage = "Sincronitzat";
        private int _availableBoxes;
        private int _reservedBoxes;
        private int? _reservedStockId;
        private int? _verificationStateId;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int OrderId { get; init; }
        public int LineId { get; init; }
        public int ProductId { get; init; }
        public int LocationId { get; init; }
        public int RequestedBoxes { get; init; }
        public int? Pallets { get; init; }
        public string ProductText { get; init; } = "";
        public string LocationText { get; init; } = "";
        public string OrderState { get; init; } = "";

        public int AvailableBoxes
        {
            get => _availableBoxes;
            set
            {
                if (SetProperty(ref _availableBoxes, value))
                {
                    OnPropertyChanged(nameof(AvailableText));
                    OnPropertyChanged(nameof(MissingBoxes));
                    OnPropertyChanged(nameof(MissingText));
                }
            }
        }

        public int ReservedBoxes
        {
            get => _reservedBoxes;
            set
            {
                if (SetProperty(ref _reservedBoxes, value))
                {
                    OnPropertyChanged(nameof(ReservedText));
                    OnPropertyChanged(nameof(MissingBoxes));
                    OnPropertyChanged(nameof(MissingText));
                    OnPropertyChanged(nameof(HasReservation));
                }
            }
        }

        public int? ReservedStockId
        {
            get => _reservedStockId;
            set
            {
                if (SetProperty(ref _reservedStockId, value))
                {
                    OnPropertyChanged(nameof(HasReservation));
                }
            }
        }

        public int? VerificationStateId
        {
            get => _verificationStateId;
            set => SetProperty(ref _verificationStateId, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public PickingLineState State
        {
            get => _state;
            set
            {
                if (SetProperty(ref _state, value))
                {
                    OnPropertyChanged(nameof(StateText));
                    OnPropertyChanged(nameof(NeedsAttention));
                }
            }
        }

        public PickingLineSyncState SyncState
        {
            get => _syncState;
            set
            {
                if (SetProperty(ref _syncState, value))
                {
                    OnPropertyChanged(nameof(SyncStateText));
                    OnPropertyChanged(nameof(IsPending));
                }
            }
        }

        public string SyncMessage
        {
            get => _syncMessage;
            set => SetProperty(ref _syncMessage, value);
        }

        public bool HasReservation => ReservedBoxes > 0 && ReservedStockId.HasValue;
        public bool IsPending => SyncState is PickingLineSyncState.Pending or PickingLineSyncState.Error;
        public bool NeedsAttention => State is PickingLineState.Partial or PickingLineState.Missing or PickingLineState.Incident;
        public int MissingBoxes => Math.Max(0, RequestedBoxes - Math.Max(AvailableBoxes, ReservedBoxes));
        public string PalletsText => Pallets?.ToString(CultureInfo.InvariantCulture) ?? "-";
        public string AvailableText => AvailableBoxes.ToString(CultureInfo.InvariantCulture);
        public string ReservedText => ReservedBoxes == 0 ? "-" : ReservedBoxes.ToString(CultureInfo.InvariantCulture);
        public string RequestedText => RequestedBoxes.ToString(CultureInfo.InvariantCulture);
        public string MissingText => MissingBoxes == 0 ? "-" : MissingBoxes.ToString(CultureInfo.InvariantCulture);
        public string StateText => State switch
        {
            PickingLineState.Ready => "Preparat per picking",
            PickingLineState.Partial => "Stock parcial",
            PickingLineState.Missing => "Sense stock",
            PickingLineState.Prepared => "Preparat",
            PickingLineState.Incident => "Incidencia",
            _ => "Pendent"
        };
        public string SyncStateText => SyncState switch
        {
            PickingLineSyncState.Pending => "Pendent",
            PickingLineSyncState.Error => "Error sync",
            _ => "Sincronitzat"
        };

        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
