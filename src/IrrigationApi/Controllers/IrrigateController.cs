using IrrigationApi.ApplicationCore.Threading;
using IrrigationApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
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

        public IrrigateController(ChannelWriter<IrrigationJob> irrigationJobs,
                                    IIrrigationStopper irrigationStopper,
                                    ILogger<IrrigateController> logger)
        {
            _irrigationJobs = irrigationJobs;
            _irrigationStopper = irrigationStopper;
            _logger = logger;
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
