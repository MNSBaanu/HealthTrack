using Microsoft.EntityFrameworkCore;

namespace HealthTrack.Models.Data
{
    public class HealthTrackDbContext : DbContext
    {
        public HealthTrackDbContext(DbContextOptions<HealthTrackDbContext> options) : base(options)
        {

        }
    }
}
