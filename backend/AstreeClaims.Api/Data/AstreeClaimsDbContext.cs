using System;
using System.Collections.Generic;
using AstreeClaims.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AstreeClaims.Api.Data;

public partial class AstreeClaimsDbContext : DbContext
{
    public AstreeClaimsDbContext(DbContextOptions<AstreeClaimsDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<Contrat> Contrats { get; set; }

    public virtual DbSet<GenerationLog> GenerationLogs { get; set; }

    public virtual DbSet<Sinistre> Sinistres { get; set; }

    public virtual DbSet<Vehicule> Vehicules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.ClientId).HasName("PK__Clients__E67E1A248EDEA280");

            entity.Property(e => e.ClientId).HasMaxLength(20);
            entity.Property(e => e.Gouvernorat).HasMaxLength(100);
            entity.Property(e => e.Nom).HasMaxLength(100);
            entity.Property(e => e.Prenom).HasMaxLength(100);
        });

        modelBuilder.Entity<Contrat>(entity =>
        {
            entity.HasKey(e => e.ContractId).HasName("PK__Contrats__C90D34693107D3A9");

            entity.Property(e => e.ContractId).HasMaxLength(20);
            entity.Property(e => e.ClientId).HasMaxLength(20);
            entity.Property(e => e.TypeCouverture).HasMaxLength(100);

            entity.HasOne(d => d.Client).WithMany(p => p.Contrats)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Contrats_Clients");
        });

        modelBuilder.Entity<GenerationLog>(entity =>
        {
            entity.HasKey(e => e.GenerationId).HasName("PK__Generati__AFCC8A5A5DE9E9A2");

            entity.HasIndex(e => new { e.ClaimId, e.CreatedAt }, "IX_GenerationLogs_ClaimId_CreatedAt").IsDescending(false, true);

            entity.Property(e => e.GenerationId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ClaimId).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.GenerationType).HasMaxLength(30);
            entity.Property(e => e.ModelName).HasMaxLength(100);
            entity.Property(e => e.PromptVersion)
                .HasMaxLength(20)
                .HasDefaultValue("1.0");

            entity.HasOne(d => d.Claim).WithMany(p => p.GenerationLogs)
                .HasForeignKey(d => d.ClaimId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GenerationLogs_Sinistres");
        });

        modelBuilder.Entity<Sinistre>(entity =>
        {
            entity.HasKey(e => e.ClaimId).HasName("PK__Sinistre__EF2E139BB29D8226");

            entity.HasIndex(e => e.DateSinistre, "IX_Sinistres_DateSinistre");

            entity.HasIndex(e => e.Statut, "IX_Sinistres_Statut");

            entity.Property(e => e.ClaimId).HasMaxLength(20);
            entity.Property(e => e.ClientId).HasMaxLength(20);
            entity.Property(e => e.ContractId).HasMaxLength(20);
            entity.Property(e => e.MontantEstime).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MontantIndemnisation).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Statut).HasMaxLength(50);
            entity.Property(e => e.TypeSinistre).HasMaxLength(100);
            entity.Property(e => e.VehicleId).HasMaxLength(20);

            entity.HasOne(d => d.Client).WithMany(p => p.Sinistres)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sinistres_Clients");

            entity.HasOne(d => d.Contract).WithMany(p => p.Sinistres)
                .HasForeignKey(d => d.ContractId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sinistres_Contrats");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.Sinistres)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sinistres_Vehicules");
        });

        modelBuilder.Entity<Vehicule>(entity =>
        {
            entity.HasKey(e => e.VehicleId).HasName("PK__Vehicule__476B54923DA50B6C");

            entity.HasIndex(e => e.ContractId, "UQ__Vehicule__C90D346808B5B231").IsUnique();

            entity.Property(e => e.VehicleId).HasMaxLength(20);
            entity.Property(e => e.ContractId).HasMaxLength(20);
            entity.Property(e => e.Immatriculation).HasMaxLength(30);
            entity.Property(e => e.Marque).HasMaxLength(50);
            entity.Property(e => e.Modele).HasMaxLength(100);
            entity.Property(e => e.TypeVehicule).HasMaxLength(50);

            entity.HasOne(d => d.Contract).WithOne(p => p.Vehicule)
                .HasForeignKey<Vehicule>(d => d.ContractId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Vehicules_Contrats");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
