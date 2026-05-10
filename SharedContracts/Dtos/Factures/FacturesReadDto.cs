namespace SharedContracts.Dtos;

public sealed class FacturesReadDto
{
    public int Id { get; set; }
    public int IdClient { get; set; }
    public int IdComanda { get; set; }
    public decimal? Impostos { get; set; }
    public decimal Total { get; set; }
    public int IdEstat { get; set; }
    public DateTime DataEmissio { get; set; }
    public decimal? ImpostPercentatge { get; set; }
}
