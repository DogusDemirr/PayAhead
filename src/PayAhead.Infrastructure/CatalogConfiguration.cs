using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayAhead.Domain;
namespace PayAhead.Infrastructure;

public sealed class CatalogTranslationConfiguration : IEntityTypeConfiguration<CatalogCategoryTranslation>
{
    public void Configure(EntityTypeBuilder<CatalogCategoryTranslation> b)
    {
        b.ToTable("CatalogCategoryTranslations"); b.HasKey(x => new { x.CategoryId, x.CultureCode });
        b.Property(x => x.CultureCode).HasMaxLength(16).IsUnicode(false).IsRequired();
        b.Property(x => x.DisplayName).HasMaxLength(150).IsRequired(); b.Property(x => x.SearchText).HasMaxLength(500).IsRequired();
        b.HasOne<CatalogCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => new { x.CultureCode, x.SearchText }); b.HasData(CatalogTranslationSeed.All);
    }
}

public sealed class CatalogCategoryConfiguration : IEntityTypeConfiguration<CatalogCategory>
{
    public void Configure(EntityTypeBuilder<CatalogCategory> b)
    {
        b.ToTable("CatalogCategories"); b.HasKey(x => x.Id); b.Property(x => x.Code).HasMaxLength(80).IsUnicode(false).IsRequired(); b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.NameTr).HasMaxLength(50).IsRequired(); b.Property(x => x.NameEn).HasMaxLength(100).IsRequired(); b.Property(x => x.IconKey).HasMaxLength(40).IsRequired(); b.Property(x => x.SearchText).HasMaxLength(500).IsRequired();
        b.HasOne<CatalogCategory>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.NoAction); b.HasIndex(x => new { x.IsActive, x.SortOrder }); b.HasData(CatalogSeed.Categories);
    }
}
public sealed class PaymentCatalogConfiguration : IEntityTypeConfiguration<PaymentPlan>
{
    public void Configure(EntityTypeBuilder<PaymentPlan> b)
    {
        b.HasOne<CatalogCategory>().WithMany().HasForeignKey(x => x.CatalogCategoryId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<CatalogService>().WithMany().HasForeignKey(x => x.CatalogServiceId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<CatalogServiceCategory>().WithMany().HasForeignKey(x => new { x.CatalogServiceId, x.CatalogCategoryId }).OnDelete(DeleteBehavior.NoAction);
    }
}
public sealed class CatalogServiceConfiguration : IEntityTypeConfiguration<CatalogService>
{
    public void Configure(EntityTypeBuilder<CatalogService> b)
    {
        b.ToTable("CatalogServices"); b.HasKey(x => x.Id); b.Property(x => x.Code).HasMaxLength(80).IsUnicode(false).IsRequired(); b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.DisplayName).HasMaxLength(150).IsRequired(); b.Property(x => x.LogoKey).HasMaxLength(80); b.Property(x => x.SearchKeywords).HasMaxLength(500).IsRequired(); b.Property(x => x.SearchText).HasMaxLength(1000).IsRequired(); b.Property(x => x.CountryCode).HasMaxLength(2).IsUnicode(false);
        b.HasOne<CatalogCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.NoAction); b.HasIndex(x => new { x.IsActive, x.IsPopular, x.SortOrder }); b.HasData(CatalogSeed.Services);
    }
}
public sealed class CatalogServiceCategoryConfiguration : IEntityTypeConfiguration<CatalogServiceCategory>
{
    public void Configure(EntityTypeBuilder<CatalogServiceCategory> b)
    {
        b.ToTable("CatalogServiceCategories"); b.HasKey(x => new { x.ServiceId, x.CategoryId }); b.HasOne<CatalogService>().WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.NoAction); b.HasOne<CatalogCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.NoAction); b.HasIndex(x => x.CategoryId); b.HasData(CatalogSeed.Links);
    }
}
