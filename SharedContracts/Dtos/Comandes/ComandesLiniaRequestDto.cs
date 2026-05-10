namespace SharedContracts.Dtos;

public sealed class ComandesLiniaRequestDto
{
    public int IdProducte { get; set; }
    public int? IdUbicacio { get; set; }
    public string? Ubicacio { get; set; }
    public int? Palets { get; set; }
    public int Caixes { get; set; }
    public int? IdEstatVerificacio { get; set; }
}
