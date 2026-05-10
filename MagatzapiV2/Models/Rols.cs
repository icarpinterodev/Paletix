using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("rols")]
[Index("Nom", Name = "nom_UNIQUE", IsUnique = true)]
public partial class Rols
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nom")]
    [StringLength(150)]
    public string Nom { get; set; } = null!;

    [InverseProperty("IdRolNavigation")]
    public virtual ICollection<Usuaris> Usuaris { get; set; } = new List<Usuaris>();
}
