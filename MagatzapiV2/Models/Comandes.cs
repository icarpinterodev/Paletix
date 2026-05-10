using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("comandes")]
[Index("IdChofer", Name = "fk_chofer_idx")]
[Index("IdClient", Name = "fk_client1_idx")]
[Index("IdEstat", Name = "fk_estat1_idx")]
[Index("IdPreparador", Name = "fk_preparador_idx")]
[Index("IdVehicleTransportista", Name = "fk_vehicle1_idx")]
[Index("DataCreacio", "DataPrevistaEntrega", Name = "idx_comandes_dates")]
public partial class Comandes
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("id_client")]
    public int IdClient { get; set; }

    [Column("id_chofer")]
    public int IdChofer { get; set; }

    [Column("id_preparador")]
    public int IdPreparador { get; set; }

    [Column("id_vehicle_transportista")]
    public int IdVehicleTransportista { get; set; }

    [Column("id_estat")]
    public int IdEstat { get; set; }

    [Column("data_creacio")]
    public DateOnly? DataCreacio { get; set; }

    [Column("notes")]
    [StringLength(500)]
    public string? Notes { get; set; }

    [Column("data_prevista_entrega")]
    public DateOnly DataPrevistaEntrega { get; set; }

    [Column("data_entregat")]
    public DateOnly? DataEntregat { get; set; }

    [Column("poblacio_entrega_alternativa")]
    [StringLength(120)]
    public string? PoblacioEntregaAlternativa { get; set; }

    [Column("adreca_entrega_alternativa")]
    [StringLength(300)]
    public string? AdrecaEntregaAlternativa { get; set; }

    [InverseProperty("IdComandaNavigation")]
    public virtual ICollection<Factures> Factures { get; set; } = new List<Factures>();

    [ForeignKey("IdChofer")]
    [InverseProperty("ComandesIdChoferNavigation")]
    public virtual Usuaris IdChoferNavigation { get; set; } = null!;

    [ForeignKey("IdClient")]
    [InverseProperty("Comandes")]
    public virtual Clients IdClientNavigation { get; set; } = null!;

    [ForeignKey("IdEstat")]
    [InverseProperty("Comandes")]
    public virtual Estats IdEstatNavigation { get; set; } = null!;

    [ForeignKey("IdPreparador")]
    [InverseProperty("ComandesIdPreparadorNavigation")]
    public virtual Usuaris IdPreparadorNavigation { get; set; } = null!;

    [ForeignKey("IdVehicleTransportista")]
    [InverseProperty("Comandes")]
    public virtual Vehicles IdVehicleTransportistaNavigation { get; set; } = null!;

    [InverseProperty("IdComandaNavigation")]
    public virtual ICollection<Liniescomanda> Liniescomanda { get; set; } = new List<Liniescomanda>();
}
