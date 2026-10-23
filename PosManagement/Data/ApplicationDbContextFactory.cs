using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Text;

namespace PosManagement.Data
{
    public class ApplicationDbContextFactory
    : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder =
                new DbContextOptionsBuilder<ApplicationDbContext>();

            optionsBuilder.UseSqlServer(
                @"Server=DESKTOP-94E2O8D;Database=PosManagementDb;Trusted_Connection=True;TrustServerCertificate=True;");

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
