using LeaderboardApi.Data;
using LeaderboardApi.Models;
using LeaderboardApi.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaderboardController : ControllerBase
    {
        private readonly LeaderboardContext _context;

        public LeaderboardController(LeaderboardContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<PlayerScore>> SubmitScore(PlayerScore score)
        {
            // Look for an existing score by the same player on the same level
            var existing = await _context.PlayerScores
                .FirstOrDefaultAsync(p => p.PlayerName == score.PlayerName && p.Level == score.Level);

            if (existing == null)
            {
                // No previous score for this player/level → insert new score
                _context.PlayerScores.Add(score);
            }
            else
            {
                // Score exists → update only if new score is higher
                if (score.Score > existing.Score)
                {
                    existing.Score = score.Score;
                    existing.SubmittedAt = DateTime.UtcNow;
                    _context.PlayerScores.Update(existing);
                }
                else
                {
                    // New score is lower or equal → return existing record without updating
                    return Ok(existing);
                }
            }

            // Save changes to the database
            await _context.SaveChangesAsync();

            // Recalculate and store total score in level 0 
            var totalScore = await _context.PlayerScores
                .Where(p => p.PlayerName == score.PlayerName && p.Level != 0)
                .SumAsync(p => p.Score);

            var totalRecord = await _context.PlayerScores
                .FirstOrDefaultAsync(p => p.PlayerName == score.PlayerName && p.Level == 0);

            if (totalRecord == null)
            {
                _context.PlayerScores.Add(new PlayerScore
                {
                    PlayerName = score.PlayerName,
                    Level = 0,
                    Score = totalScore,
                    SubmittedAt = DateTime.UtcNow
                });
            }
            else
            {
                totalRecord.Score = totalScore;
                totalRecord.SubmittedAt = DateTime.UtcNow;
                _context.PlayerScores.Update(totalRecord);
            }

            await _context.SaveChangesAsync();

            // Return 200 OK with the inserted or updated score
            return Ok(score);
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetTopScores(
            [FromQuery] int page = 0,
            [FromQuery] int pageSize = 10,
            [FromQuery] int level = 0,
            [FromQuery] string? player = null)
        {
            IQueryable<PlayerScore> query = _context.PlayerScores;

            //if (level > 0)
            //    query = query.Where(p => p.Level == level);

            //// Group scores by player and sum total scores
            //var grouped = await query
            //    .GroupBy(p => p.PlayerName)
            //    .Select(g => new
            //    {
            //        PlayerName = g.Key,
            //        TotalScore = g.Sum(x => x.Score),
            //        LatestSubmission = g.Max(x => x.SubmittedAt)
            //    })
            //    .OrderByDescending(g => g.TotalScore)
            //    .ThenBy(g => g.LatestSubmission)
            //    .ToListAsync();

            var grouped = await query
              .Where(p => p.Level == level)
              .Select(p => new
              {
                  PlayerName = p.PlayerName,
                  TotalScore = p.Score,
                  LatestSubmission = p.SubmittedAt
              })
              .OrderByDescending(p => p.TotalScore)
              .ThenBy(p => p.LatestSubmission)
              .ToListAsync();


            // Assign global ranks (1-based index)
            var ranked = grouped
                .Select((entry, index) => new
                {
                    Rank = index + 1,
                    entry.PlayerName,
                    entry.TotalScore,
                    entry.LatestSubmission
                })
                .ToList();

            

            LeaderboardEntryDto? playerRow = null;

            // If playerName is provided and page calculate which page the player appears on
            if (!string.IsNullOrWhiteSpace(player))
            {
                var match = ranked.FirstOrDefault(p =>
                    string.Equals(p.PlayerName, player, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    playerRow = new LeaderboardEntryDto
                    {
                        Rank = match.Rank,
                        PlayerName = match.PlayerName,
                        TotalScore = match.TotalScore,
                        LatestSubmission = match.LatestSubmission
                    };

                    // Only compute page if page == 0
                    if (page == 0)
                    {
                        int index = match.Rank - 1;
                        page = (index / pageSize) + 1;
                    }
                }
            }

            // If still 0 (e.g., no playerName given), default to page 1
            if (page == 0)
                page = 1;

            // Calculate total pages
            int totalPages = (int)Math.Ceiling((double)ranked.Count / pageSize);

            // Apply paging logic
            var paged = ranked
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();


            var dto = new LeaderboardResultDto
            {
                Page = page,
                TotalPages = totalPages,
                TotalPlayers = ranked.Count,
                PlayerRow = playerRow, // include if present
                Results = paged.Select(p => new LeaderboardEntryDto
                {
                    Rank = p.Rank,
                    PlayerName = p.PlayerName,
                    TotalScore = p.TotalScore,
                    LatestSubmission = p.LatestSubmission
                }).ToList()
            };

            return Ok(dto);
        }


        [HttpPost("populate")]
        public async Task<IActionResult> PopulateTestData()
        {
            var random = new Random();
            var players = new List<string>();

            // Generate 50 unique player names
            for (int i = 1; i <= 50; i++)
                players.Add($"Player{i}");

            for (int level = 1; level <= 5; level++)
            {
                foreach (var player in players)
                {
                    Console.WriteLine($"{player} added to {level}");
                    var score = random.Next(1, 50)*100;
                    var playerScore = new PlayerScore
                    {
                        PlayerName = player,
                        Level = level,
                        Score = score
                    };

                    await SubmitScore(playerScore); // Reuse existing logic (updates level 0)
                }
            }

            return Ok("Test data populated.");
        }

        [HttpPost("clear")]
        public async Task<IActionResult> ClearScores()
        {
            var allScores = _context.PlayerScores;
            _context.PlayerScores.RemoveRange(allScores);
            await _context.SaveChangesAsync();

            return Ok("All scores cleared.");
        }

        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<PlayerScore>>> GetAllScores()
        {
            return await _context.PlayerScores
                .OrderBy(p => p.PlayerName)
                .ThenBy(p => p.Level)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PlayerScore>> GetScoreById(int id)
        {
            var score = await _context.PlayerScores.FindAsync(id);
            if (score == null) return NotFound();
            return Ok(score);
        }
    }
}
