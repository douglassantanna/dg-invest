using System;
using api.Controllers;
using api.Services.Contracts;
using api.Shared;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace unit_tests.Controllers;

public class HealthControllerTests
{
    [Fact]
    public async Task CheckDatabase_WhenHealthy_ReturnsOk()
    {
        var healthCheckService = new Mock<IHealthCheckService>();
        healthCheckService
            .Setup(service => service.IsDatabaseHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var controller = new HealthController(healthCheckService.Object);

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

        var controller = new HealthController(healthCheckService.Object);

        var result = await controller.CheckDatabase(CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(503);
    }
}
