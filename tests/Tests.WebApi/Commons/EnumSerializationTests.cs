using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Commons;
using Tests.Shared.Attributes;
using Tests.Shared.Base;

namespace Tests.WebApi.Commons;

[Unit]
public sealed class EnumSerializationTests : UnitTestsBase
{
    private readonly JsonSerializerOptions _options = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) }
    };

    [TestCase(ErrorCode.None, "\"NONE\"")]
    [TestCase(ErrorCode.Validation, "\"VALIDATION\"")]
    [TestCase(ErrorCode.NotFound, "\"NOT_FOUND\"")]
    [TestCase(ErrorCode.Conflict, "\"CONFLICT\"")]
    [TestCase(ErrorCode.Unprocessable, "\"UNPROCESSABLE\"")]
    [TestCase(ErrorCode.Unauthorized, "\"UNAUTHORIZED\"")]
    [TestCase(ErrorCode.Forbidden, "\"FORBIDDEN\"")]
    [TestCase(ErrorCode.Internal, "\"INTERNAL\"")]
    public void ErrorCode_serializes_to_SNAKE_UPPER(ErrorCode code, string expected)
    {
        var json = JsonSerializer.Serialize(code, _options);

        json.Should().Be(expected);
    }
}
