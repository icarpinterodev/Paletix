using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("liniescomanda")]
[Index("IdEstatVerificacio", Name = "fk_estats1_idx")]
[Index("IdProducte", Name = "fk_productes_idx")]
[Index("IdUbicacio", Name = "fk_ubicacio_idx")]
[Index("IdComanda", "IdProducte", Name = "idx_liniescomanda_comanda_producte")]
public partial class Liniescomanda
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("id_comanda")]
    public int IdComanda { get; set; }

    [Column("id_producte")]
    public int IdProducte { get; set; }

    [Column("id_ubicacio")]
    public int IdUbicacio { get; set; }

    [Column("palets")]
    public int? Palets { get; set; }

    [Column("caixes")]
    public int Caixes { get; set; }

    [Column("id_estat_verificacio")]
    public int? IdEstatVerificacio { get; set; }

    [ForeignKey("IdComanda")]
    [InverseProperty("Liniescomanda")]
    public virtual Comandes IdComandaNavigation { get; set; } = null!;

    [ForeignKey("IdEstatVerificacio")]
    [InverseProperty("Liniescomanda")]
    public virtual Estats? IdEstatVerificacioNavigation { get; set; }

    [ForeignKey("IdProducte")]
    [InverseProperty("Liniescomanda")]
    public virtual Productes IdProducteNavigation { get; set; } = null!;

    [ForeignKey("IdUbicacio")]
    [InverseProperty("Liniescomanda")]
    public virtual Ubicacions IdUbicacioNavigation { get; set; } = null!;
}
