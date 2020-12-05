using IrrigationApi.ApplicationCore.Configuration;
using IrrigationApi.ApplicationCore.Hardware;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace IrrigationApi.Backround
{
    public class PinInitializer : IHostedService
    {
        private readonly RelayBoard _relayBoard;
        private readonly IrrigationConfig _config;

        public PinInitializer(RelayBoard relayBoard, IOptions<IrrigationConfig> config)
        {
            _relayBoard = relayBoard;
            _config = config.Value;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _relayBoard[_config.MasterControlValveGpio].On = false;//start with the mcv off

            foreach (var valve in _config.Valves)
            {
                _relayBoard[valve.GpioPin].On = false; //start with the valves all off
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            //tell the board to turn off all the valves, and don't wait for state change
            //then close the pin
            _relayBoard[_config.MasterControlValveGpio].On = false;//turn off the mcv

            foreach (var valve in _config.Valves)
            {
                _relayBoard[valve.GpioPin].On = false; //and all the valves
            }

            return Task.CompletedTask;
        }
    }
}
