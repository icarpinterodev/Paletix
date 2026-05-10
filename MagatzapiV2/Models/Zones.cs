using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("zones")]
[Index("NomZona", Name = "codi_zona_UNIQUE", IsUnique = true)]
public partial class Zones
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nom_zona")]
    [StringLength(20)]
    public string NomZona { get; set; } = null!;

    [Column("descripcio")]
    [StringLength(45)]
    public string? Descripcio { get; set; }

    [Column("area_m2")]
    public int? AreaM2 { get; set; }
}
