using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("clients")]
[Index("Email", Name = "email")]
[Index("NifEmpresa", Name = "nif_empresa_UNIQUE", IsUnique = true)]
[Index("NomEmpresa", Name = "nom_empresa_UNIQUE", IsUnique = true)]
public partial class Clients
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nom_empresa")]
    public string NomEmpresa { get; set; } = null!;

    [Column("nif_empresa")]
    [StringLength(20)]
    public string? NifEmpresa { get; set; }

    [Column("telefon")]
    [StringLength(25)]
    public string Telefon { get; set; } = null!;

    [Column("email")]
    public string? Email { get; set; }

    [Column("adreca")]
    [StringLength(500)]
    public string Adreca { get; set; } = null!;

    [Column("poblacio")]
    [StringLength(100)]
    public string Poblacio { get; set; } = null!;

    [Column("nom_responsable")]
    [StringLength(255)]
    public string? NomResponsable { get; set; }

    [InverseProperty("IdClientNavigation")]
    public virtual ICollection<Comandes> Comandes { get; set; } = new List<Comandes>();

    [InverseProperty("IdClientNavigation")]
    public virtual ICollection<Factures> Factures { get; set; } = new List<Factures>();
}
