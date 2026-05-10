namespace SharedContracts.Dtos;

public sealed class ProveidorsReadDto
{
    public int Id { get; set; }
    public string? MarcaMatriu { get; set; }
    public string NomEmpresa { get; set; } = null!;
    public string Telefon { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Adreca { get; set; }
    public string? UrlWeb { get; set; }
    public int? IdTipusProductePrincipal { get; set; }
}
