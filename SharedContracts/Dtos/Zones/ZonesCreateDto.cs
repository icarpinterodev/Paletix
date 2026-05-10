namespace SharedContracts.Dtos;

public sealed class ZonesCreateDto
{
    public int Id { get; set; }
    public string NomZona { get; set; } = null!;
    public string? Descripcio { get; set; }
    public int? AreaM2 { get; set; }
}
