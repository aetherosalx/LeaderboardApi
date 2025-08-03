namespace LeaderboardApi.Models.Dtos
{
    public class LeaderboardEntryDto
    {
        public int Rank { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int TotalScore { get; set; }
        public DateTime LatestSubmission { get; set; }
    }

    public class LeaderboardResultDto
    {
        public int Page { get; set; }

        public int TotalPages { get; set; }

        public int TotalPlayers { get; set; }

        public LeaderboardEntryDto? PlayerRow { get; set; } 
        public List<LeaderboardEntryDto> Results { get; set; } = new();
    }
}
