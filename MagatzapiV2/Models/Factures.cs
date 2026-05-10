using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("factures")]
[Index("IdClient", Name = "fk_client2_idx")]
[Index("IdComanda", Name = "fk_comanda_idx")]
[Index("IdEstat", Name = "fk_estat2_idx")]
public partial class Factures
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("id_client")]
    public int IdClient { get; set; }

    [Column("id_comanda")]
    public int IdComanda { get; set; }

    [Column("impostos")]
    [Precision(10, 2)]
    public decimal? Impostos { get; set; }

    [Column("total")]
    [Precision(10, 2)]
    public decimal Total { get; set; }

    [Column("id_estat")]
    public int IdEstat { get; set; }

    [Column("data_emissio", TypeName = "datetime")]
    public DateTime DataEmissio { get; set; }

    [Column("impost_percentatge")]
    [Precision(5, 2)]
    public decimal? ImpostPercentatge { get; set; }

    [ForeignKey("IdClient")]
    [InverseProperty("Factures")]
    public virtual Clients IdClientNavigation { get; set; } = null!;

    [ForeignKey("IdComanda")]
    [InverseProperty("Factures")]
    public virtual Comandes IdComandaNavigation { get; set; } = null!;

    [ForeignKey("IdEstat")]
    [InverseProperty("Factures")]
    public virtual Estats IdEstatNavigation { get; set; } = null!;

    [InverseProperty("IdFacturaNavigation")]
    public virtual ICollection<Pagaments> Pagaments { get; set; } = new List<Pagaments>();
}
