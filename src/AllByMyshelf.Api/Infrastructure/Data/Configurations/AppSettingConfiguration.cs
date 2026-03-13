using AllByMyshelf.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AllByMyshelf.Api.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="AppSetting"/> entity.
/// </summary>
public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("app_settings");

        builder.HasKey(a => a.Key);

        builder.Property(a => a.Key)
            .HasColumnName("key")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(a => a.Value)
            .HasColumnName("value")
            .HasMaxLength(2000)
            .IsRequired();
    }
}
