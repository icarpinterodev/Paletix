using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("premis")]
[Index("Nom", Name = "nom_UNIQUE", IsUnique = true)]
[Index("PreuPunts", Name = "preupunts_idx")]
public partial class Premis
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nom")]
    [StringLength(150)]
    public string Nom { get; set; } = null!;

    [Column("preu_punts")]
    public int PreuPunts { get; set; }

    [Column("cost_per_la_empresa_euros")]
    [Precision(10, 2)]
    public decimal CostPerLaEmpresaEuros { get; set; }

    [InverseProperty("IdPremiNavigation")]
    public virtual ICollection<Bescanvis> Bescanvis { get; set; } = new List<Bescanvis>();
}
