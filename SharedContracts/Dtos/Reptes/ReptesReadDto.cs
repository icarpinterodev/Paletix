namespace SharedContracts.Dtos;

public sealed class ReptesReadDto
{
    public int Id { get; set; }
    public int IdUsuariProposador { get; set; }
    public string? Nom { get; set; }
    public string? Descripcio { get; set; }
    public int? Punts { get; set; }
    public DateTime? DataCreacio { get; set; }
}
