using Majstorio.Services.Mapping;
using Mapster;
using MapsterMapper;

var builder = WebApplication.CreateBuilder(args);

var mappingConfig = new TypeAdapterConfig();
new CityMappingProfile().Register(mappingConfig);
new ServiceCategoryMappingProfile().Register(mappingConfig);
builder.Services.AddSingleton(mappingConfig);
builder.Services.AddScoped<IMapper, ServiceMapper>();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
