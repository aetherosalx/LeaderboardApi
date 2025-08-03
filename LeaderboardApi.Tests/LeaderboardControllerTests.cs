using Xunit;
using Microsoft.EntityFrameworkCore;
using LeaderboardApi.Controllers;
using LeaderboardApi.Data;
using LeaderboardApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LeaderboardApi.Tests
{
    public class LeaderboardControllerTests
    {
        private LeaderboardContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<LeaderboardContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new LeaderboardContext(options);
        }

        [Fact]
        public async Task SubmitScore_Inserts_New_Score_When_None_Exists()
        {
            // Arrange
            var context = GetInMemoryContext();
            var controller = new LeaderboardController(context);
            var newScore = new PlayerScore { PlayerName = "Alex", Level = 1, Score = 100 };

            // Act
            var result = await controller.SubmitScore(newScore);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedScore = Assert.IsType<PlayerScore>(okResult.Value);
            Assert.Equal(100, returnedScore.Score);
        }

        [Fact]
        public async Task SubmitScore_Updates_Score_When_Higher()
        {
            var context = GetInMemoryContext();
            context.PlayerScores.Add(new PlayerScore { PlayerName = "Alex", Level = 1, Score = 90 });
            await context.SaveChangesAsync();

            var controller = new LeaderboardController(context);
            var updatedScore = new PlayerScore { PlayerName = "Alex", Level = 1, Score = 120 };

            var result = await controller.SubmitScore(updatedScore);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<PlayerScore>(okResult.Value);
            Assert.Equal(120, returned.Score);
        }

        [Fact]
        public async Task SubmitScore_Ignores_Score_When_Lower()
        {
            var context = GetInMemoryContext();
            context.PlayerScores.Add(new PlayerScore { PlayerName = "Alex", Level = 1, Score = 150 });
            await context.SaveChangesAsync();

            var controller = new LeaderboardController(context);
            var lowerScore = new PlayerScore { PlayerName = "Alex", Level = 1, Score = 100 };

            var result = await controller.SubmitScore(lowerScore);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<PlayerScore>(okResult.Value);
            Assert.Equal(150, returned.Score); // Should still be the higher one
        }
    }
}
