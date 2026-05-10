using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("carrecs")]
[Index("Nom", Name = "nom_UNIQUE", IsUnique = true)]
public partial class Carrecs
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nom")]
    [StringLength(50)]
    public string Nom { get; set; } = null!;

    [InverseProperty("IdCarrecNavigation")]
    public virtual ICollection<Usuaris> Usuaris { get; set; } = new List<Usuaris>();
}
