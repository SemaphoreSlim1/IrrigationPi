using IrrigationApi.ApplicationCore.Configuration;
using IrrigationApi.ApplicationCore.Hardware;
using IrrigationApi.ApplicationCore.Threading;
using IrrigationApi.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace IrrigationApi.Backround
{
    public class IrrigationProcessorStatus
    {
        public bool Running { get; set; }
    }

    public class IrrigationProcessor : BackgroundService
    {
        private readonly ChannelReader<IrrigationJob> _jobReader;
        private readonly RelayBoard _relayBoard;
        private readonly IIrrigationStopper _irrigationStopper;
        private readonly IrrigationConfig _config;
        private readonly ILogger _logger;

        private CancellationTokenSource _irrigationCts;
        private readonly IrrigationProcessorStatus _status;

        public IrrigationProcessor(ChannelReader<IrrigationJob> jobReader,
                                    RelayBoard relayBoard,
                                    IIrrigationStopper irrigationStopper,
                                    IrrigationProcessorStatus status,
                                    IOptions<IrrigationConfig> config,
                                    ILogger<IrrigationProcessor> logger)
        {
            _jobReader = jobReader;
            _relayBoard = relayBoard;
            _irrigationStopper = irrigationStopper;
            _config = config.Value;
            _logger = logger;
            _status = status;

            _irrigationCts = new CancellationTokenSource();
            _irrigationStopper.StopRequested += StopIrrigation;
        }

        private void StopIrrigation(object sender, EventArgs e)
        {
            _irrigationCts.Cancel();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _status.Running = true;

            while (await _jobReader.WaitToReadAsync(stoppingToken))
            {
                var executedJobs = new List<IrrigationJob>();
                _irrigationCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

                var zoneSwitch = false;

                while (_jobReader.TryRead(out var job))
                {
                    if (zoneSwitch)
                    { await Task.Delay(_config.ZoneSwitchDelay, _irrigationCts.Token); }

                    _logger.LogInformation("Incoming job: {@job}", job);

                    //turn on the master control valve, then run the job for the desired duration
                    _logger.LogInformation("Turning on MCV");
                    _relayBoard[_config.MasterControlValveGpio].On = true;
                    _logger.LogInformation("MCV on");

                    var valvePin = _config.Valves.First(v => v.ValveNumber == job.Valve).GpioPin;

                    _relayBoard[valvePin].On = true;

                    try
                    { await Task.Delay(job.Duration, _irrigationCts.Token); }
                    catch { }

                    _relayBoard[valvePin].On = false;

                    _logger.LogInformation("Turning off MCV");
                    _relayBoard[_config.MasterControlValveGpio].On = false;
                    _logger.LogInformation("MCV off");

                    executedJobs.Add(job);

                    if (stoppingToken.IsCancellationRequested)
                    {
                        //stop was requested, drain the queue of remaining jobs
                        while (_jobReader.TryRead(out job))
                        { }
                    }
                    else { zoneSwitch = true; }
                }

                if (executedJobs.Any() == false)
                { continue; } //no need to bleed pressure if no jobs were executed

                _logger.LogInformation("Bleeding pressure on {@executedJobs}", executedJobs);

                //for pressure bleeding, we just want the distinct pins - zones may have been executed multiple times
                //so open the zone, but don't turn on the mcv. This allows the pressure in the manifold to bleed out
                var irrigatedPins = executedJobs.Select(j => _config.Valves.First(v => v.ValveNumber == j.Valve).GpioPin).Distinct();

                foreach (var valvePin in irrigatedPins)
                {
                    _relayBoard[valvePin].On = true;

                    //wait a bit for the pressure to bleed out
                    try
                    { await Task.Delay(_config.PressureBleedTime, stoppingToken); }
                    catch { }

                    _relayBoard[valvePin].On = false;
                }

                _logger.LogInformation("Pressure bled");
                _logger.LogInformation("Irrigation complete");
            }

            _status.Running = false;
        }

    }
}
