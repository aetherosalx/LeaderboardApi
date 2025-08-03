using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;

using LeaderboardApi.Models.Dtos;

public class LeaderboardModel : PageModel
{

    // List to be shown in the table
    public List<LeaderboardEntryDto> Scores { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int CurrentLevel { get; set; } = 0;

    public int TotalPages { get; set; } = 0;
    public int TotalPlayers { get; set; } = 0;

    public string PlayerName { get; set; } = null;

    public LeaderboardEntryDto? PlayerRow { get; set; }



    // Called when the page loads
    public async Task OnGetAsync([FromQuery] int page = 0, [FromQuery] int level = 0, [FromQuery] string player = null)
    {
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


        // Request leaderboard scores with page number
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
}
