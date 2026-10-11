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
        public DbSet<ConsultationProvider> ConsultationProviders { get; set; }
        public DbSet<AvailabilitySlot> AvailabilitySlots { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ConsultationProvider>().HasOne(p => p.Person).WithMany()
                .HasForeignKey(p => p.PersonId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<ConsultationProvider>().HasIndex(p => p.PersonId)
                .IsUnique().HasFilter("[PersonId] IS NOT NULL");
            builder.Entity<AvailabilitySlot>().HasOne(s => s.Provider).WithMany(p => p.Slots)
                .HasForeignKey(s => s.ProviderId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<AvailabilitySlot>().HasIndex(s => new { s.ProviderId, s.StartUtc, s.EndUtc });
            builder.Entity<AvailabilitySlot>().ToTable("AvailabilitySlots", table =>
                table.HasCheckConstraint("CK_AvailabilitySlots_TimeRange", "[EndUtc] > [StartUtc]"));
            builder.Entity<Appointment>().HasOne(a => a.Slot).WithMany(s => s.Appointments)
                .HasForeignKey(a => a.SlotId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Appointment>().HasOne(a => a.Client).WithMany()
                .HasForeignKey(a => a.ClientId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Appointment>().HasIndex(a => new { a.ClientId, a.Status });
            builder.Entity<Appointment>().HasIndex(a => a.SlotId).IsUnique()
                .HasFilter("[Status] <> 2");
            builder.Entity<Appointment>().ToTable("Appointments", table =>
                table.HasCheckConstraint("CK_Appointments_Status", "[Status] IN (0, 1, 2, 3)"));

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
