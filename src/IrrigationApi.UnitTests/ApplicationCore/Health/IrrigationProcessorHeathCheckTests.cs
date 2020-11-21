using IrrigationApi.ApplicationCore.Health;
using IrrigationApi.Backround;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IrrigationApi.UnitTests.ApplicationCore.Health
{
    public class IrrigationProcessorHeathCheckTests
    {
        [Theory]
        [InlineData(true, HealthStatus.Healthy)]
        [InlineData(false, HealthStatus.Unhealthy)]
        public async Task CheckHealth_ReturnsExpectedFromStatus(bool running, HealthStatus expectedStatus)
        {
            var ctx = new HealthCheckContext();
            var status = new IrrigationProcessorStatus();
            status.Running = running;

            var check = new IrrigationProcessorHealthCheck(status);
            var result = await check.CheckHealthAsync(ctx, CancellationToken.None);

            Assert.Equal(expectedStatus, result.Status);
        }
    }
}
