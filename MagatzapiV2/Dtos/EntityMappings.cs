using MagatzapiV2.Models;
using SharedContracts.Dtos;
namespace MagatzapiV2.Dtos;

public static class EntityMappings
{
    public static BescanvisReadDto ToReadDto(Bescanvis entity)
    {
        return new BescanvisReadDto
        {
            Id = entity.Id,
            IdUsuari = entity.IdUsuari,
            IdPremi = entity.IdPremi,
            DataBescanviat = entity.DataBescanviat
        };
    }

    public static Bescanvis ToEntity(BescanvisRequestDto dto)
    {
        return new Bescanvis
        {
            IdUsuari = dto.IdUsuari,
            IdPremi = dto.IdPremi
        };
    }

    public static void ApplyUpdate(BescanvisRequestDto dto, Bescanvis entity)
    {
        entity.IdUsuari = dto.IdUsuari;
        entity.IdPremi = dto.IdPremi;
    }

    public static CarrecsReadDto ToReadDto(Carrecs entity)
    {
        return new CarrecsReadDto
        {
            Id = entity.Id,
            Nom = entity.Nom
        };
    }

    public static Carrecs ToEntity(CarrecsRequestDto dto)
    {
        return new Carrecs
        {
            Nom = dto.Nom
        };
    }

    public static void ApplyUpdate(CarrecsRequestDto dto, Carrecs entity)
    {
        entity.Nom = dto.Nom;
    }

    public static ClientsReadDto ToReadDto(Clients entity)
    {
        return new ClientsReadDto
        {
            Id = entity.Id,
            NomEmpresa = entity.NomEmpresa,
            NifEmpresa = entity.NifEmpresa,
            Telefon = entity.Telefon,
            Email = entity.Email,
            Adreca = entity.Adreca,
            Poblacio = entity.Poblacio,
            NomResponsable = entity.NomResponsable
        };
    }

    public static Clients ToEntity(ClientsRequestDto dto)
    {
        return new Clients
        {
            NomEmpresa = dto.NomEmpresa,
            NifEmpresa = dto.NifEmpresa,
            Telefon = dto.Telefon,
            Email = dto.Email,
            Adreca = dto.Adreca,
            Poblacio = dto.Poblacio,
            NomResponsable = dto.NomResponsable
        };
    }

    public static void ApplyUpdate(ClientsRequestDto dto, Clients entity)
    {
        entity.NomEmpresa = dto.NomEmpresa;
        entity.NifEmpresa = dto.NifEmpresa;
        entity.Telefon = dto.Telefon;
        entity.Email = dto.Email;
        entity.Adreca = dto.Adreca;
        entity.Poblacio = dto.Poblacio;
        entity.NomResponsable = dto.NomResponsable;
    }

    public static ComandesReadDto ToReadDto(Comandes entity)
    {
        return new ComandesReadDto
        {
            Id = entity.Id,
            IdClient = entity.IdClient,
            IdChofer = entity.IdChofer,
            IdPreparador = entity.IdPreparador,
            IdVehicleTransportista = entity.IdVehicleTransportista,
            IdEstat = entity.IdEstat,
            DataCreacio = entity.DataCreacio,
            Notes = entity.Notes,
            DataPrevistaEntrega = entity.DataPrevistaEntrega,
            DataEntregat = entity.DataEntregat,
            PoblacioEntregaAlternativa = entity.PoblacioEntregaAlternativa,
            AdrecaEntregaAlternativa = entity.AdrecaEntregaAlternativa
        };
    }

    public static Comandes ToEntity(ComandesRequestDto dto)
    {
        return new Comandes
        {
            IdClient = dto.IdClient,
            IdChofer = dto.IdChofer,
            IdPreparador = dto.IdPreparador,
            IdVehicleTransportista = dto.IdVehicleTransportista,
            IdEstat = dto.IdEstat,
            Notes = dto.Notes,
            DataPrevistaEntrega = dto.DataPrevistaEntrega,
            DataEntregat = dto.DataEntregat,
            PoblacioEntregaAlternativa = dto.PoblacioEntregaAlternativa,
            AdrecaEntregaAlternativa = dto.AdrecaEntregaAlternativa
        };
    }

