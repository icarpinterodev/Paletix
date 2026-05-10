using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PaletixDesktop.Models
{
    public enum ClientSyncState
    {
        Synced,
        Pending,
        Error,
        PendingDelete
    }

    public sealed class ClientRecord : INotifyPropertyChanged
    {
        private string _nomEmpresa = "";
        private string? _nifEmpresa;
        private string _telefon = "";
        private string? _email;
        private string _adreca = "";
        private string _poblacio = "";
        private string? _nomResponsable;
        private ClientSyncState _syncState = ClientSyncState.Synced;
        private string _syncMessage = "Sincronitzat";
        private bool _isSelected;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id { get; init; }

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

        public string? NifEmpresa
        {
            get => _nifEmpresa;
            set => SetProperty(ref _nifEmpresa, value);
        }

        public string Telefon
        {
            get => _telefon;
            set => SetProperty(ref _telefon, value);
        }

        public string? Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value))
                {
                    OnPropertyChanged(nameof(EmailText));
                }
            }
        }

        public string Adreca
        {
            get => _adreca;
            set => SetProperty(ref _adreca, value);
        }

        public string Poblacio
        {
            get => _poblacio;
            set => SetProperty(ref _poblacio, value);
        }

        public string? NomResponsable
        {
            get => _nomResponsable;
            set
            {
                if (SetProperty(ref _nomResponsable, value))
                {
                    OnPropertyChanged(nameof(ResponsableText));
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public ClientSyncState SyncState
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

        public string IdText => Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public string EmailText => string.IsNullOrWhiteSpace(Email) ? "Sense email" : Email!;
        public string ResponsableText => string.IsNullOrWhiteSpace(NomResponsable) ? "Sense responsable" : NomResponsable!;
        public string Initials => string.IsNullOrWhiteSpace(NomEmpresa) ? "CL" : NomEmpresa.Trim()[0].ToString().ToUpperInvariant();
        public bool IsPending => SyncState is ClientSyncState.Pending or ClientSyncState.Error or ClientSyncState.PendingDelete;
        public bool IsPendingDelete => SyncState == ClientSyncState.PendingDelete;
        public string SyncStateText => SyncState switch
        {
            ClientSyncState.Pending => "Pendent",
            ClientSyncState.Error => "Error sync",
            ClientSyncState.PendingDelete => "Eliminacio pendent",
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
