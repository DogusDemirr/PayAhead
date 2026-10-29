using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayAhead.Domain;

namespace PayAhead.Infrastructure;
public sealed class PasswordHistoryConfiguration : IEntityTypeConfiguration<PasswordHistoryEntry>
{
    public void Configure(EntityTypeBuilder<PasswordHistoryEntry> b)
    {
        b.ToTable("PasswordHistory"); b.HasKey(x => x.Id); b.Property(x => x.PasswordHash).HasMaxLength(1024).IsRequired();
        b.HasIndex(x => new { x.UserId, x.NormalChangedAtUtc });
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class LegalReceiptConfiguration : IEntityTypeConfiguration<LegalNoticeReceipt>
{
    public void Configure(EntityTypeBuilder<LegalNoticeReceipt> b)
    {
        b.ToTable("LegalNoticeReceipts"); b.HasKey(x => x.UserId); b.Property(x => x.NoticeVersion).HasMaxLength(64).IsUnicode(false);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class RegistrationLegalConfiguration : IEntityTypeConfiguration<RegistrationLegalConfirmation>
{
    public void Configure(EntityTypeBuilder<RegistrationLegalConfirmation> b)
    {
        b.ToTable("RegistrationLegalConfirmations"); b.HasKey(x => new { x.UserId, x.DocumentId });
        b.Property(x => x.DocumentId).HasMaxLength(16).IsUnicode(false);
        b.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32).IsUnicode(false);
        b.Property(x => x.DocumentVersion).HasMaxLength(64).IsUnicode(false).IsRequired();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
