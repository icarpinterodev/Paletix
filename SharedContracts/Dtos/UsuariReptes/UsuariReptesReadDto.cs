namespace SharedContracts.Dtos;

public sealed class UsuariReptesReadDto
{
    public int Id { get; set; }
    public int IdUsuariGuanyador { get; set; }
    public int IdRepte { get; set; }
    public DateTime? DataCompletat { get; set; }
    public sbyte Completat { get; set; }
}
