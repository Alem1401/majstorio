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

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
