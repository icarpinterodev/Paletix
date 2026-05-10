namespace SharedContracts.Dtos;

public sealed class StockEntradaRequestDto
{
    public int IdProducte { get; set; }
    public int IdUbicacio { get; set; }
    public int? IdLot { get; set; }
    public int Quantitat { get; set; }
    public string? Motiu { get; set; }
}

public sealed class StockMovimentRequestDto
{
    public int IdProducte { get; set; }
    public int IdUbicacioOrigen { get; set; }
    public int IdUbicacioDesti { get; set; }
    public int? IdLot { get; set; }
    public int Quantitat { get; set; }
    public string? Motiu { get; set; }
}

public sealed class StockAjustRequestDto
{
    public int IdStock { get; set; }
    public int NouTotal { get; set; }
    public string? Motiu { get; set; }
}

public sealed class StockReservaRequestDto
{
    public int IdStock { get; set; }
    public int Quantitat { get; set; }
    public string? Motiu { get; set; }
}

public sealed class StockAlliberamentRequestDto
{
    public int IdStock { get; set; }
    public int Quantitat { get; set; }
    public string? Motiu { get; set; }
}

public sealed class StockMovimentReadDto
{
    public int Id { get; set; }
    public string Tipus { get; set; } = "";
    public int IdProducte { get; set; }
    public int? IdLot { get; set; }
    public int? IdUbicacioOrigen { get; set; }
    public int? IdUbicacioDesti { get; set; }
    public int Quantitat { get; set; }
    public int? TotalOrigenAbans { get; set; }
    public int? TotalOrigenDespres { get; set; }
    public int? ReservatOrigenAbans { get; set; }
    public int? ReservatOrigenDespres { get; set; }
    public int? TotalDestiAbans { get; set; }
    public int? TotalDestiDespres { get; set; }
    public int? ReservatDestiAbans { get; set; }
    public int? ReservatDestiDespres { get; set; }
    public string? Motiu { get; set; }
    public DateTime DataMoviment { get; set; }
}

public sealed class StockOperationResultDto
{
    public StockReadDto? Stock { get; set; }
    public StockReadDto? Origen { get; set; }
    public StockReadDto? Desti { get; set; }
    public StockMovimentReadDto Moviment { get; set; } = new();
}
