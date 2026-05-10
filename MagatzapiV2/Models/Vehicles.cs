using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("vehicles")]
[Index("IdTipusVehicle", Name = "fk_tipusvehicle1_idx")]
[Index("Matricula", Name = "matricula_UNIQUE", IsUnique = true)]
public partial class Vehicles
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("matricula")]
    [StringLength(10)]
    public string Matricula { get; set; } = null!;

    [Column("marca")]
    [StringLength(45)]
    public string Marca { get; set; } = null!;

    [Column("model")]
    [StringLength(60)]
    public string Model { get; set; } = null!;

    [Column("id_tipus_vehicle")]
    public int IdTipusVehicle { get; set; }

    [Column("kilometratge_o_horesfuncionament")]
    public int KilometratgeOHoresfuncionament { get; set; }

    [Column("ultima_revisio")]
    public DateOnly? UltimaRevisio { get; set; }

    [Column("vehicle_llogat")]
    public sbyte VehicleLlogat { get; set; }

    [Column("capacitat_kg")]
    public int? CapacitatKg { get; set; }

    [Column("ultim_registre_kilometratge")]
    public DateOnly? UltimRegistreKilometratge { get; set; }

    [Column("capacitat_palets")]
    public int? CapacitatPalets { get; set; }

    [Column("es_electric")]
    public sbyte? EsElectric { get; set; }

    [InverseProperty("IdVehicleTransportistaNavigation")]
    public virtual ICollection<Comandes> Comandes { get; set; } = new List<Comandes>();

    [ForeignKey("IdTipusVehicle")]
    [InverseProperty("Vehicles")]
    public virtual TipusVehicles IdTipusVehicleNavigation { get; set; } = null!;
}
