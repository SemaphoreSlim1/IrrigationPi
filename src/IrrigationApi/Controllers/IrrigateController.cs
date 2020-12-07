using IrrigationApi.ApplicationCore.Configuration;
using IrrigationApi.ApplicationCore.Threading;
using IrrigationApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace IrrigationApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IrrigateController : ControllerBase
    {
        private readonly ChannelWriter<IrrigationJob> _irrigationJobs;
        private readonly IIrrigationStopper _irrigationStopper;
        private readonly ILogger _logger;
        private readonly IrrigationConfig _config;

        public IrrigateController(ChannelWriter<IrrigationJob> irrigationJobs,
                                    IIrrigationStopper irrigationStopper,
                                    ILogger<IrrigateController> logger,
                                    IOptions<IrrigationConfig> config)
        {
            _irrigationJobs = irrigationJobs;
            _irrigationStopper = irrigationStopper;
            _logger = logger;
            _config = config.Value;
        }


        /// <summary>
        /// Irrigates a zone by enqueuing an irrigation job
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Irrigate([Required] IrrigationJob[] jobs)
        {
            _logger.LogInformation("Incoming jobs: {@jobs}", jobs);

            foreach (var job in jobs)
            {
                if (job != null)
                { await _irrigationJobs.WriteAsync(job); }
            }

            return Accepted();
        }

        [HttpGet("Configuration")]
        public IActionResult GetConfiguration()
        {
            return Ok(_config);
        }

        [HttpPost("Test/{valveNumber}")]
        public async Task<IActionResult> TestValve(int valveNumber)
        {
            var valve = _config.Valves.FirstOrDefault(v => v.ValveNumber == valveNumber);

            if (valve == null)
            { return this.Problem($"Valve number {valveNumber} is not a valid valve number"); }

            var job = new IrrigationJob()
            {
                Valve = valveNumber,
                Duration = TimeSpan.FromMinutes(2)
            };

            await _irrigationJobs.WriteAsync(job);

            return Accepted();
        }

        /// <summary>
        /// Stops irrigation and turns off the master control valve
        /// </summary>
        /// <returns></returns>
        [HttpPost("Stop")]
        public IActionResult Stop()
        {
            _irrigationStopper.RequestStop();

            return Ok();
        }

    }
}
