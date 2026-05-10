namespace SharedContracts.Dtos;

public sealed class ProductesReadDto
{
    public int Id { get; set; }
    public string? Referencia { get; set; }
    public string Nom { get; set; } = null!;
    public string? Descripcio { get; set; }
    public int IdTipus { get; set; }
    public decimal? VolumMl { get; set; }
    public int IdProveidor { get; set; }
    public int IdUbicacio { get; set; }
    public int CaixesPerPalet { get; set; }
    public string? ImatgeUrl { get; set; }
    public sbyte Actiu { get; set; }
    public decimal PreuVendaCaixa { get; set; }
    public decimal CostPerCaixa { get; set; }
    public int? EstabilitatAlPalet { get; set; }
    public decimal? PesKg { get; set; }
    public DateTime? DataAfegit { get; set; }
}
