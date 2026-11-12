using DotNetEnv;
using FluentValidation;
using Majstorio.Common.Services;
using Majstorio.Services.Database;
using Majstorio.Services.Services;
using Majstorio.Services.Mapping;
using Majstorio.Services.Validators.City;
using Majstorio.WebAPI.Filters;
using Majstorio.WebAPI.Handlers;
using Majstorio.WebAPI.Services;
using Majstorio.WebAPI.Services.Auth;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

// Local runs (dotnet run, Visual Studio) read the .env found by walking up from the working directory.
// Containers get the same values from docker compose, and values already set in the environment win.
Env.TraversePath().NoClobber().Load();

var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
    throw new InvalidOperationException(
        "ConnectionStrings__DefaultConnection is not configured. Copy .env.example to .env and fill in the values.");

var mappingConfig = new TypeAdapterConfig();
new CityMappingProfile().Register(mappingConfig);
new ServiceCategoryMappingProfile().Register(mappingConfig);
new NewsPostMappingProfile().Register(mappingConfig);
new AppointmentMappingProfile().Register(mappingConfig);
new OfferMappingProfile().Register(mappingConfig);
new ReviewMappingProfile().Register(mappingConfig);
builder.Services.AddSingleton(mappingConfig);
builder.Services.AddScoped<IMapper, ServiceMapper>();

builder.Services.AddValidatorsFromAssemblyContaining<CityInsertRequestValidator>();
builder.Services.AddOpenApi();
builder.Services.AddControllers(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    options.Filters.Add<RequestValidationFilter>();
});
builder.Services.AddSingleton<ResultToStatusCodeMapper>();

builder.Services.AddDbContext<MajstorioDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<ICryptoService, CryptoService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<INewsPostService, NewsPostService>();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Creates the 230070 database on first start and applies new migrations, including seed data.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MajstorioDbContext>();
    app.Logger.LogInformation("Applying database migrations.");
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

await app.RunAsync();
