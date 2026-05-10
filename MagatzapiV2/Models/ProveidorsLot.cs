using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("proveidors_lot")]
[Index("IdProducte", Name = "fk_producte_idx")]
[Index("IdProveidor", Name = "fk_proveidor_idx")]
public partial class ProveidorsLot
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("id_proveidor")]
    public int IdProveidor { get; set; }

    [Column("id_producte")]
    public int IdProducte { get; set; }

    [Column("quantitat_rebuda")]
    public int QuantitatRebuda { get; set; }

    [Column("data_demanat")]
    public DateOnly? DataDemanat { get; set; }

    [Column("data_rebut")]
    public DateOnly DataRebut { get; set; }

    [Column("data_caducitat")]
    public DateOnly DataCaducitat { get; set; }

    [ForeignKey("IdProducte")]
    [InverseProperty("ProveidorsLot")]
    public virtual Productes IdProducteNavigation { get; set; } = null!;

    [ForeignKey("IdProveidor")]
    [InverseProperty("ProveidorsLot")]
    public virtual Proveidors IdProveidorNavigation { get; set; } = null!;

    [InverseProperty("IdLotNavigation")]
    public virtual ICollection<Stock> Stock { get; set; } = new List<Stock>();
}
