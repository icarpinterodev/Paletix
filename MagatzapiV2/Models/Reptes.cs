using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("reptes")]
[Index("IdUsuariProposador", Name = "fk_usuari_idx")]
public partial class Reptes
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("id_usuari_proposador")]
    public int IdUsuariProposador { get; set; }

    [Column("nom")]
    [StringLength(45)]
    public string? Nom { get; set; }

    [Column("descripcio")]
    [StringLength(45)]
    public string? Descripcio { get; set; }

    [Column("punts")]
    public int? Punts { get; set; }

    [Column("data_creacio", TypeName = "datetime")]
    public DateTime? DataCreacio { get; set; }

    [ForeignKey("IdUsuariProposador")]
    [InverseProperty("Reptes")]
    public virtual Usuaris IdUsuariProposadorNavigation { get; set; } = null!;

    [InverseProperty("IdRepteNavigation")]
    public virtual ICollection<UsuariReptes> UsuariReptes { get; set; } = new List<UsuariReptes>();
}
