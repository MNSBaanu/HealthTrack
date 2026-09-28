using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Models.Data
{
    public class HealthTrackDbContext : IdentityDbContext<Person>
    {
        public HealthTrackDbContext(DbContextOptions<HealthTrackDbContext> options) : base(options)
        {

        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<HealthMetric> HealthMetrics { get; set; }
        public DbSet<WorkoutLog> WorkoutLogs { get; set; }
        public DbSet<MealLog> MealLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Person>().ToTable("Users");
            builder.Entity<IdentityRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");

            builder.Entity<Person>()
                .HasDiscriminator<string>("Role")
                .HasValue<Client>("Client")
                .HasValue<Admin>("Admin");

            builder.Entity<MealLog>()
                .Property(m => m.MealType)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Entity<Notification>()
                .Property(n => n.Type)
                .HasConversion<string>()
                .HasMaxLength(20);
        }
    }
}
