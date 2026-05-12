using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace PaletixDesktop.Models
{
    public enum AdminIdentitySyncState
    {
        Synced,
        Pending,
        Error,
        PendingDelete
    }

    public abstract class AdminIdentityRecordBase : INotifyPropertyChanged
    {
        private bool _isSelected;
        private AdminIdentitySyncState _syncState = AdminIdentitySyncState.Synced;
        private string _syncMessage = "Sincronitzat";

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id { get; init; }
        public string IdText => Id.ToString(CultureInfo.InvariantCulture);

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public AdminIdentitySyncState SyncState
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

        public bool IsPending => SyncState is AdminIdentitySyncState.Pending or AdminIdentitySyncState.Error or AdminIdentitySyncState.PendingDelete;
        public bool IsPendingDelete => SyncState == AdminIdentitySyncState.PendingDelete;
        public string SyncStateText => SyncState switch
        {
            AdminIdentitySyncState.Pending => "Pendent",
            AdminIdentitySyncState.Error => "Error sync",
            AdminIdentitySyncState.PendingDelete => "Eliminacio pendent",
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

    public sealed class AdminSimpleRecord : AdminIdentityRecordBase
    {
        private string _nom = "";

        public string Nom
        {
            get => _nom;
            set => SetProperty(ref _nom, value);
        }
    }

    public sealed class AdminUserRecord : AdminIdentityRecordBase
    {
        private string _nom = "";
        private string _cognoms = "";
        private string _dni = "";
        private DateTime _dataNaixement = DateTime.Today.AddYears(-18);
        private DateTime _dataContractacio = DateTime.Today;
        private string _email = "";
        private string _telefon = "";
        private string _password = "";
        private decimal _salari;
        private sbyte? _torn;
        private string? _numSeguretatSocial;
        private string? _numCompteBancari;
        private int _idCarrec;
        private int _idRol;
        private int _saldoPunts;
        private int _nivell = 1;
        private sbyte? _anysExperiencia;
        private DateTime? _dataDeCreacio;
        private string _carrecText = "";
        private string _rolText = "";

        public string Nom { get => _nom; set => SetProperty(ref _nom, value); }
        public string Cognoms { get => _cognoms; set => SetProperty(ref _cognoms, value); }
        public string Dni { get => _dni; set => SetProperty(ref _dni, value); }
        public DateTime DataNaixement { get => _dataNaixement; set => SetProperty(ref _dataNaixement, value); }
        public DateTime DataContractacio { get => _dataContractacio; set => SetProperty(ref _dataContractacio, value); }
        public string Email { get => _email; set => SetProperty(ref _email, value); }
        public string Telefon { get => _telefon; set => SetProperty(ref _telefon, value); }
        public string Password { get => _password; set => SetProperty(ref _password, value); }
        public decimal Salari { get => _salari; set => SetProperty(ref _salari, value); }
        public sbyte? Torn { get => _torn; set => SetProperty(ref _torn, value); }
        public string? NumSeguretatSocial { get => _numSeguretatSocial; set => SetProperty(ref _numSeguretatSocial, value); }
        public string? NumCompteBancari { get => _numCompteBancari; set => SetProperty(ref _numCompteBancari, value); }
        public int IdCarrec { get => _idCarrec; set => SetProperty(ref _idCarrec, value); }
        public int IdRol { get => _idRol; set => SetProperty(ref _idRol, value); }
        public int SaldoPunts { get => _saldoPunts; set => SetProperty(ref _saldoPunts, value); }
        public int Nivell { get => _nivell; set => SetProperty(ref _nivell, value); }
        public sbyte? AnysExperiencia { get => _anysExperiencia; set => SetProperty(ref _anysExperiencia, value); }
        public DateTime? DataDeCreacio { get => _dataDeCreacio; set => SetProperty(ref _dataDeCreacio, value); }
        public string CarrecText { get => _carrecText; set => SetProperty(ref _carrecText, value); }
        public string RolText { get => _rolText; set => SetProperty(ref _rolText, value); }

        public string NomComplet => $"{Nom} {Cognoms}".Trim();
        public string DataNaixementText => DataNaixement.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("ca-ES"));
        public string DataContractacioText => DataContractacio.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("ca-ES"));
        public string TornText => Torn?.ToString(CultureInfo.InvariantCulture) ?? "";
        public string AnysExperienciaText => AnysExperiencia?.ToString(CultureInfo.InvariantCulture) ?? "";
        public string SaldoPuntsText => SaldoPunts.ToString(CultureInfo.InvariantCulture);
        public string NivellText => Nivell.ToString(CultureInfo.InvariantCulture);
    }
}
