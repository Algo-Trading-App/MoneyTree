using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Messaging.Models
{
    public partial class userDBContext : DbContext
    {
        public userDBContext()
        {
        }

        public userDBContext(DbContextOptions<userDBContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Holding> Holding { get; set; }
        public virtual DbSet<Portfolio> Portfolio { get; set; }
        public virtual DbSet<PortfolioActions> PortfolioActions { get; set; }
        public virtual DbSet<PortfolioHistory> PortfolioHistory { get; set; }
        public virtual DbSet<User> User { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=moneytree.levrum.com,25002;Database=userDB;User ID=sa; Password=Capstone2020!;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Holding>(entity =>
            {
                entity.Property(e => e.Abbreviation)
                    .IsRequired()
                    .HasMaxLength(16);

                entity.Property(e => e.Description).HasMaxLength(256);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.HasOne(d => d.Portfolio)
                    .WithMany(p => p.Holding)
                    .HasForeignKey(d => d.PortfolioId)
                    .HasConstraintName("FK__Holding__Portfol__14270015");
            });

            modelBuilder.Entity<Portfolio>(entity =>
            {
                entity.Property(e => e.InitialValue).HasColumnType("money");

                entity.Property(e => e.StopValue).HasColumnType("money");

                entity.HasOne(d => d.Owner)
                    .WithMany(p => p.Portfolio)
                    .HasForeignKey(d => d.OwnerId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK__Portfolio__Owner__151B244E");
            });

            modelBuilder.Entity<PortfolioActions>(entity =>
            {
                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(1);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(64);
            });

            modelBuilder.Entity<PortfolioHistory>(entity =>
            {
                entity.Property(e => e.Valuation).HasColumnType("money");

                entity.HasOne(d => d.ActionTaken)
                    .WithMany(p => p.PortfolioHistory)
                    .HasForeignKey(d => d.ActionTakenId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Portfolio__Actio__2F10007B");

                entity.HasOne(d => d.Portfolio)
                    .WithMany(p => p.PortfolioHistory)
                    .HasForeignKey(d => d.PortfolioId)
                    .HasConstraintName("FK__Portfolio__Portf__03F0984C");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email)
                    .HasName("UQ__User__A9D10534FDC00C59")
                    .IsUnique();

                entity.Property(e => e.BrokerageAccount).HasMaxLength(64);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(64);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
