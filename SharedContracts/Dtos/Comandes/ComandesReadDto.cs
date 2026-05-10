namespace SharedContracts.Dtos;

public sealed class ComandesReadDto
{
    public int Id { get; set; }
    public int IdClient { get; set; }
    public int IdChofer { get; set; }
    public int IdPreparador { get; set; }
    public int IdVehicleTransportista { get; set; }
    public int IdEstat { get; set; }
    public DateOnly? DataCreacio { get; set; }
    public string? Notes { get; set; }
    public DateOnly DataPrevistaEntrega { get; set; }
    public DateOnly? DataEntregat { get; set; }
    public string? PoblacioEntregaAlternativa { get; set; }
    public string? AdrecaEntregaAlternativa { get; set; }
    public string? Estat { get; set; }
    public List<ComandesLiniaReadDto> Linies { get; set; } = new();
}
