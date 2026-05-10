namespace SharedContracts.Dtos;

public sealed class PagamentsReadDto
{
    public int Id { get; set; }
    public int IdFactura { get; set; }
    public decimal Import { get; set; }
    public string Metode { get; set; } = null!;
    public DateTime Data { get; set; }
}
