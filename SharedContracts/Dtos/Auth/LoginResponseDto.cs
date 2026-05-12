using System.Collections.Generic;

namespace SharedContracts.Dtos;

public sealed class LoginResponseDto
{
    public int Id { get; set; }
    public string Nom { get; set; } = "";
    public string Cognoms { get; set; } = "";
    public string Email { get; set; } = "";
    public string Rol { get; set; } = "";
    public string Carrec { get; set; } = "";
    public int SaldoPunts { get; set; }
    public int Nivell { get; set; }
    public List<string> Permissions { get; set; } = new();
}