    public static void ApplyUpdate(ComandesRequestDto dto, Comandes entity)
    {
        entity.IdClient = dto.IdClient;
        entity.IdChofer = dto.IdChofer;
        entity.IdPreparador = dto.IdPreparador;
        entity.IdVehicleTransportista = dto.IdVehicleTransportista;
        entity.IdEstat = dto.IdEstat;
        entity.Notes = dto.Notes;
        entity.DataPrevistaEntrega = dto.DataPrevistaEntrega;
        entity.DataEntregat = dto.DataEntregat;
        entity.PoblacioEntregaAlternativa = dto.PoblacioEntregaAlternativa;
        entity.AdrecaEntregaAlternativa = dto.AdrecaEntregaAlternativa;
    }

    public static EstatsReadDto ToReadDto(Estats entity)
    {
        return new EstatsReadDto
        {
            Id = entity.Id,
            Codi = entity.Codi,
            Descripcio = entity.Descripcio
        };
    }

    public static Estats ToEntity(EstatsRequestDto dto)
    {
        return new Estats
        {
            Codi = dto.Codi,
            Descripcio = dto.Descripcio
        };
    }

    public static void ApplyUpdate(EstatsRequestDto dto, Estats entity)
    {
        entity.Codi = dto.Codi;
        entity.Descripcio = dto.Descripcio;
    }

    public static FacturesReadDto ToReadDto(Factures entity)
    {
        return new FacturesReadDto
        {
            Id = entity.Id,
            IdClient = entity.IdClient,
            IdComanda = entity.IdComanda,
            Impostos = entity.Impostos,
            Total = entity.Total,
            IdEstat = entity.IdEstat,
            DataEmissio = entity.DataEmissio,
            ImpostPercentatge = entity.ImpostPercentatge
        };
    }

    public static Factures ToEntity(FacturesRequestDto dto)
    {
        return new Factures
        {
            IdClient = dto.IdClient,
            IdComanda = dto.IdComanda,
            Total = dto.Total,
            IdEstat = dto.IdEstat
        };
    }

    public static void ApplyUpdate(FacturesRequestDto dto, Factures entity)
    {
        entity.IdClient = dto.IdClient;
        entity.IdComanda = dto.IdComanda;
        entity.Total = dto.Total;
        entity.IdEstat = dto.IdEstat;
    }

    public static LiniescomandaReadDto ToReadDto(Liniescomanda entity)
    {
        return new LiniescomandaReadDto
        {
            Id = entity.Id,
            IdComanda = entity.IdComanda,
            IdProducte = entity.IdProducte,
            IdUbicacio = entity.IdUbicacio,
            Palets = entity.Palets,
            Caixes = entity.Caixes,
            IdEstatVerificacio = entity.IdEstatVerificacio
        };
    }

    public static Liniescomanda ToEntity(LiniescomandaRequestDto dto)
    {
        return new Liniescomanda
        {
            IdComanda = dto.IdComanda,
            IdProducte = dto.IdProducte,
            IdUbicacio = dto.IdUbicacio,
            Palets = dto.Palets,
            Caixes = dto.Caixes,
            IdEstatVerificacio = dto.IdEstatVerificacio
        };
    }

    public static void ApplyUpdate(LiniescomandaRequestDto dto, Liniescomanda entity)
    {
        entity.IdComanda = dto.IdComanda;
        entity.IdProducte = dto.IdProducte;
        entity.IdUbicacio = dto.IdUbicacio;
        entity.Palets = dto.Palets;
        entity.Caixes = dto.Caixes;
        entity.IdEstatVerificacio = dto.IdEstatVerificacio;
    }

    public static MedallesReadDto ToReadDto(Medalles entity)
    {
        return new MedallesReadDto
        {
            Id = entity.Id,
            Nom = entity.Nom,
            Descripcio = entity.Descripcio
        };
    }

    public static Medalles ToEntity(MedallesRequestDto dto)
    {
        return new Medalles
        {
            Nom = dto.Nom,
            Descripcio = dto.Descripcio
        };
    }

    public static void ApplyUpdate(MedallesRequestDto dto, Medalles entity)
    {
        entity.Nom = dto.Nom;
        entity.Descripcio = dto.Descripcio;
    }

