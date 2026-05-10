using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("tasques")]
[Index("Nom", Name = "nom_UNIQUE", IsUnique = true)]
public partial class Tasques
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nom")]
    [StringLength(140)]
    public string Nom { get; set; } = null!;

    [Column("tipus")]
    [StringLength(45)]
    public string? Tipus { get; set; }

    [Column("punts_per_tasca")]
    public int PuntsPerTasca { get; set; }

    [InverseProperty("IdTascaNavigation")]
    public virtual ICollection<RegistreTasques> RegistreTasques { get; set; } = new List<RegistreTasques>();
}
