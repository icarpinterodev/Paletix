namespace SharedContracts.Dtos;

public sealed class StockReadDto
{
    public int Id { get; set; }
    public int IdProducte { get; set; }
    public int IdUbicacio { get; set; }
    public int? IdLot { get; set; }
    public int TotalsEnStock { get; set; }
    public int ReservatsPerComandes { get; set; }
    public int? Disponibles { get; set; }
}