    public static PagamentsReadDto ToReadDto(Pagaments entity)
    {
        return new PagamentsReadDto
        {
            Id = entity.Id,
            IdFactura = entity.IdFactura,
            Import = entity.Import,
            Metode = entity.Metode,
            Data = entity.Data
        };
    }

    public static Pagaments ToEntity(PagamentsRequestDto dto)
    {
        return new Pagaments
        {
            IdFactura = dto.IdFactura,
            Import = dto.Import,
            Metode = dto.Metode
        };
    }

    public static void ApplyUpdate(PagamentsRequestDto dto, Pagaments entity)
    {
        entity.IdFactura = dto.IdFactura;
        entity.Import = dto.Import;
        entity.Metode = dto.Metode;
    }

    public static PremisReadDto ToReadDto(Premis entity)
    {
        return new PremisReadDto
        {
            Id = entity.Id,
            Nom = entity.Nom,
            PreuPunts = entity.PreuPunts,
            CostPerLaEmpresaEuros = entity.CostPerLaEmpresaEuros
        };
    }

    public static Premis ToEntity(PremisRequestDto dto)
    {
        return new Premis
        {
            Nom = dto.Nom,
            PreuPunts = dto.PreuPunts,
            CostPerLaEmpresaEuros = dto.CostPerLaEmpresaEuros
        };
    }

    public static void ApplyUpdate(PremisRequestDto dto, Premis entity)
    {
        entity.Nom = dto.Nom;
        entity.PreuPunts = dto.PreuPunts;
        entity.CostPerLaEmpresaEuros = dto.CostPerLaEmpresaEuros;
    }

    public static ProductesReadDto ToReadDto(Productes entity)
    {
        return new ProductesReadDto
        {
            Id = entity.Id,
            Referencia = entity.Referencia,
            Nom = entity.Nom,
            Descripcio = entity.Descripcio,
            IdTipus = entity.IdTipus,
            VolumMl = entity.VolumMl,
            IdProveidor = entity.IdProveidor,
            IdUbicacio = entity.IdUbicacio,
            CaixesPerPalet = entity.CaixesPerPalet,
            ImatgeUrl = entity.ImatgeUrl,
            Actiu = entity.Actiu,
            PreuVendaCaixa = entity.PreuVendaCaixa,
            CostPerCaixa = entity.CostPerCaixa,
            EstabilitatAlPalet = entity.EstabilitatAlPalet,
            PesKg = entity.PesKg,
            DataAfegit = entity.DataAfegit
        };
    }

    public static Productes ToEntity(ProductesRequestDto dto)
    {
        return new Productes
        {
            Referencia = dto.Referencia,
            Nom = dto.Nom,
            Descripcio = dto.Descripcio,
            IdTipus = dto.IdTipus,
            VolumMl = dto.VolumMl,
            IdProveidor = dto.IdProveidor,
            IdUbicacio = dto.IdUbicacio,
            CaixesPerPalet = dto.CaixesPerPalet,
            ImatgeUrl = dto.ImatgeUrl,
            Actiu = dto.Actiu,
            PreuVendaCaixa = dto.PreuVendaCaixa,
            CostPerCaixa = dto.CostPerCaixa,
            EstabilitatAlPalet = dto.EstabilitatAlPalet,
            PesKg = dto.PesKg
        };
    }

    public static void ApplyUpdate(ProductesRequestDto dto, Productes entity)
    {
        entity.Referencia = dto.Referencia;
        entity.Nom = dto.Nom;
        entity.Descripcio = dto.Descripcio;
        entity.IdTipus = dto.IdTipus;
        entity.VolumMl = dto.VolumMl;
        entity.IdProveidor = dto.IdProveidor;
        entity.IdUbicacio = dto.IdUbicacio;
        entity.CaixesPerPalet = dto.CaixesPerPalet;
        entity.ImatgeUrl = dto.ImatgeUrl;
        entity.Actiu = dto.Actiu;
        entity.PreuVendaCaixa = dto.PreuVendaCaixa;
        entity.CostPerCaixa = dto.CostPerCaixa;
        entity.EstabilitatAlPalet = dto.EstabilitatAlPalet;
        entity.PesKg = dto.PesKg;
    }

