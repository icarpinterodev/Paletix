namespace SharedContracts.Dtos;

public sealed class ClientsReadDto
{
    public int Id { get; set; }
    public string NomEmpresa { get; set; } = null!;
    public string? NifEmpresa { get; set; }
    public string Telefon { get; set; } = null!;
    public string? Email { get; set; }
    public string Adreca { get; set; } = null!;
    public string Poblacio { get; set; } = null!;
    public string? NomResponsable { get; set; }
}
