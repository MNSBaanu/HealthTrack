using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Models.Data
{
    // Entity Framework database context for HealthTrack, including the Identity tables.
    public class HealthTrackDbContext : IdentityDbContext<Person>
    {
        private readonly IDataProtector _protector;

        public HealthTrackDbContext(DbContextOptions<HealthTrackDbContext> options, IDataProtectionProvider dataProtection) : base(options)
        {
            _protector = dataProtection.CreateProtector("HealthTrack.SensitiveData");
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<ClientTrainerAssignment> ClientTrainerAssignments { get; set; }
        public DbSet<HealthMetric> HealthMetrics { get; set; }
        public DbSet<WorkoutLog> WorkoutLogs { get; set; }
        public DbSet<MealLog> MealLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AdminBroadcast> AdminBroadcasts { get; set; }
        public DbSet<NotificationEmail> NotificationEmails { get; set; }
        public DbSet<ChatQuestion> ChatQuestions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<ConsultationProvider> ConsultationProviders { get; set; }
        public DbSet<AvailabilitySlot> AvailabilitySlots { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<WorkoutPlan> WorkoutPlans { get; set; }
        public DbSet<WorkoutPlanItem> WorkoutPlanItems { get; set; }
        public DbSet<WorkoutPlanCompletion> WorkoutPlanCompletions { get; set; }
        public DbSet<DietPlan> DietPlans { get; set; }
        public DbSet<DietPlanMeal> DietPlanMeals { get; set; }
        public DbSet<TrainerFeedback> TrainerFeedbacks { get; set; }
        public DbSet<RescheduleRequest> RescheduleRequests { get; set; }
        public DbSet<WeeklyCheckIn> WeeklyCheckIns { get; set; }
        public DbSet<LoginHistory> LoginHistories { get; set; }
        public DbSet<TrainerAssignmentHistory> TrainerAssignmentHistories { get; set; }
        public DbSet<SystemSettings> SystemSettings { get; set; }
        public DbSet<ChatFaq> ChatFaqs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            var encrypted = new EncryptedStringConverter(_protector);
            builder.Entity<Client>().Property(c => c.FitnessGoal).HasConversion(encrypted).HasColumnType("nvarchar(max)");
            builder.Entity<Appointment>().Property(a => a.Notes).HasConversion(encrypted).HasColumnType("nvarchar(max)");
            builder.Entity<WeeklyCheckIn>().Property(c => c.Note).HasConversion(encrypted).HasColumnType("nvarchar(max)");
            builder.Entity<WeeklyCheckIn>().Property(c => c.Comment).HasConversion(encrypted).HasColumnType("nvarchar(max)");
            builder.Entity<TrainerFeedback>().Property(f => f.Comments).HasConversion(encrypted).HasColumnType("nvarchar(max)");
            builder.Entity<TrainerFeedback>().Property(f => f.ClientReply).HasConversion(encrypted).HasColumnType("nvarchar(max)");
            builder.Entity<SystemSettings>().Property(s => s.Id).ValueGeneratedNever();
            builder.Entity<Notification>().HasOne(n => n.AdminBroadcast).WithMany(b => b.Notifications)
                .HasForeignKey(n => n.AdminBroadcastId).OnDelete(DeleteBehavior.SetNull);
            builder.Entity<NotificationEmail>().HasKey(e => e.NotificationId);
            builder.Entity<NotificationEmail>().HasOne(e => e.Notification).WithOne(n => n.EmailDelivery)
                .HasForeignKey<NotificationEmail>(e => e.NotificationId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<NotificationEmail>().HasIndex(e => new { e.SentAtUtc, e.CancelledAtUtc, e.DueAtUtc });
            builder.Entity<AdminBroadcast>().HasIndex(b => b.CreatedAtUtc);

            builder.Entity<Notification>().HasOne(n => n.Appointment).WithMany()
                .HasForeignKey(n => n.AppointmentId).OnDelete(DeleteBehavior.NoAction);
            builder.Entity<Notification>().HasIndex(n => n.AppointmentId).IsUnique()
                .HasFilter("[AppointmentId] IS NOT NULL");

            builder.Entity<ClientTrainerAssignment>().HasKey(a => a.ClientId);
            builder.Entity<ClientTrainerAssignment>().HasOne(a => a.Client).WithOne(c => c.TrainerAssignment)
                .HasForeignKey<ClientTrainerAssignment>(a => a.ClientId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ClientTrainerAssignment>().HasOne(a => a.Trainer).WithMany(t => t.ClientAssignments)
                .HasForeignKey(a => a.TrainerId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<ClientTrainerAssignment>().Property(a => a.RowVersion).IsRowVersion();

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

            // Only one open (Pending or Accepted) reschedule request per appointment.
            builder.Entity<RescheduleRequest>().HasOne(r => r.Appointment).WithMany()
                .HasForeignKey(r => r.AppointmentId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<RescheduleRequest>().HasIndex(r => r.AppointmentId).IsUnique()
                .HasFilter("[Status] IN (0, 1)");
            builder.Entity<RescheduleRequest>().ToTable("RescheduleRequests", table =>
                table.HasCheckConstraint("CK_RescheduleRequests_Status", "[Status] IN (0, 1, 2, 3, 4)"));

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
                .HasValue<Trainer>("Trainer")
                .HasValue<Admin>("Admin");

            builder.Entity<MealLog>()
                .Property(m => m.MealType)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Entity<Notification>()
                .Property(n => n.Type)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Entity<WorkoutPlan>().HasOne(p => p.Trainer).WithMany(t => t.WorkoutPlans)
                .HasForeignKey(p => p.TrainerId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<WorkoutPlan>().HasOne(p => p.Client).WithMany(c => c.WorkoutPlans)
                .HasForeignKey(p => p.ClientId).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WorkoutPlanItem>().HasOne(i => i.WorkoutPlan).WithMany(p => p.Items)
                .HasForeignKey(i => i.WorkoutPlanId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<WorkoutPlanCompletion>().HasKey(c => new { c.WorkoutPlanItemId, c.ClientId, c.CompletedOn });
            builder.Entity<WorkoutPlanCompletion>().Property(c => c.CompletedOn).HasColumnType("date");
            builder.Entity<WorkoutPlanCompletion>().HasOne(c => c.WorkoutPlanItem).WithMany()
                .HasForeignKey(c => c.WorkoutPlanItemId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<WorkoutPlanCompletion>().HasOne(c => c.Client).WithMany()
                .HasForeignKey(c => c.ClientId).OnDelete(DeleteBehavior.NoAction);
            builder.Entity<WorkoutLog>().HasOne(l => l.WorkoutPlanItem).WithMany()
                .HasForeignKey(l => l.WorkoutPlanItemId).OnDelete(DeleteBehavior.ClientSetNull);
            builder.Entity<MealLog>().HasOne(l => l.DietPlanMeal).WithMany()
                .HasForeignKey(l => l.DietPlanMealId).OnDelete(DeleteBehavior.ClientSetNull);

            builder.Entity<DietPlan>().HasOne(p => p.Trainer).WithMany(t => t.DietPlans)
                .HasForeignKey(p => p.TrainerId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<DietPlan>().HasOne(p => p.Client).WithMany(c => c.DietPlans)
                .HasForeignKey(p => p.ClientId).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DietPlanMeal>().HasOne(m => m.DietPlan).WithMany(p => p.Meals)
                .HasForeignKey(m => m.DietPlanId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<DietPlanMeal>().Property(m => m.MealType)
                .HasConversion<string>().HasMaxLength(20);

            builder.Entity<TrainerFeedback>().HasOne(f => f.Trainer).WithMany(t => t.Feedbacks)
                .HasForeignKey(f => f.TrainerId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<TrainerFeedback>().HasOne(f => f.Client).WithMany(c => c.Feedbacks)
                .HasForeignKey(f => f.ClientId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<TrainerFeedback>().Property(f => f.Category)
                .HasConversion<string>().HasMaxLength(20);

            builder.Entity<WeeklyCheckIn>().HasOne(c => c.Trainer).WithMany()
                .HasForeignKey(c => c.TrainerId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<WeeklyCheckIn>().HasOne(c => c.Client).WithMany()
                .HasForeignKey(c => c.ClientId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<WeeklyCheckIn>().HasIndex(c => new { c.ClientId, c.RequestedAtUtc });
            builder.Entity<WeeklyCheckIn>().HasIndex(c => new { c.ClientId, c.TrainerId }).IsUnique()
                .HasFilter("[RespondedAtUtc] IS NULL");

            builder.Entity<LoginHistory>().HasOne(l => l.Person).WithMany()
                .HasForeignKey(l => l.PersonId).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TrainerAssignmentHistory>().HasOne(h => h.Client).WithMany()
                .HasForeignKey(h => h.ClientId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<TrainerAssignmentHistory>().HasOne(h => h.Trainer).WithMany()
                .HasForeignKey(h => h.TrainerId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
