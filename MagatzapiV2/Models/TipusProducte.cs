using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("tipus_producte")]
public partial class TipusProducte
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("descripcio_tipus_producte")]
    [StringLength(80)]
    public string? DescripcioTipusProducte { get; set; }

    [Column("material")]
    [StringLength(85)]
    public string? Material { get; set; }

    [Column("tipus_envas")]
    [StringLength(20)]
    public string TipusEnvas { get; set; } = null!;

    [Column("estat_fisic")]
    [StringLength(6)]
    public string EstatFisic { get; set; } = null!;

    [Column("congelat")]
    public sbyte Congelat { get; set; }

    [Column("fragil")]
    public sbyte Fragil { get; set; }

    [InverseProperty("IdTipusNavigation")]
    public virtual ICollection<Productes> Productes { get; set; } = new List<Productes>();

    [InverseProperty("IdTipusProductePrincipalNavigation")]
    public virtual ICollection<Proveidors> Proveidors { get; set; } = new List<Proveidors>();
}
