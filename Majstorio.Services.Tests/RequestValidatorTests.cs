using System.Reflection;
using FluentValidation;
using Majstorio.Model.Requests.City;
using Majstorio.Services.Validators.City;
using Xunit;

namespace Majstorio.Services.Tests;

public class RequestValidatorTests
{
    public static IEnumerable<object[]> RequestTypes() => typeof(CityInsertRequest).Assembly.GetTypes()
        .Where(type => type.IsClass && type.Namespace?.StartsWith("Majstorio.Model.Requests.") == true)
        .Select(type => new object[] { type });

    [Theory]
    [MemberData(nameof(RequestTypes))]
    public void EveryRequest_HasOneFluentValidatorAndAcceptsValidInput(Type requestType)
    {
        var validator = CreateValidator(requestType);

        var result = validator.Validate(new ValidationContext<object>(CreateValidRequest(requestType)));

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Theory]
    [MemberData(nameof(RequestTypes))]
    public void RequiredTextFields_RejectNullEmptyAndWhitespace(Type requestType)
    {
        var validator = CreateValidator(requestType);
        var nullability = new NullabilityInfoContext();
        var requiredTextFields = requestType.GetProperties().Where(property =>
            property.PropertyType == typeof(string)
            && nullability.Create(property).ReadState == NullabilityState.NotNull
            && property.Name != "ImageUrl"); // News post images are optional.

        foreach (var property in requiredTextFields)
        {
            foreach (var invalidValue in new[] { null, "", " \t\r\n" })
            {
                var request = CreateValidRequest(requestType);
                property.SetValue(request, invalidValue);

                var result = validator.Validate(new ValidationContext<object>(request));

                Assert.Contains(result.Errors, error => error.PropertyName == property.Name);
            }
        }
    }

    [Theory]
    [MemberData(nameof(RequestTypes))]
    public void RequestIds_RejectZeroAndNegativeValues(Type requestType)
    {
        var validator = CreateValidator(requestType);
        var idFields = requestType.GetProperties().Where(property =>
            property.PropertyType == typeof(int) && property.Name.EndsWith("Id"));

        foreach (var property in idFields)
        {
            foreach (var invalidId in new[] { 0, -1 })
            {
                var request = CreateValidRequest(requestType);
                property.SetValue(request, invalidId);

                var result = validator.Validate(new ValidationContext<object>(request));

                Assert.Contains(result.Errors, error => error.PropertyName == property.Name);
            }
        }
    }

    [Theory]
    [MemberData(nameof(RequestTypes))]
    public void OptionalTextFields_AcceptNull(Type requestType)
    {
        var request = CreateValidRequest(requestType);
        var nullability = new NullabilityInfoContext();
        foreach (var property in requestType.GetProperties().Where(property =>
            property.PropertyType == typeof(string)
            && (nullability.Create(property).ReadState == NullabilityState.Nullable || property.Name == "ImageUrl")))
        {
            property.SetValue(request, null);
        }

        var result = CreateValidator(requestType).Validate(new ValidationContext<object>(request));

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    private static IValidator CreateValidator(Type requestType)
    {
        // Uses the same assembly scanner as the API's AddValidatorsFromAssemblyContaining registration.
        var registration = Assert.Single(
            AssemblyScanner.FindValidatorsInAssembly(typeof(CityInsertRequestValidator).Assembly),
            result => result.InterfaceType == typeof(IValidator<>).MakeGenericType(requestType));
        return (IValidator)Activator.CreateInstance(registration.ValidatorType)!;
    }

    private static object CreateValidRequest(Type requestType)
    {
        var request = Activator.CreateInstance(requestType)!;
        var startUtc = DateTime.UtcNow.AddDays(2);
        foreach (var property in requestType.GetProperties())
        {
            if (property.PropertyType == typeof(string))
            {
                property.SetValue(request, property.Name switch
                {
                    "Email" => "person@example.com",
                    "DecisionReason" => null,
                    "ImageUrl" or "ProfileImageUrl" => "https://example.com/image.jpg",
                    _ => "Valid text"
                });
            }
            else if (property.PropertyType == typeof(int))
                property.SetValue(request, property.Name == "Status" ? 0 : 1);
            else if (property.PropertyType == typeof(decimal))
                property.SetValue(request, 100m);
            else if (property.PropertyType == typeof(DateTime))
                property.SetValue(request, property.Name == "EndUtc" ? startUtc.AddHours(1) : startUtc);
        }

        return request;
    }
}
