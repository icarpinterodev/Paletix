namespace SharedContracts.Dtos;

public sealed class PremisRequestDto
{
    public string Nom { get; set; } = null!;
    public int PreuPunts { get; set; }
    public decimal CostPerLaEmpresaEuros { get; set; }
}
