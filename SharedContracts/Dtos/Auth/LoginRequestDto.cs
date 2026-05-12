namespace SharedContracts.Dtos;

public sealed class LoginRequestDto
{
    public string Identifier { get; set; } = null!;
    public string Password { get; set; } = null!;
}
