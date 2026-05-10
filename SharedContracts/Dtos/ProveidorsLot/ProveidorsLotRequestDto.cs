namespace SharedContracts.Dtos;

public sealed class ProveidorsLotRequestDto
{
    public int IdProveidor { get; set; }
    public int IdProducte { get; set; }
    public int QuantitatRebuda { get; set; }
    public DateOnly? DataDemanat { get; set; }
    public DateOnly DataRebut { get; set; }
    public DateOnly DataCaducitat { get; set; }
}
