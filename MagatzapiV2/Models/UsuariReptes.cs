using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

/// <summary>
/// Reptes proposats per l&apos;administració o els propis treballadors, a canvi d
/// </summary>
[Table("usuari_reptes")]
[Index("IdRepte", Name = "fk_premi0_idx")]
[Index("IdUsuariGuanyador", Name = "fk_usuari_idx")]
public partial class UsuariReptes
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("id_usuari_guanyador")]
    public int IdUsuariGuanyador { get; set; }

    [Column("id_repte")]
    public int IdRepte { get; set; }

    [Column("data_completat", TypeName = "datetime")]
    public DateTime? DataCompletat { get; set; }

    [Column("completat")]
    public sbyte Completat { get; set; }

    [ForeignKey("IdRepte")]
    [InverseProperty("UsuariReptes")]
    public virtual Reptes IdRepteNavigation { get; set; } = null!;

    [ForeignKey("IdUsuariGuanyador")]
    [InverseProperty("UsuariReptes")]
    public virtual Usuaris IdUsuariGuanyadorNavigation { get; set; } = null!;
}