    public static ProveidorsReadDto ToReadDto(Proveidors entity)
    {
        return new ProveidorsReadDto
        {
            Id = entity.Id,
            MarcaMatriu = entity.MarcaMatriu,
            NomEmpresa = entity.NomEmpresa,
            Telefon = entity.Telefon,
            Email = entity.Email,
            Adreca = entity.Adreca,
            UrlWeb = entity.UrlWeb,
            IdTipusProductePrincipal = entity.IdTipusProductePrincipal
        };
    }

    public static Proveidors ToEntity(ProveidorsRequestDto dto)
    {
        return new Proveidors
        {
            MarcaMatriu = dto.MarcaMatriu,
            NomEmpresa = dto.NomEmpresa,
            Telefon = dto.Telefon,
            Email = dto.Email,
            Adreca = dto.Adreca,
            UrlWeb = dto.UrlWeb,
            IdTipusProductePrincipal = dto.IdTipusProductePrincipal
        };
    }

    public static void ApplyUpdate(ProveidorsRequestDto dto, Proveidors entity)
    {
        entity.MarcaMatriu = dto.MarcaMatriu;
        entity.NomEmpresa = dto.NomEmpresa;
        entity.Telefon = dto.Telefon;
        entity.Email = dto.Email;
        entity.Adreca = dto.Adreca;
        entity.UrlWeb = dto.UrlWeb;
        entity.IdTipusProductePrincipal = dto.IdTipusProductePrincipal;
    }

    public static ProveidorsLotReadDto ToReadDto(ProveidorsLot entity)
    {
        return new ProveidorsLotReadDto
        {
            Id = entity.Id,
            IdProveidor = entity.IdProveidor,
            IdProducte = entity.IdProducte,
            QuantitatRebuda = entity.QuantitatRebuda,
            DataDemanat = entity.DataDemanat,
            DataRebut = entity.DataRebut,
            DataCaducitat = entity.DataCaducitat
        };
    }

    public static ProveidorsLot ToEntity(ProveidorsLotRequestDto dto)
    {
        return new ProveidorsLot
        {
            IdProveidor = dto.IdProveidor,
            IdProducte = dto.IdProducte,
            QuantitatRebuda = dto.QuantitatRebuda,
            DataDemanat = dto.DataDemanat,
            DataRebut = dto.DataRebut,
            DataCaducitat = dto.DataCaducitat
        };
    }

    public static void ApplyUpdate(ProveidorsLotRequestDto dto, ProveidorsLot entity)
    {
        entity.IdProveidor = dto.IdProveidor;
        entity.IdProducte = dto.IdProducte;
        entity.QuantitatRebuda = dto.QuantitatRebuda;
        entity.DataDemanat = dto.DataDemanat;
        entity.DataRebut = dto.DataRebut;
        entity.DataCaducitat = dto.DataCaducitat;
    }

    public static RegistreTasquesReadDto ToReadDto(RegistreTasques entity)
    {
        return new RegistreTasquesReadDto
        {
            Id = entity.Id,
            IdUsuari = entity.IdUsuari,
            IdTasca = entity.IdTasca,
            MinutsEmprats = entity.MinutsEmprats,
            Errors = entity.Errors,
            Data = entity.Data
        };
    }

    public static RegistreTasques ToEntity(RegistreTasquesRequestDto dto)
    {
        return new RegistreTasques
        {
            IdUsuari = dto.IdUsuari,
            IdTasca = dto.IdTasca,
            Errors = dto.Errors
        };
    }

    public static void ApplyUpdate(RegistreTasquesRequestDto dto, RegistreTasques entity)
    {
        entity.IdUsuari = dto.IdUsuari;
        entity.IdTasca = dto.IdTasca;
        entity.Errors = dto.Errors;
    }

    public static ReptesReadDto ToReadDto(Reptes entity)
    {
        return new ReptesReadDto
        {
            Id = entity.Id,
            IdUsuariProposador = entity.IdUsuariProposador,
            Nom = entity.Nom,
            Descripcio = entity.Descripcio,
            Punts = entity.Punts,
            DataCreacio = entity.DataCreacio
        };
    }

