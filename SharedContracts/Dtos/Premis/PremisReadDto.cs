namespace SharedContracts.Dtos;

public sealed class PremisReadDto
{
    public int Id { get; set; }
    public string Nom { get; set; } = null!;
    public int PreuPunts { get; set; }
    public decimal CostPerLaEmpresaEuros { get; set; }
}
