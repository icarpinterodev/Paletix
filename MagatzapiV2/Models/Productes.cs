using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("productes")]
[Index("IdProveidor", Name = "FK_producte_proveidor")]
[Index("IdTipus", Name = "FK_producte_tipus")]
[Index("IdUbicacio", Name = "FK_producte_ubicacio")]
[Index("Referencia", Name = "referencia_UNIQUE", IsUnique = true)]
public partial class Productes
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("referencia")]
    [StringLength(50)]
    public string? Referencia { get; set; }

    [Column("nom")]
    [StringLength(150)]
    public string Nom { get; set; } = null!;

    [Column("descripcio")]
    [StringLength(300)]
    public string? Descripcio { get; set; }

    [Column("id_tipus")]
    public int IdTipus { get; set; }

    [Column("volum_ml")]
    [Precision(10, 2)]
    public decimal? VolumMl { get; set; }

    [Column("id_proveidor")]
    public int IdProveidor { get; set; }

    [Column("id_ubicacio")]
    public int IdUbicacio { get; set; }

    [Column("caixes_per_palet")]
    public int CaixesPerPalet { get; set; }

    [Column("imatge_url")]
    [StringLength(2048)]
    public string? ImatgeUrl { get; set; }

    [Column("actiu")]
    public sbyte Actiu { get; set; }

    [Column("preu_venda_caixa")]
    [Precision(10, 2)]
    public decimal PreuVendaCaixa { get; set; }

    [Column("cost_per_caixa")]
    [Precision(10, 2)]
    public decimal CostPerCaixa { get; set; }

    [Column("estabilitat_al_palet")]
    public int? EstabilitatAlPalet { get; set; }

    [Column("pes_kg")]
    [Precision(10, 2)]
    public decimal? PesKg { get; set; }

    [Column("data_afegit", TypeName = "datetime")]
    public DateTime? DataAfegit { get; set; }

    [ForeignKey("IdProveidor")]
    [InverseProperty("Productes")]
    public virtual Proveidors IdProveidorNavigation { get; set; } = null!;

    [ForeignKey("IdTipus")]
    [InverseProperty("Productes")]
    public virtual TipusProducte IdTipusNavigation { get; set; } = null!;

    [ForeignKey("IdUbicacio")]
    [InverseProperty("Productes")]
    public virtual Ubicacions IdUbicacioNavigation { get; set; } = null!;

    [InverseProperty("IdProducteNavigation")]
    public virtual ICollection<Liniescomanda> Liniescomanda { get; set; } = new List<Liniescomanda>();

    [InverseProperty("IdProducteNavigation")]
    public virtual ICollection<ProveidorsLot> ProveidorsLot { get; set; } = new List<ProveidorsLot>();

    [InverseProperty("IdProducteNavigation")]
    public virtual ICollection<Stock> Stock { get; set; } = new List<Stock>();
}
