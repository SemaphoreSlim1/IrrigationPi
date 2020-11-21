using IrrigationApi.Backround;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

namespace IrrigationApi.ApplicationCore.Health
{
    public class IrrigationProcessorHealthCheck : IHealthCheck
    {
        private readonly IrrigationProcessorStatus _status;

        public IrrigationProcessorHealthCheck(IrrigationProcessorStatus status)
        {
            _status = status;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (_status.Running)
            { return Task.FromResult(HealthCheckResult.Healthy("Irrigation processor is running")); }
            else
            { return Task.FromResult(HealthCheckResult.Unhealthy("Irrigation processor is not running")); }
        }
    }
}
