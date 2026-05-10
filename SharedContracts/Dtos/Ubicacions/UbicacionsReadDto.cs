namespace SharedContracts.Dtos;

public sealed class UbicacionsReadDto
{
    public int Id { get; set; }
    public string? CodiGenerat { get; set; }
    public int Zona { get; set; }
    public int Passadis { get; set; }
    public int BlocEstanteria { get; set; }
    public int Fila { get; set; }
    public int Columna { get; set; }
}
