namespace SharedContracts.Dtos;

public sealed class VehiclesRequestDto
{
    public string Matricula { get; set; } = null!;
    public string Marca { get; set; } = null!;
    public string Model { get; set; } = null!;
    public int IdTipusVehicle { get; set; }
    public int KilometratgeOHoresfuncionament { get; set; }
    public DateOnly? UltimaRevisio { get; set; }
    public sbyte VehicleLlogat { get; set; }
    public int? CapacitatKg { get; set; }
    public int? CapacitatPalets { get; set; }
}
