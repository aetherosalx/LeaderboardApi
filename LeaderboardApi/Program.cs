
using LeaderboardApi.Data;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddRazorPages(); 

            builder.Services.AddDbContext<LeaderboardContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
            


            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                //app.UseSwaggerUI();

                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Leaderboard API V1");

                    // Add custom HTML/JS to inject a link
                    c.HeadContent = @"
        <script>
            window.addEventListener('DOMContentLoaded', function () {
                const link = document.createElement('a');
                link.href = '/Leaderboard';
                link.target = '_blank';
                link.innerText = 'Open Leaderboard UI';
                link.style.position = 'absolute';
                link.style.top = '10px';
                link.style.right = '20px';
                link.style.zIndex = '1000';
                link.style.backgroundColor = '#fff';
                link.style.padding = '8px 12px';
                link.style.border = '1px solid #ccc';
                link.style.borderRadius = '4px';
                link.style.fontSize = '14px';
                link.style.textDecoration = 'none';
                link.style.color = '#000';
                document.body.appendChild(link);
            });
        </script>";
                });

            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers(); // For Web API routes
            app.MapRazorPages(); // For Razor Page routing


            app.Run();
        }
    }
}
