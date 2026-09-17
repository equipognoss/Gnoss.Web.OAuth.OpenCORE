using Gnoss.Web.OAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gnoss.Web.OAuth.Infrastructure.Persistence.Configurations
{
    public sealed class OAuthTokenConfiguration : IEntityTypeConfiguration<OAuthToken>
    {
        public void Configure(EntityTypeBuilder<OAuthToken> builder)
        {
            builder.ToTable("OAuthToken");
            builder.HasKey(t => t.TokenId);
            builder.Property(t => t.TokenId).ValueGeneratedOnAdd();
            builder.Property(t => t.Token).IsRequired().HasMaxLength(50);
            builder.Property(t => t.TokenSecret).IsRequired().HasMaxLength(50);
            builder.HasIndex(t => t.Token);
            builder.HasIndex(t => new { t.Token, t.ConsumerId });
        }
    }
}
