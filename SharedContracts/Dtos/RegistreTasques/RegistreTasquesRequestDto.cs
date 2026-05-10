namespace SharedContracts.Dtos;

public sealed class RegistreTasquesRequestDto
{
    public int IdUsuari { get; set; }
    public int IdTasca { get; set; }
    public int Errors { get; set; }
}
