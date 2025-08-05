
using LeaderboardApi.Data;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.AddConsole();

            // Bind to dynamic port only in production
            if (!builder.Environment.IsDevelopment())
            {
                var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
                builder.WebHost.UseUrls($"http://*:{port}");
            }

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddRazorPages(); 

            builder.Services.AddDbContext<LeaderboardContext>(options =>
            {
                var useSqlite = builder.Configuration.GetValue<bool>("UseSqlite");
                if (useSqlite)
                {
                    var sqliteConnection = builder.Configuration.GetConnectionString("SqliteConnection");
                    options.UseSqlite(sqliteConnection);
                }
                else
                {
                    var postgresConnection = builder.Configuration.GetConnectionString("DefaultConnection");
                    options.UseNpgsql(postgresConnection);
                }
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Logging.AddConsole();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();

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

            //app.UseHttpsRedirection(); Render deals with HTTPS automatically

            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync($"Internal Server Error: {ex.Message}");
                }
            });
            app.UseRouting();
            app.UseAuthorization();


            app.MapControllers(); // For Web API routes
            app.MapRazorPages(); // For Razor Page routing


            app.Run();
        }
    }
}
