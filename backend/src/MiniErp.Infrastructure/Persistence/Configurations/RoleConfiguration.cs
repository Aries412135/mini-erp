using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniErp.Domain.Constants;
using MiniErp.Domain.Entities;

namespace MiniErp.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Name).IsRequired().HasMaxLength(50);
        builder.HasIndex(r => r.Name).IsUnique();

        builder.HasData(
            new Role(1, RoleNames.Admin),
            new Role(2, RoleNames.Purchasing),
            new Role(3, RoleNames.Warehouse),
            new Role(4, RoleNames.Sales),
            new Role(5, RoleNames.Finance),
            new Role(6, RoleNames.Manager));
    }
}