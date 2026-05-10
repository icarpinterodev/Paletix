using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace PaletixDesktop.Models
{
    public enum ProductSyncState
    {
        Synced,
        Pending,
        Error,
        PendingDelete
    }

    public sealed class ProductRecord : INotifyPropertyChanged
    {
        private string? _referencia;
        private string _nom = "";
        private string? _descripcio;
        private int _idTipus;
        private decimal? _volumMl;
        private int _idProveidor;
        private int _idUbicacio;
        private int _caixesPerPalet;
        private string? _imatgeUrl;
        private sbyte _actiu = 1;
        private decimal _preuVendaCaixa;
        private decimal _costPerCaixa;
        private int? _estabilitatAlPalet;
        private decimal? _pesKg;
        private DateTime? _dataAfegit;
        private ProductSyncState _syncState = ProductSyncState.Synced;
        private string _syncMessage = "Sincronitzat";
        private bool _isSelected;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id { get; init; }

        public string? Referencia
        {
            get => _referencia;
            set => SetProperty(ref _referencia, value);
        }

        public string Nom
        {
            get => _nom;
            set => SetProperty(ref _nom, value);
        }

        public string? Descripcio
        {
            get => _descripcio;
            set => SetProperty(ref _descripcio, value);
        }

        public int IdTipus
        {
            get => _idTipus;
            set => SetProperty(ref _idTipus, value);
        }

        public decimal? VolumMl
        {
            get => _volumMl;
            set
            {
                if (SetProperty(ref _volumMl, value))
                {
                    OnPropertyChanged(nameof(VolumMlText));
                }
            }
        }

        public int IdProveidor
        {
            get => _idProveidor;
            set => SetProperty(ref _idProveidor, value);
        }

        public int IdUbicacio
        {
            get => _idUbicacio;
            set => SetProperty(ref _idUbicacio, value);
        }

        public int CaixesPerPalet
        {
            get => _caixesPerPalet;
            set => SetProperty(ref _caixesPerPalet, value);
        }

        public string? ImatgeUrl
        {
            get => _imatgeUrl;
            set
            {
                if (SetProperty(ref _imatgeUrl, value))
                {
                    OnPropertyChanged(nameof(ImageText));
                    OnPropertyChanged(nameof(ImageGlyph));
                }
            }
        }

        public sbyte Actiu
        {
            get => _actiu;
            set
            {
                if (SetProperty(ref _actiu, value))
                {
                    OnPropertyChanged(nameof(ActiuText));
                }
            }
        }

        public decimal PreuVendaCaixa
        {
            get => _preuVendaCaixa;
            set
            {
                if (SetProperty(ref _preuVendaCaixa, value))
                {
                    OnPropertyChanged(nameof(PreuVendaCaixaText));
                }
            }
        }

        public decimal CostPerCaixa
        {
            get => _costPerCaixa;
            set
            {
                if (SetProperty(ref _costPerCaixa, value))
                {
                    OnPropertyChanged(nameof(CostPerCaixaText));
                }
            }
        }

        public int? EstabilitatAlPalet
        {
            get => _estabilitatAlPalet;
            set => SetProperty(ref _estabilitatAlPalet, value);
        }

        public decimal? PesKg
        {
            get => _pesKg;
            set
            {
                if (SetProperty(ref _pesKg, value))
                {
                    OnPropertyChanged(nameof(PesKgText));
                }
            }
        }

        public DateTime? DataAfegit
        {
            get => _dataAfegit;
            set
            {
                if (SetProperty(ref _dataAfegit, value))
                {
                    OnPropertyChanged(nameof(DataAfegitText));
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public ProductSyncState SyncState
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
        public string VolumMlText => VolumMl?.ToString("0.##", CultureInfo.InvariantCulture) ?? "";
        public string PreuVendaCaixaText => PreuVendaCaixa.ToString("C2", CultureInfo.GetCultureInfo("ca-ES"));
        public string CostPerCaixaText => CostPerCaixa.ToString("C2", CultureInfo.GetCultureInfo("ca-ES"));
        public string PesKgText => PesKg?.ToString("0.##", CultureInfo.InvariantCulture) ?? "";
        public string DataAfegitText => DataAfegit?.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("ca-ES")) ?? "";
        public string ActiuText => Actiu == 1 ? "Actiu" : "Inactiu";
        public string ImageText => string.IsNullOrWhiteSpace(ImatgeUrl) ? "Sense imatge" : "Imatge";
        public string ImageGlyph => string.IsNullOrWhiteSpace(ImatgeUrl) ? "\uE8F1" : "\uEB9F";
        public bool IsPending => SyncState is ProductSyncState.Pending or ProductSyncState.Error or ProductSyncState.PendingDelete;
        public bool IsPendingDelete => SyncState == ProductSyncState.PendingDelete;
        public string SyncStateText => SyncState switch
        {
            ProductSyncState.Pending => "Pendent",
            ProductSyncState.Error => "Error sync",
            ProductSyncState.PendingDelete => "Eliminacio pendent",
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
