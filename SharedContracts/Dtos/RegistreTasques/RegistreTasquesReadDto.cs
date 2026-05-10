namespace SharedContracts.Dtos;

public sealed class RegistreTasquesReadDto
{
    public int Id { get; set; }
    public int IdUsuari { get; set; }
    public int IdTasca { get; set; }
    public int? MinutsEmprats { get; set; }
    public int Errors { get; set; }
    public DateTime Data { get; set; }
}