    public static Reptes ToEntity(ReptesRequestDto dto)
    {
        return new Reptes
        {
            IdUsuariProposador = dto.IdUsuariProposador,
            Nom = dto.Nom,
            Descripcio = dto.Descripcio,
            Punts = dto.Punts
        };
    }

    public static void ApplyUpdate(ReptesRequestDto dto, Reptes entity)
    {
        entity.IdUsuariProposador = dto.IdUsuariProposador;
        entity.Nom = dto.Nom;
        entity.Descripcio = dto.Descripcio;
        entity.Punts = dto.Punts;
    }

    public static RolsReadDto ToReadDto(Rols entity)
    {
        return new RolsReadDto
        {
            Id = entity.Id,
            Nom = entity.Nom
        };
    }

    public static Rols ToEntity(RolsRequestDto dto)
    {
        return new Rols
        {
            Nom = dto.Nom
        };
    }

    public static void ApplyUpdate(RolsRequestDto dto, Rols entity)
    {
        entity.Nom = dto.Nom;
    }

    public static StatsTreballadorReadDto ToReadDto(StatsTreballador entity)
    {
        return new StatsTreballadorReadDto
        {
            Id = entity.Id,
            IdUsuari = entity.IdUsuari,
            TasquesRealitzades = entity.TasquesRealitzades,
            TotalErrorsGreus = entity.TotalErrorsGreus,
            TotalErrorsLleus = entity.TotalErrorsLleus,
            DataUltimRegistre = entity.DataUltimRegistre,
            MitjanaMinutsPreparacioPerTasca = entity.MitjanaMinutsPreparacioPerTasca,
            MitjanaUnitatsPerTasca = entity.MitjanaUnitatsPerTasca,
            HoraMitjanaFixatgeEntrant = entity.HoraMitjanaFixatgeEntrant,
            HoraMitjanaFitxatgeSortint = entity.HoraMitjanaFitxatgeSortint,
            MinutsEmpratsDescans = entity.MinutsEmpratsDescans
        };
    }

    public static StatsTreballador ToEntity(StatsTreballadorRequestDto dto)
    {
        return new StatsTreballador
        {
            IdUsuari = dto.IdUsuari,
            TasquesRealitzades = dto.TasquesRealitzades,
            TotalErrorsGreus = dto.TotalErrorsGreus,
            TotalErrorsLleus = dto.TotalErrorsLleus,
            MitjanaMinutsPreparacioPerTasca = dto.MitjanaMinutsPreparacioPerTasca,
            MitjanaUnitatsPerTasca = dto.MitjanaUnitatsPerTasca,
            HoraMitjanaFixatgeEntrant = dto.HoraMitjanaFixatgeEntrant,
            HoraMitjanaFitxatgeSortint = dto.HoraMitjanaFitxatgeSortint,
            MinutsEmpratsDescans = dto.MinutsEmpratsDescans
        };
    }

    public static void ApplyUpdate(StatsTreballadorRequestDto dto, StatsTreballador entity)
    {
        entity.IdUsuari = dto.IdUsuari;
        entity.TasquesRealitzades = dto.TasquesRealitzades;
        entity.TotalErrorsGreus = dto.TotalErrorsGreus;
        entity.TotalErrorsLleus = dto.TotalErrorsLleus;
        entity.MitjanaMinutsPreparacioPerTasca = dto.MitjanaMinutsPreparacioPerTasca;
        entity.MitjanaUnitatsPerTasca = dto.MitjanaUnitatsPerTasca;
        entity.HoraMitjanaFixatgeEntrant = dto.HoraMitjanaFixatgeEntrant;
        entity.HoraMitjanaFitxatgeSortint = dto.HoraMitjanaFitxatgeSortint;
        entity.MinutsEmpratsDescans = dto.MinutsEmpratsDescans;
    }

    public static StockReadDto ToReadDto(Stock entity)
    {
        return new StockReadDto
        {
            Id = entity.Id,
            IdProducte = entity.IdProducte,
            IdUbicacio = entity.IdUbicacio,
            IdLot = entity.IdLot,
            TotalsEnStock = entity.TotalsEnStock,
            ReservatsPerComandes = entity.ReservatsPerComandes,
            Disponibles = entity.Disponibles
        };
    }

