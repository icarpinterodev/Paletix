namespace SharedContracts.Dtos;

public sealed class UsuarisRequestDto
{
    public string Nom { get; set; } = null!;
    public string Cognoms { get; set; } = null!;
    public string Dni { get; set; } = null!;
    public DateOnly DataNaixement { get; set; }
    public DateOnly DataContractacio { get; set; }
    public string Email { get; set; } = null!;
    public string Telefon { get; set; } = null!;
    public string Password { get; set; } = null!;
    public decimal Salari { get; set; }
    public sbyte? Torn { get; set; }
    public string? NumSeguretatSocial { get; set; }
    public string? NumCompteBancari { get; set; }
    public int IdCarrec { get; set; }
    public int IdRol { get; set; }
    public int SaldoPunts { get; set; }
    public int Nivell { get; set; }
    public sbyte? AnysExperiencia { get; set; }
}
