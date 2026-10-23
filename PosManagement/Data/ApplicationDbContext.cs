using Microsoft.EntityFrameworkCore;
using PosManagement.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PosManagement.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products => Set<Product>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.ProductId);

                entity.HasIndex(p => p.SKU)
                    .IsUnique();

                entity.HasIndex(p => p.Barcode)
                    .IsUnique()
                    .HasFilter("[Barcode] IS NOT NULL");

                entity.Property(p => p.Name)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(p => p.SKU)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(p => p.CostPrice)
                    .HasPrecision(18, 2);

                entity.Property(p => p.SellingPrice)
                    .HasPrecision(18, 2);
            });
        }
    }
}
