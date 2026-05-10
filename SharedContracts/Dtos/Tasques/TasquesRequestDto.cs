namespace SharedContracts.Dtos;

public sealed class TasquesRequestDto
{
    public string Nom { get; set; } = null!;
    public string? Tipus { get; set; }
    public int PuntsPerTasca { get; set; }
}
