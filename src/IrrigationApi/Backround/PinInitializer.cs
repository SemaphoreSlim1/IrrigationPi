using IrrigationApi.ApplicationCore.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Device.Gpio;
using System.Threading;
using System.Threading.Tasks;

namespace IrrigationApi.Backround
{
    public class PinInitializer : IHostedService
    {
        private readonly GpioController _gpioController;
        private readonly IrrigationConfig _config;

        public PinInitializer(GpioController gpioController, IOptions<IrrigationConfig> config)
        {
            _gpioController = gpioController;
            _config = config.Value;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _gpioController.OpenPin(_config.MasterControlValveGpio, PinMode.Output);
            _gpioController.Write(_config.MasterControlValveGpio, PinValue.High); //start with the mcv off

            foreach (var valve in _config.Valves)
            {
                _gpioController.OpenPin(valve.GpioPin, PinMode.Output);
                _gpioController.Write(valve.GpioPin, PinValue.High); //start with the valves all off
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            //tell the board to turn off all the valves, and don't wait for state change
            //then close the pin
            _gpioController.Write(_config.MasterControlValveGpio, PinValue.High);
            _gpioController.ClosePin(_config.MasterControlValveGpio);

            foreach (var valve in _config.Valves)
            {
                _gpioController.Write(valve.GpioPin, PinValue.High);
                _gpioController.ClosePin(valve.GpioPin);
            }

            return Task.CompletedTask;
        }
    }
}
