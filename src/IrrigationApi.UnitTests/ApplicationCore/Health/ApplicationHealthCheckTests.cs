using IrrigationApi.ApplicationCore.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IrrigationApi.UnitTests.ApplicationCore.Health
{
    public class ApplicationHealthCheckTests
    {
        [Fact]
        public async Task CheckHealth_AlwaysReturnsHealthy()
        {
            var ctx = new HealthCheckContext();
            var appHealthCheck = new ApplicationHealthCheck();
            var result = await appHealthCheck.CheckHealthAsync(ctx, CancellationToken.None);

            Assert.Equal(HealthStatus.Healthy, result.Status);
        }
    }
}
