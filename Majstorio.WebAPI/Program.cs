using Majstorio.Services.Mapping;
using Majstorio.WebAPI.Handlers;
using Majstorio.WebAPI.Services;
using Mapster;
using MapsterMapper;

var builder = WebApplication.CreateBuilder(args);

var mappingConfig = new TypeAdapterConfig();
new CityMappingProfile().Register(mappingConfig);
new ServiceCategoryMappingProfile().Register(mappingConfig);
builder.Services.AddSingleton(mappingConfig);
builder.Services.AddScoped<IMapper, ServiceMapper>();

builder.Services.AddControllers();
builder.Services.AddSingleton<ResultToStatusCodeMapper>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

app.MapControllers();

app.Run();
