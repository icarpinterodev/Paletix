using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("medalles")]
[Index("Descripcio", Name = "descripcio_UNIQUE", IsUnique = true)]
[Index("Nom", Name = "nom_UNIQUE", IsUnique = true)]
public partial class Medalles
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nom")]
    [StringLength(50)]
    public string Nom { get; set; } = null!;

    [Column("descripcio")]
    [StringLength(100)]
    public string Descripcio { get; set; } = null!;

    [InverseProperty("IdMedallaNavigation")]
    public virtual ICollection<UsuariMedalles> UsuariMedalles { get; set; } = new List<UsuariMedalles>();
}
