using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UsersApi.Domain.Entities;

namespace UsersApi.Data.Mappings;

public class UserMapping : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PublicId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(300).IsRequired();

        // Enum gravado como texto ("Admin") para ficar legível no pgAdmin.
        builder.Property(x => x.Profile).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(x => x.PublicId).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();
    }
}
