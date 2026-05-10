using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace PaletixDesktop.Models
{
    public enum WarehouseSyncState
    {
        Synced,
        Pending,
        Error,
        PendingDelete
    }

    public interface IWarehouseRecord : INotifyPropertyChanged
    {
        int Id { get; }
        bool IsSelected { get; set; }
        WarehouseSyncState SyncState { get; set; }
        string SyncMessage { get; set; }
        string IdText { get; }
        bool IsPending { get; }
        bool IsPendingDelete { get; }
        string SyncStateText { get; }
    }

    public abstract class WarehouseRecordBase : IWarehouseRecord
    {
        private WarehouseSyncState _syncState = WarehouseSyncState.Synced;
        private string _syncMessage = "Sincronitzat";
        private bool _isSelected;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id { get; init; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public WarehouseSyncState SyncState
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

        public string IdText => Id.ToString(CultureInfo.InvariantCulture);
        public bool IsPending => SyncState is WarehouseSyncState.Pending or WarehouseSyncState.Error or WarehouseSyncState.PendingDelete;
        public bool IsPendingDelete => SyncState == WarehouseSyncState.PendingDelete;
        public string SyncStateText => SyncState switch
        {
            WarehouseSyncState.Pending => "Pendent",
            WarehouseSyncState.Error => "Error sync",
            WarehouseSyncState.PendingDelete => "Eliminacio pendent",
            _ => "Sincronitzat"
        };

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed class StockRecord : WarehouseRecordBase
    {
        private int _idProducte;
        private int _idUbicacio;
        private int? _idLot;
        private int _totalsEnStock;
        private int _reservatsPerComandes;
        private int? _disponibles;
        private string _producteText = "";
        private string _ubicacioText = "";
        private string _lotText = "Sense lot";

        public int IdProducte { get => _idProducte; set => SetProperty(ref _idProducte, value); }
        public int IdUbicacio { get => _idUbicacio; set => SetProperty(ref _idUbicacio, value); }
        public int? IdLot { get => _idLot; set => SetProperty(ref _idLot, value); }
        public int TotalsEnStock { get => _totalsEnStock; set => SetProperty(ref _totalsEnStock, value); }
        public int ReservatsPerComandes { get => _reservatsPerComandes; set => SetProperty(ref _reservatsPerComandes, value); }
        public int? Disponibles { get => _disponibles; set => SetProperty(ref _disponibles, value); }
        public string ProducteText { get => string.IsNullOrWhiteSpace(_producteText) ? $"Producte {IdProducte}" : _producteText; set => SetProperty(ref _producteText, value); }
        public string UbicacioText { get => string.IsNullOrWhiteSpace(_ubicacioText) ? $"Ubicacio {IdUbicacio}" : _ubicacioText; set => SetProperty(ref _ubicacioText, value); }
        public string LotText { get => IdLot is null ? "Sense lot" : string.IsNullOrWhiteSpace(_lotText) ? $"Lot {IdLot}" : _lotText; set => SetProperty(ref _lotText, value); }
        public string DisponiblesText => Disponibles?.ToString(CultureInfo.InvariantCulture) ?? Math.Max(0, TotalsEnStock - ReservatsPerComandes).ToString(CultureInfo.InvariantCulture);
    }

    public sealed class LocationRecord : WarehouseRecordBase
    {
        private string? _codiGenerat;
        private int _zona;
        private int _passadis;
        private int _blocEstanteria;
        private int _fila;
        private int _columna;

        public string? CodiGenerat { get => _codiGenerat; set => SetProperty(ref _codiGenerat, value); }
        public int Zona { get => _zona; set => SetProperty(ref _zona, value); }
        public int Passadis { get => _passadis; set => SetProperty(ref _passadis, value); }
        public int BlocEstanteria { get => _blocEstanteria; set => SetProperty(ref _blocEstanteria, value); }
        public int Fila { get => _fila; set => SetProperty(ref _fila, value); }
        public int Columna { get => _columna; set => SetProperty(ref _columna, value); }
        public string CodiText => string.IsNullOrWhiteSpace(CodiGenerat) ? $"Z{Zona}-P{Passadis}-B{BlocEstanteria}-F{Fila}-C{Columna}" : CodiGenerat!;
    }

    public sealed class SupplierLotRecord : WarehouseRecordBase
    {
        private int _idProveidor;
        private int _idProducte;
        private int _quantitatRebuda;
        private DateOnly? _dataDemanat;
        private DateOnly _dataRebut = DateOnly.FromDateTime(DateTime.Today);
        private DateOnly _dataCaducitat = DateOnly.FromDateTime(DateTime.Today.AddMonths(6));
        private string _proveidorText = "";
        private string _producteText = "";

        public int IdProveidor { get => _idProveidor; set => SetProperty(ref _idProveidor, value); }
        public int IdProducte { get => _idProducte; set => SetProperty(ref _idProducte, value); }
        public int QuantitatRebuda { get => _quantitatRebuda; set => SetProperty(ref _quantitatRebuda, value); }
        public DateOnly? DataDemanat { get => _dataDemanat; set => SetProperty(ref _dataDemanat, value); }
        public DateOnly DataRebut { get => _dataRebut; set => SetProperty(ref _dataRebut, value); }
        public DateOnly DataCaducitat { get => _dataCaducitat; set => SetProperty(ref _dataCaducitat, value); }
        public string ProveidorText { get => string.IsNullOrWhiteSpace(_proveidorText) ? $"Proveidor {IdProveidor}" : _proveidorText; set => SetProperty(ref _proveidorText, value); }
        public string ProducteText { get => string.IsNullOrWhiteSpace(_producteText) ? $"Producte {IdProducte}" : _producteText; set => SetProperty(ref _producteText, value); }
        public string DataDemanatText => DataDemanat?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "Sense data";
        public string DataRebutText => DataRebut.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        public string DataCaducitatText => DataCaducitat.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    }

    public sealed class StockMovementRecord
    {
        public int Id { get; init; }
        public string Tipus { get; init; } = "";
        public int IdProducte { get; init; }
        public int? IdLot { get; init; }
        public int? IdUbicacioOrigen { get; init; }
        public int? IdUbicacioDesti { get; init; }
        public int Quantitat { get; init; }
        public string? Motiu { get; init; }
        public DateTime DataMoviment { get; init; }
        public string ProducteText { get; set; } = "";
        public string LotText { get; set; } = "";
        public string OrigenText { get; set; } = "";
        public string DestiText { get; set; } = "";
        public string DataText => DataMoviment.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
    }
}
