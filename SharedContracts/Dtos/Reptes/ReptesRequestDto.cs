namespace SharedContracts.Dtos;

public sealed class ReptesRequestDto
{
    public int IdUsuariProposador { get; set; }
    public string? Nom { get; set; }
    public string? Descripcio { get; set; }
    public int? Punts { get; set; }
}
