namespace SharedContracts.Dtos;

public sealed class TasquesReadDto
{
    public int Id { get; set; }
    public string Nom { get; set; } = null!;
    public string? Tipus { get; set; }
    public int PuntsPerTasca { get; set; }
}
