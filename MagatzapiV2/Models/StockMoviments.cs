using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("stock_moviments")]
[Index("IdProducte", Name = "idx_stock_moviments_producte")]
[Index("IdLot", Name = "idx_stock_moviments_lot")]
[Index("IdUbicacioOrigen", Name = "idx_stock_moviments_ubicacio_origen")]
[Index("IdUbicacioDesti", Name = "idx_stock_moviments_ubicacio_desti")]
public partial class StockMoviments
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("tipus")]
    [StringLength(30)]
    public string Tipus { get; set; } = null!;

    [Column("id_producte")]
    public int IdProducte { get; set; }

    [Column("id_lot")]
    public int? IdLot { get; set; }

    [Column("id_ubicacio_origen")]
    public int? IdUbicacioOrigen { get; set; }

    [Column("id_ubicacio_desti")]
    public int? IdUbicacioDesti { get; set; }

    [Column("quantitat")]
    public int Quantitat { get; set; }

    [Column("total_origen_abans")]
    public int? TotalOrigenAbans { get; set; }

    [Column("total_origen_despres")]
    public int? TotalOrigenDespres { get; set; }

    [Column("reservat_origen_abans")]
    public int? ReservatOrigenAbans { get; set; }

    [Column("reservat_origen_despres")]
    public int? ReservatOrigenDespres { get; set; }

    [Column("total_desti_abans")]
    public int? TotalDestiAbans { get; set; }

    [Column("total_desti_despres")]
    public int? TotalDestiDespres { get; set; }

    [Column("reservat_desti_abans")]
    public int? ReservatDestiAbans { get; set; }

    [Column("reservat_desti_despres")]
    public int? ReservatDestiDespres { get; set; }

    [Column("motiu")]
    [StringLength(255)]
    public string? Motiu { get; set; }

    [Column("data_moviment", TypeName = "datetime")]
    public DateTime DataMoviment { get; set; }
}
