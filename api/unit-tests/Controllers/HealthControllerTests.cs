using api.Controllers;
using api.HealthCheck;
using api.Services.Contracts;
using api.Shared;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace unit_tests.Controllers;

public class HealthControllerTests
{
    private const string FunctionKeyHeaderName = "X-Function-Key";
    private const string ValidFunctionKey = "test-key";

    private static HealthController CreateController(IHealthCheckService healthCheckService, HealthPingOptions options, string? providedKey = null)
    {
        var controller = new HealthController(healthCheckService, Options.Create(options))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        if (!string.IsNullOrWhiteSpace(providedKey))
            controller.Request.Headers[FunctionKeyHeaderName] = providedKey;

        return controller;
    }

    [Fact]
    public async Task CheckDatabase_WhenHealthy_ReturnsOk()
    {
        var healthCheckService = new Mock<IHealthCheckService>();
        healthCheckService
            .Setup(service => service.IsDatabaseHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var options = new HealthPingOptions
        {
            Endpoint = "http://localhost",
            FunctionKey = ValidFunctionKey
        };
        var controller = CreateController(healthCheckService.Object, options, ValidFunctionKey);

        var result = await controller.CheckDatabase(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CheckDatabase_WhenUnhealthy_ReturnsServiceUnavailable()
    {
        var healthCheckService = new Mock<IHealthCheckService>();
        healthCheckService
            .Setup(service => service.IsDatabaseHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure("Database down"));

        var options = new HealthPingOptions
        {
            Endpoint = "http://localhost",
            FunctionKey = ValidFunctionKey
        };
        var controller = CreateController(healthCheckService.Object, options, ValidFunctionKey);

        var result = await controller.CheckDatabase(CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task CheckDatabase_WhenKeyMissing_ReturnsUnauthorized()
    {
        var healthCheckService = new Mock<IHealthCheckService>();
        healthCheckService
            .Setup(service => service.IsDatabaseHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var options = new HealthPingOptions
        {
            Endpoint = "http://localhost",
            FunctionKey = ValidFunctionKey
        };
        var controller = CreateController(healthCheckService.Object, options);

        var result = await controller.CheckDatabase(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task CheckDatabase_WhenKeyInvalid_ReturnsUnauthorized()
    {
        var healthCheckService = new Mock<IHealthCheckService>();
        healthCheckService
            .Setup(service => service.IsDatabaseHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var options = new HealthPingOptions
        {
            Endpoint = "http://localhost",
            FunctionKey = ValidFunctionKey
        };
        var controller = CreateController(healthCheckService.Object, options, "wrong-key");

        var result = await controller.CheckDatabase(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task CheckDatabase_WhenKeyNotConfigured_ReturnsServerError()
    {
        var healthCheckService = new Mock<IHealthCheckService>();
        healthCheckService
            .Setup(service => service.IsDatabaseHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var options = new HealthPingOptions
        {
            Endpoint = "http://localhost",
            FunctionKey = ""
        };
        var controller = CreateController(healthCheckService.Object, options, ValidFunctionKey);

        var result = await controller.CheckDatabase(CancellationToken.None);

        var statusResult = result.Should().BeOfType<StatusCodeResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }
}
