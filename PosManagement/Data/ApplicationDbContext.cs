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

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Sale> Sales => Set<Sale>();

        public DbSet<SaleItem> SaleItems => Set<SaleItem>();
        public DbSet<StockMovement> StockMovements => Set<StockMovement>();
        public DbSet<SaleReturn> SaleReturns => Set<SaleReturn>();

        public DbSet<SaleReturnItem> SaleReturnItems => Set<SaleReturnItem>();
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

                entity.HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.Property(p => p.RowVersion)
                    .IsRowVersion();
                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_Products_StockQuantity_NonNegative",
                    "[StockQuantity] >= 0"));
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.CategoryId);

                entity.HasIndex(c => c.Name)
                    .IsUnique();

                entity.Property(c => c.Name)
                    .HasMaxLength(100)
                    .IsRequired();
            });
            modelBuilder.Entity<Sale>(entity => {
                entity.HasIndex(s => s.InvoiceNumber)
                    .IsUnique();
                entity.Property(s => s.Subtotal)
                .HasPrecision(18, 2);

                entity.Property(s => s.Discount)
                    .HasPrecision(18, 2);

                entity.Property(s => s.Total)
                    .HasPrecision(18, 2);

                entity.Property(s => s.AmountReceived)
                    .HasPrecision(18, 2);

                entity.Property(s => s.ChangeAmount)
                    .HasPrecision(18, 2);

                entity.HasMany(s => s.Items)
                .WithOne(i => i.Sale)
                .HasForeignKey(i => i.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

                entity.Property(s => s.TaxRate)
                    .HasPrecision(5, 2);

                entity.Property(s => s.TaxAmount)
                    .HasPrecision(18, 2);

            });
            modelBuilder.Entity<SaleItem>(entity => {
                entity.Property(i => i.UnitPrice)
                      .HasPrecision(18, 2);

                entity.Property(i => i.LineTotal)
                    .HasPrecision(18, 2);

                entity.HasOne(i => i.Product)
                    .WithMany()
                    .HasForeignKey(i => i.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<StockMovement>(entity => {
                entity.HasOne(m => m.Product)
                    .WithMany()
                    .HasForeignKey(m => m.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(m => m.MovementType)
                    .HasMaxLength(30);

                entity.Property(m => m.Notes)
                    .HasMaxLength(500);

                entity.HasOne(sm => sm.Sale)
                    .WithMany()
                    .HasForeignKey(sm => sm.SaleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<SaleReturn>(entity =>
            {
                entity.HasOne(r => r.Sale)
                    .WithMany()
                    .HasForeignKey(r => r.SaleId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(r => r.RefundAmount)
                .HasPrecision(18, 2);

                entity.Property(r => r.RefundMethod)
                    .HasMaxLength(30);

                entity.Property(r => r.Reason)
                    .HasMaxLength(500);

                entity.HasMany(r => r.Items)
                    .WithOne(i => i.SaleReturn)
                    .HasForeignKey(i => i.SaleReturnId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<SaleReturnItem>(entity =>
            {
                entity.HasOne(i => i.SaleItem)
                .WithMany()
                .HasForeignKey(i => i.SaleItemId)
                .OnDelete(DeleteBehavior.Restrict);

                entity.Property(i => i.UnitPrice)
                    .HasPrecision(18, 2);

                entity.Property(i => i.RefundAmount)
                    .HasPrecision(18, 2);
            });            
        }
    }
}
