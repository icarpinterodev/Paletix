namespace SharedContracts.Dtos;

public sealed class LiniescomandaRequestDto
{
    public int IdComanda { get; set; }
    public int IdProducte { get; set; }
    public int IdUbicacio { get; set; }
    public int? Palets { get; set; }
    public int Caixes { get; set; }
    public int? IdEstatVerificacio { get; set; }
}
