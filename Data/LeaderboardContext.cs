using Microsoft.EntityFrameworkCore;
using LeaderboardApi.Models;

namespace LeaderboardApi.Data
{
    public class LeaderboardContext : DbContext
    {
        public LeaderboardContext(DbContextOptions<LeaderboardContext> options) : base(options) { }

        public DbSet<PlayerScore> PlayerScores => Set<PlayerScore>();
    }
}
