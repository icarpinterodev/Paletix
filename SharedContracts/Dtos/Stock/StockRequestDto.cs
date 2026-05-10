namespace SharedContracts.Dtos;

public sealed class StockRequestDto
{
    public int IdProducte { get; set; }
    public int IdUbicacio { get; set; }
    public int? IdLot { get; set; }
    public int TotalsEnStock { get; set; }
    public int ReservatsPerComandes { get; set; }
}
