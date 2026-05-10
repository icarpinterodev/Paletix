using System;
using System.Collections.Generic;
using MagatzapiV2.Models;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Bescanvis> Bescanvis { get; set; }

    public virtual DbSet<Carrecs> Carrecs { get; set; }

    public virtual DbSet<Clients> Clients { get; set; }

    public virtual DbSet<Comandes> Comandes { get; set; }

    public virtual DbSet<Estats> Estats { get; set; }

    public virtual DbSet<Factures> Factures { get; set; }

    public virtual DbSet<Liniescomanda> Liniescomanda { get; set; }

    public virtual DbSet<Medalles> Medalles { get; set; }

    public virtual DbSet<Pagaments> Pagaments { get; set; }

    public virtual DbSet<Premis> Premis { get; set; }

    public virtual DbSet<Productes> Productes { get; set; }

    public virtual DbSet<Proveidors> Proveidors { get; set; }

    public virtual DbSet<ProveidorsLot> ProveidorsLot { get; set; }

    public virtual DbSet<RegistreTasques> RegistreTasques { get; set; }

    public virtual DbSet<Reptes> Reptes { get; set; }

    public virtual DbSet<Rols> Rols { get; set; }

    public virtual DbSet<StatsTreballador> StatsTreballador { get; set; }

    public virtual DbSet<Stock> Stock { get; set; }

    public virtual DbSet<StockMoviments> StockMoviments { get; set; }

    public virtual DbSet<Tasques> Tasques { get; set; }

    public virtual DbSet<TipusProducte> TipusProducte { get; set; }

    public virtual DbSet<TipusVehicles> TipusVehicles { get; set; }

    public virtual DbSet<Ubicacions> Ubicacions { get; set; }

    public virtual DbSet<UsuariMedalles> UsuariMedalles { get; set; }

    public virtual DbSet<UsuariReptes> UsuariReptes { get; set; }

    public virtual DbSet<Usuaris> Usuaris { get; set; }

    public virtual DbSet<Vehicles> Vehicles { get; set; }

    public virtual DbSet<Zones> Zones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Bescanvis>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.DataBescanviat).HasDefaultValueSql("curdate()");

            entity.HasOne(d => d.IdPremiNavigation).WithMany(p => p.Bescanvis)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_bescanvis_premi");

            entity.HasOne(d => d.IdUsuariNavigation).WithMany(p => p.Bescanvis)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_bescanvis_idusuari");
        });

        modelBuilder.Entity<Carrecs>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
        });

        modelBuilder.Entity<Clients>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
        });

        modelBuilder.Entity<Comandes>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.DataCreacio).HasDefaultValueSql("curdate()");

            entity.HasOne(d => d.IdChoferNavigation).WithMany(p => p.ComandesIdChoferNavigation)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_chofer1");

            entity.HasOne(d => d.IdClientNavigation).WithMany(p => p.Comandes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_client1");

            entity.HasOne(d => d.IdEstatNavigation).WithMany(p => p.Comandes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_estat1");

            entity.HasOne(d => d.IdPreparadorNavigation).WithMany(p => p.ComandesIdPreparadorNavigation)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_preparador1");

            entity.HasOne(d => d.IdVehicleTransportistaNavigation).WithMany(p => p.Comandes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_vehicle1");
        });

        modelBuilder.Entity<Estats>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
        });

        modelBuilder.Entity<Factures>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.DataEmissio).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ImpostPercentatge).HasDefaultValueSql("'21.00'");
            entity.Property(e => e.Impostos).HasComputedColumnSql("`total` * (`impost_percentatge` / 100)", false);

            entity.HasOne(d => d.IdClientNavigation).WithMany(p => p.Factures)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_client2");

            entity.HasOne(d => d.IdComandaNavigation).WithMany(p => p.Factures)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_comanda");

            entity.HasOne(d => d.IdEstatNavigation).WithMany(p => p.Factures)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_estat2");
        });

        modelBuilder.Entity<Liniescomanda>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasOne(d => d.IdComandaNavigation).WithMany(p => p.Liniescomanda)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_comandes");

            entity.HasOne(d => d.IdEstatVerificacioNavigation).WithMany(p => p.Liniescomanda).HasConstraintName("fk_estats1");

            entity.HasOne(d => d.IdProducteNavigation).WithMany(p => p.Liniescomanda)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_productes");

            entity.HasOne(d => d.IdUbicacioNavigation).WithMany(p => p.Liniescomanda)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_liniescomanda_ubicacio");
        });

        modelBuilder.Entity<Medalles>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
        });

        modelBuilder.Entity<Pagaments>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Data).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.IdFacturaNavigation).WithMany(p => p.Pagaments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_factura");
        });

        modelBuilder.Entity<Premis>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
        });

        modelBuilder.Entity<Productes>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Actiu).HasDefaultValueSql("'1'");
            entity.Property(e => e.DataAfegit).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.IdProveidorNavigation).WithMany(p => p.Productes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_producte_proveidor");

            entity.HasOne(d => d.IdTipusNavigation).WithMany(p => p.Productes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_producte_tipus");

            entity.HasOne(d => d.IdUbicacioNavigation).WithMany(p => p.Productes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_producte_ubicacio");
        });

        modelBuilder.Entity<Proveidors>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasOne(d => d.IdTipusProductePrincipalNavigation).WithMany(p => p.Proveidors).HasConstraintName("fk_id_tipus_producte_principal");
        });

        modelBuilder.Entity<ProveidorsLot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasOne(d => d.IdProducteNavigation).WithMany(p => p.ProveidorsLot)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_pvlot_producte");

            entity.HasOne(d => d.IdProveidorNavigation).WithMany(p => p.ProveidorsLot)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_proveidor");
        });

        modelBuilder.Entity<RegistreTasques>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Data).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.MinutsEmprats).HasDefaultValueSql("'0'");

            entity.HasOne(d => d.IdTascaNavigation).WithMany(p => p.RegistreTasques)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_premi00");

            entity.HasOne(d => d.IdUsuariNavigation).WithMany(p => p.RegistreTasques)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_usuari00");
        });

        modelBuilder.Entity<Reptes>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.DataCreacio).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.IdUsuariProposadorNavigation).WithMany(p => p.Reptes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_reptes_idusuari");
        });

        modelBuilder.Entity<Rols>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
        });

        modelBuilder.Entity<StatsTreballador>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.DataUltimRegistre).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.IdUsuariNavigation).WithOne(p => p.StatsTreballador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_statstreballador_id_usuari");
        });

        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Disponibles).HasComputedColumnSql("`totals_en_stock` - `reservats_per_comandes`", true);

            entity.HasOne(d => d.IdLotNavigation).WithMany(p => p.Stock).HasConstraintName("fk_lot");

            entity.HasOne(d => d.IdProducteNavigation).WithMany(p => p.Stock)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_Stock_producte");

            entity.HasOne(d => d.IdUbicacioNavigation).WithMany(p => p.Stock)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_Stock_ubicacio");
        });

        modelBuilder.Entity<StockMoviments>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.DataMoviment).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Tasques>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
        });

        modelBuilder.Entity<TipusProducte>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
        });

        modelBuilder.Entity<TipusVehicles>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
        });

        modelBuilder.Entity<Ubicacions>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.CodiGenerat).HasComputedColumnSql("concat(_utf8mb4'Z',`zona`,_utf8mb4'P',`passadis`,_utf8mb4'-E',`bloc_estanteria`,_utf8mb4'_X',`fila`,_utf8mb4'-Y',`columna`)", true);
        });

        modelBuilder.Entity<UsuariMedalles>(entity =>
        {
            entity.HasKey(e => e.Registre).HasName("PRIMARY");

            entity.Property(e => e.DataObtencio).HasDefaultValueSql("curdate()");

            entity.HasOne(d => d.IdMedallaNavigation).WithMany(p => p.UsuariMedalles).HasConstraintName("fk_medalles");

            entity.HasOne(d => d.IdUsuariNavigation).WithMany(p => p.UsuariMedalles).HasConstraintName("fk_medalles_idusuari");
        });

        modelBuilder.Entity<UsuariReptes>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("usuari_reptes", tb => tb.HasComment("Reptes proposats per l'administració o els propis treballadors, a canvi d"));

            entity.Property(e => e.DataCompletat).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.IdRepteNavigation).WithMany(p => p.UsuariReptes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_premi0");

            entity.HasOne(d => d.IdUsuariGuanyadorNavigation).WithMany(p => p.UsuariReptes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_usuari0");
        });

        modelBuilder.Entity<Usuaris>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("usuaris", tb => tb.HasComment("Tots els usuaris del sistema es troben en aquesta taula."));

            entity.Property(e => e.DataDeCreacio).HasDefaultValueSql("curdate()");

            entity.HasOne(d => d.IdCarrecNavigation).WithMany(p => p.Usuaris)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_carrec");

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.Usuaris)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_rols");
        });

        modelBuilder.Entity<Vehicles>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.EsElectric).HasDefaultValueSql("'0'");
            entity.Property(e => e.UltimRegistreKilometratge).HasDefaultValueSql("curdate()");

            entity.HasOne(d => d.IdTipusVehicleNavigation).WithMany(p => p.Vehicles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_tipusvehicle1");
        });

        modelBuilder.Entity<Zones>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
