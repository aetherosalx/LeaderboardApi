using LeaderboardApi.Models;
using LeaderboardApi.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

public class LeaderboardModel : PageModel
{

    // List to be shown in the table
    public List<LeaderboardEntryDto> Scores { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int CurrentLevel { get; set; } = 0;
    public int TotalPages { get; set; } = 0;
    public int TotalPlayers { get; set; } = 0;
    public string PlayerName { get; set; } = "";

    public LeaderboardEntryDto? PlayerRow { get; set; }


    // Called when the page loads
    public async Task OnGetAsync([FromQuery] int page = 0, [FromQuery] int level = 0, [FromQuery] string player = "")
    {
        //Console.WriteLine("=======PAGE LOAD===========");
        //CurrentPage = page;
        CurrentLevel = level;
        PlayerName = player;

        using var client = new HttpClient();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var url = $"{baseUrl}/api/leaderboard?page={page}";
        if (level > 0)
            url += $"&level={level}";

        // Add playerName if present
        if (!string.IsNullOrWhiteSpace(player))
            url += $"&player={Uri.EscapeDataString(player)}";


        // Request leaderboard scores 
        var result = await client.GetFromJsonAsync<LeaderboardResultDto>(url);


        if (result is not null)
        {
            CurrentPage = result.Page;
            TotalPages = result.TotalPages;
            TotalPlayers = result.TotalPlayers;
            Scores = result.Results;
            PlayerRow = result.PlayerRow;
        }
    }


    [BindProperty]
    public string SubmitPlayerName { get; set; } = "";

    [BindProperty]
    public int Level { get; set; }

    [BindProperty]
    public int Score { get; set; }
    public async Task<IActionResult> OnPostAsync()
    {
        //Console.WriteLine("=======ENTERED OnPostAsync===========");
        SubmitPlayerName = Regex.Replace(SubmitPlayerName?.Trim() ?? string.Empty, @"\s+", " ");

        if (string.IsNullOrWhiteSpace(SubmitPlayerName?.Trim()) ||
             SubmitPlayerName.Length > 50 ||
             Level < 1 || Level > 5 ||
             Score < 1 || Score > 1000000)
        {
            ModelState.AddModelError(string.Empty, "Please enter a valid player name, level, and score.");
            await OnGetAsync(0, Level, SubmitPlayerName);
            return Page();
        }

        using var client = new HttpClient();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var postUrl = $"{baseUrl}/api/Leaderboard";

        Console.WriteLine($"Submitting to: {postUrl}");
        var scoreData = new PlayerScore
        {
            PlayerName = SubmitPlayerName,
            Level = Level,
            Score = Score,
            SubmittedAt = DateTime.UtcNow
        };



        var response = await client.PostAsJsonAsync(postUrl, scoreData);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error: {response.StatusCode} - {errorContent}");

            ModelState.AddModelError(string.Empty, "Failed to submit score.");
            await OnGetAsync(0, Level, PlayerName);
            return Page();
        }

        if (response.IsSuccessStatusCode)
        {
            // Reset form fields
            //Level = 0;
            Score = 0;

            //Console.WriteLine($"Redirecting to Leaderboard with: page=0, level={Level}, player={SubmitPlayerName}");
            // Reload the page with the new data (e.g. force player’s row to be shown)
            return RedirectToPage("/Leaderboard", new { page = 0, level = Level, player = SubmitPlayerName });
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error: {response.StatusCode} - {errorContent}");

            ModelState.AddModelError(string.Empty, "Failed to submit score.");
            await OnGetAsync(0, Level, SubmitPlayerName);
            return Page();
        }
    }
}
