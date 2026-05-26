using Microsoft.EntityFrameworkCore;
using Network_Monitor_API.Models.SystemUsageDBModels;

namespace Network_Monitor_API.Data
{
    public partial class SystemUsageDbContext : DbContext
    {
        public SystemUsageDbContext(DbContextOptions<SystemUsageDbContext> options)
            : base(options)
        {
        }
        public DbSet<SystemUsage> SystemUsages { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SystemUsage>(entity =>
            {
                entity.HasKey(e => e.SystemUsageId);
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.CpuUsage).IsRequired();
                entity.Property(e => e.MemoryUsage).IsRequired();
                entity.Property(e => e.NetworkUsage).IsRequired();
                entity.Property(e => e.ActiveConnections).IsRequired();
            });
        }
    }
}
