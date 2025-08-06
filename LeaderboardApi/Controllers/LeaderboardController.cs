using LeaderboardApi.Data;
using LeaderboardApi.Models;
using LeaderboardApi.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

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
        public async Task<ActionResult<PlayerScore>> SubmitScore([FromBody] PlayerScore score)
        {
            //string playerName = score.PlayerName?.Trim() ?? string.Empty;
           // playerName = Regex.Replace(score.PlayerName?.Trim() ?? string.Empty, @"\s+", " ");

            score.PlayerName = Regex.Replace(score.PlayerName?.Trim() ?? string.Empty, @"\s+", " ");

            if (string.IsNullOrWhiteSpace(score.PlayerName) ||
             score.PlayerName.Length > 50 ||
             score.Level < 1 || score.Level > 5 ||
             score.Score < 1 || score.Score > 1000000)
            {
                return BadRequest("Invalid input. Please check player name, level, and score.");
            }


            //Console.WriteLine("=======ENTERED SUBMIT SCORE===========");
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // Look for an existing score by the same player on the same level
            var existing = await _context.PlayerScores
                .FirstOrDefaultAsync(p => p.PlayerName == score.PlayerName && p.Level == score.Level);

            // No previous score for this player/level → insert new score
            if (existing == null)
            {
                _context.PlayerScores.Add(score);
            }
            // Score exists → update only if new score is higher
            else
            {
                
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
                    SubmittedAt = score.SubmittedAt
                });
            }
            else
            {
                totalRecord.Score = totalScore;
                totalRecord.SubmittedAt = score.SubmittedAt;
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


        [HttpGet("populate")]
        [HttpPost("populate")]
        public async Task<IActionResult> PopulateTestData()
        {
            var random = new Random();

            // NATO phonetic alphabet words
            string[] natoWords = new[]
            {
        "Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel", "India",
        "Juliett", "Kilo", "Lima", "Mike", "November", "Oscar", "Papa", "Quebec", "Romeo",
        "Sierra", "Tango", "Uniform", "Victor", "Whiskey", "Xray", "Yankee", "Zulu"
    };

            var players = new HashSet<string>();

            // Generate 50 unique player names using two-word combinations + number
            while (players.Count < 50)
            {
                var first = natoWords[random.Next(natoWords.Length)];
                var second = natoWords[random.Next(natoWords.Length)];

                // Ensure the two words are different (e.g., avoid "AlphaAlpha")
                if (first == second)
                    continue;

                //var suffix = random.Next(10, 100); // e.g., 42
                var name = $"{first}{second}";// {suffix}"; // e.g., "AlphaZulu42"

                players.Add(name);
            }

            // Randomly assign scores to levels for each player
            foreach (var player in players)
            {
                // Pick a random number of levels (1–5) for this player
                int levelsToSubmit = random.Next(1, 6);
                var levels = Enumerable.Range(1, 5).OrderBy(_ => random.Next()).Take(levelsToSubmit);

                for (int i = 1;i <= levelsToSubmit; i++)
                {
                    var level = i;
 
                    var score = random.Next(1, 1000)*100; // Score range: 100–100000

                    var playerScore = new PlayerScore
                    {
                        PlayerName = player,
                        Level = level,
                        Score = score,
                        SubmittedAt = DateTime.UtcNow.AddMinutes(-random.Next(0, 100000))
                    };

                    await SubmitScore(playerScore);
                }
            }

            return Ok("Random test data populated.");
        }

        [HttpGet("clear")]
        [HttpPost("clear")]
        public async Task<IActionResult> ClearScores()
        {
            var allScores = _context.PlayerScores;
            _context.PlayerScores.RemoveRange(allScores);
            await _context.SaveChangesAsync();

            return Ok("All scores cleared.");
        }
    }
}
