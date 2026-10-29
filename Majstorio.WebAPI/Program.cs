using FluentValidation;
using Majstorio.Services.Mapping;
using Majstorio.Services.Validators.City;
using Majstorio.WebAPI.Filters;
using Majstorio.WebAPI.Handlers;
using Majstorio.WebAPI.Services;
using Mapster;
using MapsterMapper;

var builder = WebApplication.CreateBuilder(args);

var mappingConfig = new TypeAdapterConfig();
new CityMappingProfile().Register(mappingConfig);
new ServiceCategoryMappingProfile().Register(mappingConfig);
new NewsPostMappingProfile().Register(mappingConfig);
builder.Services.AddSingleton(mappingConfig);
builder.Services.AddScoped<IMapper, ServiceMapper>();

builder.Services.AddValidatorsFromAssemblyContaining<CityInsertRequestValidator>();
builder.Services.AddControllers(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    options.Filters.Add<RequestValidationFilter>();
});
builder.Services.AddSingleton<ResultToStatusCodeMapper>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

app.MapControllers();

app.Run();