    public static Stock ToEntity(StockRequestDto dto)
    {
        return new Stock
        {
            IdProducte = dto.IdProducte,
            IdUbicacio = dto.IdUbicacio,
            IdLot = dto.IdLot,
            TotalsEnStock = dto.TotalsEnStock,
            ReservatsPerComandes = dto.ReservatsPerComandes
        };
    }

    public static void ApplyUpdate(StockRequestDto dto, Stock entity)
    {
        entity.IdProducte = dto.IdProducte;
        entity.IdUbicacio = dto.IdUbicacio;
        entity.IdLot = dto.IdLot;
        entity.TotalsEnStock = dto.TotalsEnStock;
        entity.ReservatsPerComandes = dto.ReservatsPerComandes;
    }

    public static StockMovimentReadDto ToReadDto(StockMoviments entity)
    {
        return new StockMovimentReadDto
        {
            Id = entity.Id,
            Tipus = entity.Tipus,
            IdProducte = entity.IdProducte,
            IdLot = entity.IdLot,
            IdUbicacioOrigen = entity.IdUbicacioOrigen,
            IdUbicacioDesti = entity.IdUbicacioDesti,
            Quantitat = entity.Quantitat,
            TotalOrigenAbans = entity.TotalOrigenAbans,
            TotalOrigenDespres = entity.TotalOrigenDespres,
            ReservatOrigenAbans = entity.ReservatOrigenAbans,
            ReservatOrigenDespres = entity.ReservatOrigenDespres,
            TotalDestiAbans = entity.TotalDestiAbans,
            TotalDestiDespres = entity.TotalDestiDespres,
            ReservatDestiAbans = entity.ReservatDestiAbans,
            ReservatDestiDespres = entity.ReservatDestiDespres,
            Motiu = entity.Motiu,
            DataMoviment = entity.DataMoviment
        };
    }

    public static TasquesReadDto ToReadDto(Tasques entity)
    {
        return new TasquesReadDto
        {
            Id = entity.Id,
            Nom = entity.Nom,
            Tipus = entity.Tipus,
            PuntsPerTasca = entity.PuntsPerTasca
        };
    }

    public static Tasques ToEntity(TasquesRequestDto dto)
    {
        return new Tasques
        {
            Nom = dto.Nom,
            Tipus = dto.Tipus,
            PuntsPerTasca = dto.PuntsPerTasca
        };
    }

    public static void ApplyUpdate(TasquesRequestDto dto, Tasques entity)
    {
        entity.Nom = dto.Nom;
        entity.Tipus = dto.Tipus;
        entity.PuntsPerTasca = dto.PuntsPerTasca;
    }

    public static TipusProducteReadDto ToReadDto(TipusProducte entity)
    {
        return new TipusProducteReadDto
        {
            Id = entity.Id,
            DescripcioTipusProducte = entity.DescripcioTipusProducte,
            Material = entity.Material,
            TipusEnvas = entity.TipusEnvas,
            EstatFisic = entity.EstatFisic,
            Congelat = entity.Congelat,
            Fragil = entity.Fragil
        };
    }

    public static TipusProducte ToEntity(TipusProducteRequestDto dto)
    {
        return new TipusProducte
        {
            DescripcioTipusProducte = dto.DescripcioTipusProducte,
            Material = dto.Material,
            TipusEnvas = dto.TipusEnvas,
            EstatFisic = dto.EstatFisic,
            Congelat = dto.Congelat,
            Fragil = dto.Fragil
        };
    }

    public static void ApplyUpdate(TipusProducteRequestDto dto, TipusProducte entity)
    {
        entity.DescripcioTipusProducte = dto.DescripcioTipusProducte;
        entity.Material = dto.Material;
        entity.TipusEnvas = dto.TipusEnvas;
        entity.EstatFisic = dto.EstatFisic;
        entity.Congelat = dto.Congelat;
        entity.Fragil = dto.Fragil;
    }

    public static TipusVehiclesReadDto ToReadDto(TipusVehicles entity)
    {
        return new TipusVehiclesReadDto
        {
            Id = entity.Id,
            Nom = entity.Nom
        };
    }

    public static TipusVehicles ToEntity(TipusVehiclesRequestDto dto)
    {
        return new TipusVehicles
        {
            Nom = dto.Nom
        };
    }

