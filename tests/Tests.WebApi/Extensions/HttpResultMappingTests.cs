using Application.Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Tests.Shared.Attributes;
using Tests.Shared.Base;
using Web.Extensions;

namespace Tests.WebApi.Extensions;

[Unit]
public sealed class HttpResultMappingTests : UnitTestsBase
{
    private static HttpContext BuildContext()
    {
        var services = new ServiceCollection();
        services.AddProblemDetails();
        services.AddLogging();
        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            Response = { Body = new MemoryStream() }
        };
        return context;
    }

    [TestCase(ErrorCode.Validation, 400)]
    [TestCase(ErrorCode.NotFound, 404)]
    [TestCase(ErrorCode.Conflict, 409)]
    [TestCase(ErrorCode.Unprocessable, 422)]
    [TestCase(ErrorCode.Unauthorized, 401)]
    [TestCase(ErrorCode.Forbidden, 403)]
    [TestCase(ErrorCode.Internal, 500)]
    public async Task ToHttpResult_maps_error_code_to_expected_http_status(ErrorCode code, int expectedStatus)
    {
        // Arrange
        var result = OperationResult<string>.Fail(code, "error");
        var httpResult = result.ToHttpResult();
        var context = BuildContext();

        // Act
        await httpResult.ExecuteAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(expectedStatus);
    }

    [Test]
    public async Task ToHttpResult_success_returns_200_with_data()
    {
        // Arrange
        var result = OperationResult<string>.SuccessResult("hello");
        var httpResult = result.ToHttpResult(200);
        var context = BuildContext();

        // Act
        await httpResult.ExecuteAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(200);
    }

    [Test]
    public async Task ToHttpResult_success_202_returns_202()
    {
        // Arrange
        var result = OperationResult<string>.SuccessResult("created");
        var httpResult = result.ToHttpResult(202);
        var context = BuildContext();

        // Act
        await httpResult.ExecuteAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(202);
    }
}
