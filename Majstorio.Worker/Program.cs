using DotNetEnv;
using Majstorio.Services.Database;
using Majstorio.Worker;
using Microsoft.EntityFrameworkCore;

// Same .env as the API (found by walking up from the working directory), so configuration lives in one place.
// In Docker, compose passes the same values as environment variables and they win.
Env.TraversePath().NoClobber().Load();

var builder = Host.CreateApplicationBuilder(args);

if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
    throw new InvalidOperationException(
        "ConnectionStrings__DefaultConnection is not configured. Copy .env.example to .env and fill in the values.");

builder.Services.AddDbContext<MajstorioDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHostedService<NotificationConsumer>();

await builder.Build().RunAsync();
