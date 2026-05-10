namespace SharedContracts.Dtos;

public sealed class ProveidorsLotReadDto
{
    public int Id { get; set; }
    public int IdProveidor { get; set; }
    public int IdProducte { get; set; }
    public int QuantitatRebuda { get; set; }
    public DateOnly? DataDemanat { get; set; }
    public DateOnly DataRebut { get; set; }
    public DateOnly DataCaducitat { get; set; }
}
