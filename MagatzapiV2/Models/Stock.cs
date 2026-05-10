using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("stock")]
[Index("IdProducte", Name = "fk_Stock_producte")]
[Index("IdLot", Name = "fk_lot_idx")]
[Index("IdUbicacio", Name = "fk_ubicacio_idx")]
[Index("IdProducte", "IdUbicacio", Name = "idx_stock_producte_ubicacio")]
public partial class Stock
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("id_producte")]
    public int IdProducte { get; set; }

    [Column("id_ubicacio")]
    public int IdUbicacio { get; set; }

    [Column("id_lot")]
    public int? IdLot { get; set; }

    [Column("totals_en_stock")]
    public int TotalsEnStock { get; set; }

    [Column("reservats_per_comandes")]
    public int ReservatsPerComandes { get; set; }

    [Column("disponibles")]
    public int? Disponibles { get; set; }

    [ForeignKey("IdLot")]
    [InverseProperty("Stock")]
    public virtual ProveidorsLot? IdLotNavigation { get; set; }

    [ForeignKey("IdProducte")]
    [InverseProperty("Stock")]
    public virtual Productes IdProducteNavigation { get; set; } = null!;

    [ForeignKey("IdUbicacio")]
    [InverseProperty("Stock")]
    public virtual Ubicacions IdUbicacioNavigation { get; set; } = null!;
}
