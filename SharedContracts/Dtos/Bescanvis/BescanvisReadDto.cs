namespace SharedContracts.Dtos;

public sealed class BescanvisReadDto
{
    public int Id { get; set; }
    public int IdUsuari { get; set; }
    public int IdPremi { get; set; }
    public DateOnly? DataBescanviat { get; set; }
}
