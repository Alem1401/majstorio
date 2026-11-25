using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Majstorio.Model.Requests.City;
using Xunit;

namespace Majstorio.Services.Tests;

// Update requests carry the record's id, but only the controller may set it, from the URL.
public class RouteIdRequestTests
{
    public static IEnumerable<object[]> UpdateRequestTypes() => typeof(CityCreateRequest).Assembly.GetTypes()
        .Where(type => type.Name.EndsWith("UpdateRequest") && type.Name != "MyProfileUpdateRequest")
        .Select(type => new object[] { type });

    [Theory]
    [MemberData(nameof(UpdateRequestTypes))]
    public void UpdateRequest_HasAnIdThatIsNotReadFromTheBody(Type requestType)
    {
        var id = requestType.GetProperty("Id");

        Assert.NotNull(id);
        Assert.NotNull(id.GetCustomAttribute<JsonIgnoreAttribute>());
    }

    [Fact]
    public void UpdateRequest_IdSentInTheBodyIsIgnored()
    {
        var request = JsonSerializer.Deserialize<CityUpdateRequest>("{\"id\":7,\"name\":\"Mostar\",\"isActive\":true}",
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(0, request!.Id);
        Assert.Equal("Mostar", request.Name);
    }
}
