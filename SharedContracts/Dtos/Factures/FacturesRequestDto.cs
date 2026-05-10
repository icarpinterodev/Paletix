namespace SharedContracts.Dtos;

public sealed class FacturesRequestDto
{
    public int IdClient { get; set; }
    public int IdComanda { get; set; }
    public decimal Total { get; set; }
    public int IdEstat { get; set; }
}
