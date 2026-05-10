using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("estats")]
[Index("Codi", Name = "codi_UNIQUE", IsUnique = true)]
public partial class Estats
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("codi")]
    [StringLength(15)]
    public string Codi { get; set; } = null!;

    [Column("descripcio")]
    [StringLength(60)]
    public string Descripcio { get; set; } = null!;

    [InverseProperty("IdEstatNavigation")]
    public virtual ICollection<Comandes> Comandes { get; set; } = new List<Comandes>();

    [InverseProperty("IdEstatNavigation")]
    public virtual ICollection<Factures> Factures { get; set; } = new List<Factures>();

    [InverseProperty("IdEstatVerificacioNavigation")]
    public virtual ICollection<Liniescomanda> Liniescomanda { get; set; } = new List<Liniescomanda>();
}