    public static void ApplyUpdate(TipusVehiclesRequestDto dto, TipusVehicles entity)
    {
        entity.Nom = dto.Nom;
    }

    public static UbicacionsReadDto ToReadDto(Ubicacions entity)
    {
        return new UbicacionsReadDto
        {
            Id = entity.Id,
            CodiGenerat = entity.CodiGenerat,
            Zona = entity.Zona,
            Passadis = entity.Passadis,
            BlocEstanteria = entity.BlocEstanteria,
            Fila = entity.Fila,
            Columna = entity.Columna
        };
    }

    public static Ubicacions ToEntity(UbicacionsRequestDto dto)
    {
        return new Ubicacions
        {
            Zona = dto.Zona,
            Passadis = dto.Passadis,
            BlocEstanteria = dto.BlocEstanteria,
            Fila = dto.Fila,
            Columna = dto.Columna
        };
    }

    public static void ApplyUpdate(UbicacionsRequestDto dto, Ubicacions entity)
    {
        entity.Zona = dto.Zona;
        entity.Passadis = dto.Passadis;
        entity.BlocEstanteria = dto.BlocEstanteria;
        entity.Fila = dto.Fila;
        entity.Columna = dto.Columna;
    }

    public static UsuariMedallesReadDto ToReadDto(UsuariMedalles entity)
    {
        return new UsuariMedallesReadDto
        {
            Registre = entity.Registre,
            IdMedalla = entity.IdMedalla,
            IdUsuari = entity.IdUsuari,
            DataObtencio = entity.DataObtencio
        };
    }

    public static UsuariMedalles ToEntity(UsuariMedallesRequestDto dto)
    {
        return new UsuariMedalles
        {
            IdMedalla = dto.IdMedalla,
            IdUsuari = dto.IdUsuari
        };
    }

    public static void ApplyUpdate(UsuariMedallesRequestDto dto, UsuariMedalles entity)
    {
        entity.IdMedalla = dto.IdMedalla;
        entity.IdUsuari = dto.IdUsuari;
    }

    public static UsuariReptesReadDto ToReadDto(UsuariReptes entity)
    {
        return new UsuariReptesReadDto
        {
            Id = entity.Id,
            IdUsuariGuanyador = entity.IdUsuariGuanyador,
            IdRepte = entity.IdRepte,
            DataCompletat = entity.DataCompletat,
            Completat = entity.Completat
        };
    }

    public static UsuariReptes ToEntity(UsuariReptesRequestDto dto)
    {
        return new UsuariReptes
        {
            IdUsuariGuanyador = dto.IdUsuariGuanyador,
            IdRepte = dto.IdRepte,
            Completat = dto.Completat
        };
    }

    public static void ApplyUpdate(UsuariReptesRequestDto dto, UsuariReptes entity)
    {
        entity.IdUsuariGuanyador = dto.IdUsuariGuanyador;
        entity.IdRepte = dto.IdRepte;
        entity.Completat = dto.Completat;
    }

    public static UsuarisReadDto ToReadDto(Usuaris entity)
    {
        return new UsuarisReadDto
        {
            Id = entity.Id,
            Nom = entity.Nom,
            Cognoms = entity.Cognoms,
            Dni = entity.Dni,
            DataNaixement = entity.DataNaixement,
            DataContractacio = entity.DataContractacio,
            Email = entity.Email,
            Telefon = entity.Telefon,
            Salari = entity.Salari,
            Torn = entity.Torn,
            NumSeguretatSocial = entity.NumSeguretatSocial,
            NumCompteBancari = entity.NumCompteBancari,
            IdCarrec = entity.IdCarrec,
            IdRol = entity.IdRol,
            SaldoPunts = entity.SaldoPunts,
            Nivell = entity.Nivell,
            AnysExperiencia = entity.AnysExperiencia,
            DataDeCreacio = entity.DataDeCreacio
        };
    }

