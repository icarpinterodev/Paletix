using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("ubicacions")]
[Index("CodiGenerat", Name = "codi_UNIQUE", IsUnique = true)]
[Index("Zona", "Passadis", "BlocEstanteria", "Fila", "Columna", Name = "zona", IsUnique = true)]
public partial class Ubicacions
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("codi_generat")]
    [StringLength(30)]
    public string? CodiGenerat { get; set; }

    [Column("zona")]
    public int Zona { get; set; }

    [Column("passadis")]
    public int Passadis { get; set; }

    [Column("bloc_estanteria")]
    public int BlocEstanteria { get; set; }

    [Column("fila")]
    public int Fila { get; set; }

    [Column("columna")]
    public int Columna { get; set; }

    [InverseProperty("IdUbicacioNavigation")]
    public virtual ICollection<Liniescomanda> Liniescomanda { get; set; } = new List<Liniescomanda>();

    [InverseProperty("IdUbicacioNavigation")]
    public virtual ICollection<Productes> Productes { get; set; } = new List<Productes>();

    [InverseProperty("IdUbicacioNavigation")]
    public virtual ICollection<Stock> Stock { get; set; } = new List<Stock>();
}
