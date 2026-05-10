namespace SharedContracts.Dtos;

public sealed class UsuariMedallesReadDto
{
    public int Registre { get; set; }
    public int IdMedalla { get; set; }
    public int IdUsuari { get; set; }
    public DateOnly? DataObtencio { get; set; }
}
