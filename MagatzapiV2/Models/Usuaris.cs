using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace MagatzapiV2.Models;

/// <summary>
/// Tots els usuaris del sistema es troben en aquesta taula.
/// </summary>
[Table("usuaris")]
[Index("Dni", Name = "dni_UNIQUE", IsUnique = true)]
[Index("Email", Name = "email_UNIQUE", IsUnique = true)]
[Index("IdCarrec", Name = "fk_carrec_idx")]
[Index("IdRol", Name = "fk_rols_idx")]
[Index("NumCompteBancari", Name = "num_compte_bancari_UNIQUE", IsUnique = true)]
[Index("NumSeguretatSocial", Name = "num_seguretat_social_UNIQUE", IsUnique = true)]
[Index("Telefon", Name = "telefon_UNIQUE", IsUnique = true)]
public partial class Usuaris
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nom")]
    [StringLength(120)]
    public string Nom { get; set; } = null!;

    [Column("cognoms")]
    [StringLength(150)]
    public string Cognoms { get; set; } = null!;

    [Column("dni")]
    [StringLength(20)]
    public string Dni { get; set; } = null!;

    [Column("data_naixement")]
    public DateOnly DataNaixement { get; set; }

    [Column("data_contractacio")]
    public DateOnly DataContractacio { get; set; }

    [Column("email")]
    public string Email { get; set; } = null!;

    [Column("telefon")]
    [StringLength(25)]
    public string Telefon { get; set; } = null!;

    [Column("password")]
    [StringLength(256)]
    [JsonIgnore] // prevent serialization of password in API responses
    public string Password { get; set; } = null!;

    [Column("salari")]
    [Precision(10, 2)]
    public decimal Salari { get; set; }

    [Column("torn")]
    public sbyte? Torn { get; set; }

    [Column("num_seguretat_social")]
    [StringLength(15)]
    public string? NumSeguretatSocial { get; set; }

    [Column("num_compte_bancari")]
    [StringLength(24)]
    public string? NumCompteBancari { get; set; }

    [Column("id_carrec")]
    public int IdCarrec { get; set; }

    [Column("id_rol")]
    public int IdRol { get; set; }

    [Column("saldo_punts")]
    public int SaldoPunts { get; set; }

    [Column("nivell")]
    public int Nivell { get; set; }

    [Column("anys_experiencia")]
    public sbyte? AnysExperiencia { get; set; }

    [Column("data_de_creacio")]
    public DateOnly? DataDeCreacio { get; set; }

    [InverseProperty("IdUsuariNavigation")]
    public virtual ICollection<Bescanvis> Bescanvis { get; set; } = new List<Bescanvis>();

    [InverseProperty("IdChoferNavigation")]
    public virtual ICollection<Comandes> ComandesIdChoferNavigation { get; set; } = new List<Comandes>();

    [InverseProperty("IdPreparadorNavigation")]
    public virtual ICollection<Comandes> ComandesIdPreparadorNavigation { get; set; } = new List<Comandes>();

    [ForeignKey("IdCarrec")]
    [InverseProperty("Usuaris")]
    public virtual Carrecs IdCarrecNavigation { get; set; } = null!;

    [ForeignKey("IdRol")]
    [InverseProperty("Usuaris")]
    public virtual Rols IdRolNavigation { get; set; } = null!;

    [InverseProperty("IdUsuariNavigation")]
    public virtual ICollection<RegistreTasques> RegistreTasques { get; set; } = new List<RegistreTasques>();

    [InverseProperty("IdUsuariProposadorNavigation")]
    public virtual ICollection<Reptes> Reptes { get; set; } = new List<Reptes>();

    [InverseProperty("IdUsuariNavigation")]
    public virtual StatsTreballador? StatsTreballador { get; set; }

    [InverseProperty("IdUsuariNavigation")]
    public virtual ICollection<UsuariMedalles> UsuariMedalles { get; set; } = new List<UsuariMedalles>();

    [InverseProperty("IdUsuariGuanyadorNavigation")]
    public virtual ICollection<UsuariReptes> UsuariReptes { get; set; } = new List<UsuariReptes>();
}
