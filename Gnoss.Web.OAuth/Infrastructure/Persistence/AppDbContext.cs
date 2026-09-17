using Gnoss.Web.OAuth.Domain.Entities;
using Gnoss.Web.OAuth.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Gnoss.Web.OAuth.Infrastructure.Persistence
{
    public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<OAuthToken> OAuthTokens => Set<OAuthToken>();
        public DbSet<OAuthConsumer> OAuthConsumers => Set<OAuthConsumer>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new OAuthTokenConfiguration());
            modelBuilder.ApplyConfiguration(new OAuthConsumerConfiguration());
        }
    }
}
