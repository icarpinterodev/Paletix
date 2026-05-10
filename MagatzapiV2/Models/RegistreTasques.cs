using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("registre_tasques")]
[Index("IdTasca", Name = "fk_premi00_idx")]
[Index("IdUsuari", Name = "fk_usuari_idx")]
public partial class RegistreTasques
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("id_usuari")]
    public int IdUsuari { get; set; }

    [Column("id_tasca")]
    public int IdTasca { get; set; }

    [Column("minuts_emprats")]
    public int? MinutsEmprats { get; set; }

    [Column("errors")]
    public int Errors { get; set; }

    [Column("data", TypeName = "datetime")]
    public DateTime Data { get; set; }

    [ForeignKey("IdTasca")]
    [InverseProperty("RegistreTasques")]
    public virtual Tasques IdTascaNavigation { get; set; } = null!;

    [ForeignKey("IdUsuari")]
    [InverseProperty("RegistreTasques")]
    public virtual Usuaris IdUsuariNavigation { get; set; } = null!;
}