    public static Usuaris ToEntity(UsuarisRequestDto dto)
    {
        return new Usuaris
        {
            Nom = dto.Nom,
            Cognoms = dto.Cognoms,
            Dni = dto.Dni,
            DataNaixement = dto.DataNaixement,
            DataContractacio = dto.DataContractacio,
            Email = dto.Email,
            Telefon = dto.Telefon,
            Password = dto.Password,
            Salari = dto.Salari,
            Torn = dto.Torn,
            NumSeguretatSocial = dto.NumSeguretatSocial,
            NumCompteBancari = dto.NumCompteBancari,
            IdCarrec = dto.IdCarrec,
            IdRol = dto.IdRol,
            SaldoPunts = dto.SaldoPunts,
            Nivell = dto.Nivell,
            AnysExperiencia = dto.AnysExperiencia
        };
    }

    public static void ApplyUpdate(UsuarisRequestDto dto, Usuaris entity)
    {
        entity.Nom = dto.Nom;
        entity.Cognoms = dto.Cognoms;
        entity.Dni = dto.Dni;
        entity.DataNaixement = dto.DataNaixement;
        entity.DataContractacio = dto.DataContractacio;
        entity.Email = dto.Email;
        entity.Telefon = dto.Telefon;
        entity.Password = dto.Password;
        entity.Salari = dto.Salari;
        entity.Torn = dto.Torn;
        entity.NumSeguretatSocial = dto.NumSeguretatSocial;
        entity.NumCompteBancari = dto.NumCompteBancari;
        entity.IdCarrec = dto.IdCarrec;
        entity.IdRol = dto.IdRol;
        entity.SaldoPunts = dto.SaldoPunts;
        entity.Nivell = dto.Nivell;
        entity.AnysExperiencia = dto.AnysExperiencia;
    }

    public static VehiclesReadDto ToReadDto(Vehicles entity)
    {
        return new VehiclesReadDto
        {
            Id = entity.Id,
            Matricula = entity.Matricula,
            Marca = entity.Marca,
            Model = entity.Model,
            IdTipusVehicle = entity.IdTipusVehicle,
            KilometratgeOHoresfuncionament = entity.KilometratgeOHoresfuncionament,
            UltimaRevisio = entity.UltimaRevisio,
            VehicleLlogat = entity.VehicleLlogat,
            CapacitatKg = entity.CapacitatKg,
            UltimRegistreKilometratge = entity.UltimRegistreKilometratge,
            CapacitatPalets = entity.CapacitatPalets,
            EsElectric = entity.EsElectric
        };
    }

    public static Vehicles ToEntity(VehiclesRequestDto dto)
    {
        return new Vehicles
        {
            Matricula = dto.Matricula,
            Marca = dto.Marca,
            Model = dto.Model,
            IdTipusVehicle = dto.IdTipusVehicle,
            KilometratgeOHoresfuncionament = dto.KilometratgeOHoresfuncionament,
            UltimaRevisio = dto.UltimaRevisio,
            VehicleLlogat = dto.VehicleLlogat,
            CapacitatKg = dto.CapacitatKg,
            CapacitatPalets = dto.CapacitatPalets
        };
    }

    public static void ApplyUpdate(VehiclesRequestDto dto, Vehicles entity)
    {
        entity.Matricula = dto.Matricula;
        entity.Marca = dto.Marca;
        entity.Model = dto.Model;
        entity.IdTipusVehicle = dto.IdTipusVehicle;
        entity.KilometratgeOHoresfuncionament = dto.KilometratgeOHoresfuncionament;
        entity.UltimaRevisio = dto.UltimaRevisio;
        entity.VehicleLlogat = dto.VehicleLlogat;
        entity.CapacitatKg = dto.CapacitatKg;
        entity.CapacitatPalets = dto.CapacitatPalets;
    }

    public static ZonesReadDto ToReadDto(Zones entity)
    {
        return new ZonesReadDto
        {
            Id = entity.Id,
            NomZona = entity.NomZona,
            Descripcio = entity.Descripcio,
            AreaM2 = entity.AreaM2
        };
    }

    public static Zones ToEntity(ZonesCreateDto dto)
    {
        return new Zones
        {
            Id = dto.Id,
            NomZona = dto.NomZona,
            Descripcio = dto.Descripcio,
            AreaM2 = dto.AreaM2
        };
    }

    public static void ApplyUpdate(ZonesUpdateDto dto, Zones entity)
    {
        entity.NomZona = dto.NomZona;
        entity.Descripcio = dto.Descripcio;
        entity.AreaM2 = dto.AreaM2;
    }

}

