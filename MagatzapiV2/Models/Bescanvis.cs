using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("bescanvis")]
[Index("IdPremi", Name = "fk_premi_idx")]
[Index("IdUsuari", Name = "fk_usuari_idx")]
public partial class Bescanvis
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("id_usuari")]
    public int IdUsuari { get; set; }

    [Column("id_premi")]
    public int IdPremi { get; set; }

    [Column("data_bescanviat")]
    public DateOnly? DataBescanviat { get; set; }

    [ForeignKey("IdPremi")]
    [InverseProperty("Bescanvis")]
    public virtual Premis IdPremiNavigation { get; set; } = null!;

    [ForeignKey("IdUsuari")]
    [InverseProperty("Bescanvis")]
    public virtual Usuaris IdUsuariNavigation { get; set; } = null!;
}
