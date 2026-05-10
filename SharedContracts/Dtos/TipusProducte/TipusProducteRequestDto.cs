namespace SharedContracts.Dtos;

public sealed class TipusProducteRequestDto
{
    public string? DescripcioTipusProducte { get; set; }
    public string? Material { get; set; }
    public string TipusEnvas { get; set; } = null!;
    public string EstatFisic { get; set; } = null!;
    public sbyte Congelat { get; set; }
    public sbyte Fragil { get; set; }
}
