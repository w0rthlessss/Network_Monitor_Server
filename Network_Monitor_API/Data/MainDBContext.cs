using Microsoft.EntityFrameworkCore;
using Network_Monitor_API.Models.MainDBModels;

namespace Network_Monitor_API.Data
{
    public partial class MainDBContext : DbContext
    {
        public MainDBContext(DbContextOptions<MainDBContext> options) : base(options) { }

        public DbSet<Connection> Connections { get; set; } = null!;
        public DbSet<Alert> Alerts { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Model> Models { get; set; } = null!;
        public DbSet<Prediction> Predictions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Connection>(e =>
            {
                e.HasKey(e => e.Id);
                e.Property(c => c.Timestamp).IsRequired();
                e.Property(c => c.SrcIP).IsRequired();
                e.Property(c => c.DstIP).IsRequired();
                e.Property(c => c.SrcPort).IsRequired();
                e.Property(c => c.DstPort).IsRequired();
                e.Property(c => c.Protocol).IsRequired();
                e.Property(c => c.Service).IsRequired();
                e.Property(c => c.Duration).IsRequired();
                e.Property(c => c.SrcBytes).IsRequired();
                e.Property(c => c.DstBytes).IsRequired();
                e.Property(c => c.Traits).IsRequired();
            });

            modelBuilder.Entity<Alert>(e =>
            {
                e.HasKey(e => e.Id);
                e.Property(a => a.Timestamp).IsRequired();
                e.Property(a => a.Description).IsRequired();
                e.Property(a => a.Resolved).HasDefaultValue(false);
                e.HasOne(a => a.Connection)
                    .WithMany(c => c.Alerts)
                    .HasForeignKey(a => a.ConnectionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<User>(e =>
            {
                e.HasKey(e => e.Id);
                e.Property(u => u.Login).IsRequired();
                e.Property(u => u.PasswordHash).IsRequired();
                e.Property(u => u.Role).IsRequired();
                e.Property(u => u.Status).IsRequired();
                e.Property(u => u.Role).IsRequired();
            });

            modelBuilder.Entity<Model>(e =>
            {
                e.HasKey(e => e.Id);
                e.Property(m => m.Metrics).IsRequired();
                e.Property(m => m.ModelPath).IsRequired();
                e.Property(m => m.IsActive).HasDefaultValue(false);
                e.HasOne(m => m.User)
                    .WithMany(u => u.Models)
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Prediction>(e =>
            {
                e.HasKey(e => e.Id);
                e.Property(p => p.Timestamp).IsRequired();
                e.Property(p => p.Result).IsRequired();
                e.HasOne(p => p.Model)
                    .WithMany(m => m.Predictions)
                    .HasForeignKey(p => p.ModelId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(p => p.Connection)
                    .WithMany(c => c.Predictions)
                    .HasForeignKey(p => p.ConnectionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}
