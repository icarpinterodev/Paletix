using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("tipus_vehicles")]
[Index("Nom", Name = "nom_UNIQUE", IsUnique = true)]
public partial class TipusVehicles
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nom")]
    [StringLength(45)]
    public string Nom { get; set; } = null!;

    [InverseProperty("IdTipusVehicleNavigation")]
    public virtual ICollection<Vehicles> Vehicles { get; set; } = new List<Vehicles>();
}
