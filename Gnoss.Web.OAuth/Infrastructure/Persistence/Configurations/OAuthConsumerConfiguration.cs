using Gnoss.Web.OAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gnoss.Web.OAuth.Infrastructure.Persistence.Configurations
{
    public sealed class OAuthConsumerConfiguration : IEntityTypeConfiguration<OAuthConsumer>
    {
        public void Configure(EntityTypeBuilder<OAuthConsumer> builder)
        {
            builder.ToTable("OAuthConsumer");
            builder.HasKey(c => c.ConsumerId);
            builder.Property(c => c.ConsumerId).ValueGeneratedOnAdd();
            builder.Property(c => c.ConsumerKey).IsRequired().HasMaxLength(50);
            builder.Property(c => c.ConsumerSecret).IsRequired().HasMaxLength(50);
            builder.HasIndex(c => c.ConsumerKey).IsUnique();
        }
    }
}
