using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("pagaments")]
[Index("IdFactura", Name = "fk_factura_idx")]
public partial class Pagaments
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("id_factura")]
    public int IdFactura { get; set; }

    [Column("import")]
    [Precision(10, 2)]
    public decimal Import { get; set; }

    [Column("metode")]
    [StringLength(15)]
    public string Metode { get; set; } = null!;

    [Column("data", TypeName = "datetime")]
    public DateTime Data { get; set; }

    [ForeignKey("IdFactura")]
    [InverseProperty("Pagaments")]
    public virtual Factures IdFacturaNavigation { get; set; } = null!;
}
