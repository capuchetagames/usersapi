using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Repository.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);
        builder.Property(x=>x.Id).HasColumnType("INT").ValueGeneratedNever().UseIdentityColumn();
        builder.Property(x => x.Name).HasColumnType("VARCHAR(100)").IsRequired();
        builder.Property(x=>x.Email).HasColumnType("VARCHAR(100)").IsRequired();
        builder.Property(x => x.Password).HasColumnType("VARCHAR(100)").IsRequired();
        builder.Property(x=>x.Permission).HasColumnType("VARCHAR(10)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("DATETIME").IsRequired();
    }
}