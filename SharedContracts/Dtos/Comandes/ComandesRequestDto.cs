namespace SharedContracts.Dtos;

public sealed class ComandesRequestDto
{
    public int IdClient { get; set; }
    public int IdChofer { get; set; }
    public int IdPreparador { get; set; }
    public int IdVehicleTransportista { get; set; }
    public int IdEstat { get; set; }
    public string? Notes { get; set; }
    public DateOnly DataPrevistaEntrega { get; set; }
    public DateOnly? DataEntregat { get; set; }
    public string? PoblacioEntregaAlternativa { get; set; }
    public string? AdrecaEntregaAlternativa { get; set; }
    public string? Estat { get; set; }
    public List<ComandesLiniaRequestDto> Linies { get; set; } = new();
}
