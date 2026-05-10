namespace SharedContracts.Dtos;

public sealed class PagamentsRequestDto
{
    public int IdFactura { get; set; }
    public decimal Import { get; set; }
    public string Metode { get; set; } = null!;
}
