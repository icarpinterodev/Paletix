using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PaletixDesktop.Models
{
    public enum OrderSyncState
    {
        Synced,
        Pending,
        PendingDelete,
        Error
    }

    public sealed class OrderRecord : INotifyPropertyChanged
    {
        private bool _isSelected;
        private OrderSyncState _syncState;
        private string _syncMessage = "Sincronitzat";

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id { get; set; }
        public int IdClient { get; set; }
        public int IdChofer { get; set; }
        public int IdPreparador { get; set; }
        public int IdVehicleTransportista { get; set; }
        public int IdEstat { get; set; }
        public DateOnly? DataCreacio { get; set; }
        public string? Notes { get; set; }
        public DateOnly DataPrevistaEntrega { get; set; }
        public DateOnly? DataEntregat { get; set; }
        public string? PoblacioEntregaAlternativa { get; set; }
        public string? AdrecaEntregaAlternativa { get; set; }
        public string? Estat { get; set; }
        public string ClientText { get; set; } = "";
        public string ChoferText { get; set; } = "";
        public string PreparadorText { get; set; } = "";
        public string VehicleText { get; set; } = "";
        public string EstatText { get; set; } = "";
        public List<OrderLineRecord> Lines { get; set; } = new();

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public OrderSyncState SyncState
        {
            get => _syncState;
            set
            {
                if (SetProperty(ref _syncState, value))
                {
                    OnPropertyChanged(nameof(SyncStateText));
                    OnPropertyChanged(nameof(IsPending));
                    OnPropertyChanged(nameof(IsPendingDelete));
                }
            }
        }

        public string SyncMessage
        {
            get => _syncMessage;
            set => SetProperty(ref _syncMessage, value);
        }

        public bool IsPending => SyncState is OrderSyncState.Pending or OrderSyncState.Error or OrderSyncState.PendingDelete;
        public bool IsPendingDelete => SyncState == OrderSyncState.PendingDelete;
        public string IdText => Id.ToString(CultureInfo.InvariantCulture);
        public string DeliveryText => DataPrevistaEntrega.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        public string DeliveredText => DataEntregat?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "-";
        public string LinesText => Lines.Count.ToString(CultureInfo.InvariantCulture);
        public string BoxesText => Lines.Sum(line => line.Caixes).ToString(CultureInfo.InvariantCulture);
        public string PalletsText => Lines.Sum(line => line.Palets ?? 0).ToString(CultureInfo.InvariantCulture);
        public string SyncStateText => SyncState switch
        {
            OrderSyncState.Pending => "Pendent",
            OrderSyncState.PendingDelete => "Eliminacio pendent",
            OrderSyncState.Error => "Error sync",
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

    public sealed class OrderLineRecord : INotifyPropertyChanged
    {
        private int? _idUbicacio;
        private int? _palets;
        private int _caixes;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id { get; set; }
        public int IdProducte { get; set; }
        public string ProducteText { get; set; } = "";
        public string? Ubicacio { get; set; }
        public int? IdEstatVerificacio { get; set; }

        public int? IdUbicacio
        {
            get => _idUbicacio;
            set => SetProperty(ref _idUbicacio, value);
        }

        public int? Palets
        {
            get => _palets;
            set
            {
                if (SetProperty(ref _palets, value))
                {
                    OnPropertyChanged(nameof(PaletsValue));
                }
            }
        }

        public int Caixes
        {
            get => _caixes;
            set
            {
                if (SetProperty(ref _caixes, value))
                {
                    OnPropertyChanged(nameof(CaixesValue));
                }
            }
        }

        public double PaletsValue
        {
            get => Palets ?? 0;
            set => Palets = double.IsNaN(value) ? null : Math.Max(0, (int)Math.Round(value));
        }

        public double CaixesValue
        {
            get => Caixes;
            set => Caixes = double.IsNaN(value) ? 0 : Math.Max(0, (int)Math.Round(value));
        }

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

    public sealed class OrderProductPickerItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private double _paletsValue;
        private double _caixesValue = 1;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id { get; set; }
        public string Label { get; set; } = "";
        public string? ImageUrl { get; set; }
        public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public double PaletsValue
        {
            get => _paletsValue;
            set => SetProperty(ref _paletsValue, Math.Max(0, value));
        }

        public double CaixesValue
        {
            get => _caixesValue;
            set => SetProperty(ref _caixesValue, Math.Max(1, value));
        }

        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
