namespace SharedContracts.Dtos;

public sealed class StatsTreballadorReadDto
{
    public int Id { get; set; }
    public int IdUsuari { get; set; }
    public int TasquesRealitzades { get; set; }
    public int TotalErrorsGreus { get; set; }
    public int TotalErrorsLleus { get; set; }
    public DateTime DataUltimRegistre { get; set; }
    public int? MitjanaMinutsPreparacioPerTasca { get; set; }
    public int? MitjanaUnitatsPerTasca { get; set; }
    public TimeOnly? HoraMitjanaFixatgeEntrant { get; set; }
    public TimeOnly? HoraMitjanaFitxatgeSortint { get; set; }
    public int? MinutsEmpratsDescans { get; set; }
}
