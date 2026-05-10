using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("usuari_medalles")]
[Index("IdMedalla", Name = "fk_medalles_idx")]
[Index("IdUsuari", Name = "fk_usuari_idx")]
public partial class UsuariMedalles
{
    [Key]
    [Column("registre")]
    public int Registre { get; set; }

    [Column("id_medalla")]
    public int IdMedalla { get; set; }

    [Column("id_usuari")]
    public int IdUsuari { get; set; }

    [Column("data_obtencio")]
    public DateOnly? DataObtencio { get; set; }

    [ForeignKey("IdMedalla")]
    [InverseProperty("UsuariMedalles")]
    public virtual Medalles IdMedallaNavigation { get; set; } = null!;

    [ForeignKey("IdUsuari")]
    [InverseProperty("UsuariMedalles")]
    public virtual Usuaris IdUsuariNavigation { get; set; } = null!;
}
