namespace SharedContracts.Dtos;

public sealed class VehiclesReadDto
{
    public int Id { get; set; }
    public string Matricula { get; set; } = null!;
    public string Marca { get; set; } = null!;
    public string Model { get; set; } = null!;
    public int IdTipusVehicle { get; set; }
    public int KilometratgeOHoresfuncionament { get; set; }
    public DateOnly? UltimaRevisio { get; set; }
    public sbyte VehicleLlogat { get; set; }
    public int? CapacitatKg { get; set; }
    public DateOnly? UltimRegistreKilometratge { get; set; }
    public int? CapacitatPalets { get; set; }
    public sbyte? EsElectric { get; set; }
}
