using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace PaletixDesktop.Models
{
    public enum SupplierSyncState
    {
        Synced,
        Pending,
        Error,
        PendingDelete
    }

    public sealed class SupplierRecord : INotifyPropertyChanged
    {
        private string? _marcaMatriu;
        private string _nomEmpresa = "";
        private string _telefon = "";
        private string _email = "";
        private string? _adreca;
        private string? _urlWeb;
        private int? _idTipusProductePrincipal;
        private bool _isSelected;
        private SupplierSyncState _syncState = SupplierSyncState.Synced;
        private string _syncMessage = "Sincronitzat";

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id { get; init; }

        public string? MarcaMatriu
        {
            get => _marcaMatriu;
            set
            {
                if (SetProperty(ref _marcaMatriu, value))
                {
                    OnPropertyChanged(nameof(MarcaText));
                }
            }
        }

        public string NomEmpresa
        {
            get => _nomEmpresa;
            set
            {
                if (SetProperty(ref _nomEmpresa, value))
                {
                    OnPropertyChanged(nameof(Initials));
                }
            }
        }

        public string Telefon
        {
            get => _telefon;
            set => SetProperty(ref _telefon, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string? Adreca
        {
            get => _adreca;
            set
            {
                if (SetProperty(ref _adreca, value))
                {
                    OnPropertyChanged(nameof(AdrecaText));
                }
            }
        }

        public string? UrlWeb
        {
            get => _urlWeb;
            set
            {
                if (SetProperty(ref _urlWeb, value))
                {
                    OnPropertyChanged(nameof(UrlWebText));
                }
            }
        }

        public int? IdTipusProductePrincipal
        {
            get => _idTipusProductePrincipal;
            set
            {
                if (SetProperty(ref _idTipusProductePrincipal, value))
                {
                    OnPropertyChanged(nameof(IdTipusProductePrincipalText));
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public SupplierSyncState SyncState
        {
            get => _syncState;
            set
            {
                if (SetProperty(ref _syncState, value))
                {
                    OnPropertyChanged(nameof(IsPending));
                    OnPropertyChanged(nameof(IsPendingDelete));
                    OnPropertyChanged(nameof(SyncStateText));
                }
            }
        }

        public string SyncMessage
        {
            get => _syncMessage;
            set => SetProperty(ref _syncMessage, value);
        }

        public string IdText => Id.ToString(CultureInfo.InvariantCulture);
        public string MarcaText => string.IsNullOrWhiteSpace(MarcaMatriu) ? "Sense marca matriu" : MarcaMatriu!;
        public string AdrecaText => string.IsNullOrWhiteSpace(Adreca) ? "Sense adreca" : Adreca!;
        public string UrlWebText => string.IsNullOrWhiteSpace(UrlWeb) ? "Sense web" : UrlWeb!;
        public string IdTipusProductePrincipalText => IdTipusProductePrincipal?.ToString(CultureInfo.InvariantCulture) ?? "";
        public string Initials => string.IsNullOrWhiteSpace(NomEmpresa) ? "PR" : NomEmpresa.Trim()[0].ToString().ToUpperInvariant();
        public bool IsPending => SyncState is SupplierSyncState.Pending or SupplierSyncState.Error or SupplierSyncState.PendingDelete;
        public bool IsPendingDelete => SyncState == SupplierSyncState.PendingDelete;
        public string SyncStateText => SyncState switch
        {
            SupplierSyncState.Pending => "Pendent",
            SupplierSyncState.Error => "Error sync",
            SupplierSyncState.PendingDelete => "Eliminacio pendent",
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
