using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("proveidors")]
[Index("IdTipusProductePrincipal", Name = "fk_id_tipus_producte_principal")]
[Index("NomEmpresa", Name = "nom_empresa_UNIQUE", IsUnique = true)]
public partial class Proveidors
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("marca_matriu")]
    [StringLength(100)]
    public string? MarcaMatriu { get; set; }

    [Column("nom_empresa")]
    [StringLength(100)]
    public string NomEmpresa { get; set; } = null!;

    [Column("telefon")]
    [StringLength(16)]
    public string Telefon { get; set; } = null!;

    [Column("email")]
    [StringLength(200)]
    public string Email { get; set; } = null!;

    [Column("adreca", TypeName = "text")]
    public string? Adreca { get; set; }

    [Column("url_web")]
    [StringLength(2048)]
    public string? UrlWeb { get; set; }

    [Column("id_tipus_producte_principal")]
    public int? IdTipusProductePrincipal { get; set; }

    [ForeignKey("IdTipusProductePrincipal")]
    [InverseProperty("Proveidors")]
    public virtual TipusProducte? IdTipusProductePrincipalNavigation { get; set; }

    [InverseProperty("IdProveidorNavigation")]
    public virtual ICollection<Productes> Productes { get; set; } = new List<Productes>();

    [InverseProperty("IdProveidorNavigation")]
    public virtual ICollection<ProveidorsLot> ProveidorsLot { get; set; } = new List<ProveidorsLot>();
}
