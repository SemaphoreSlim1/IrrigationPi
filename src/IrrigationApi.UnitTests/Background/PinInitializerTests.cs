using IrrigationApi.ApplicationCore;
using IrrigationApi.ApplicationCore.Configuration;
using IrrigationApi.Backround;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IrrigationApi.UnitTests.Background
{
    public class PinInitializerTests
    {
        private readonly IrrigationConfig _config;
        private readonly PinInitializer _pinInitializer;
        private readonly MemoryGpioDriver _gpioDriver;
        private readonly GpioController _gpioController;

        public PinInitializerTests()
        {
            _config = new IrrigationConfig
            {
                MasterControlValveGpio = 1,
                PressureBleedTime = TimeSpan.FromSeconds(0),
                Valves = new List<PinMapping>
                {
                    new PinMapping{ GpioPin = 2, ValveNumber = 1},
                    new PinMapping{ GpioPin = 3, ValveNumber = 2},
                    new PinMapping{ GpioPin = 4, ValveNumber = 3}
                }
            };

            var irrigationOptionsMock = new Mock<IOptions<IrrigationConfig>>();
            irrigationOptionsMock.SetupGet(x => x.Value).Returns(_config);
            _gpioDriver = new MemoryGpioDriver(pinCount: 50);


            _gpioController = new GpioController(PinNumberingScheme.Logical, _gpioDriver);
            _pinInitializer = new PinInitializer(_gpioController, irrigationOptionsMock.Object);
        }

        /// <summary>
        /// start of the pin initialize should open the master control valve, as well as the zone valves,
        //  and drive the pins high so that the valves are off
        /// </summary>        
        [Fact]
        public async Task StartAsync_OpensMCVAndValvePins()
        {
            var pinsToOpen = new List<int> { _config.MasterControlValveGpio };
            pinsToOpen.AddRange(_config.Valves.Select(v => v.GpioPin));

            await _pinInitializer.StartAsync(CancellationToken.None);

            for (var pin = 0; pin < _gpioDriver.Pins.Count; pin++)
            {
                var shouldBeOpen = false;
                var shouldBeMode = default(PinMode);
                var shouldBePinValue = default(PinValue);

                if (pinsToOpen.Contains(pin))
                {
                    shouldBeOpen = true;
                    shouldBeMode = PinMode.Output;
                    shouldBePinValue = PinValue.High;
                }

                Assert.Equal(shouldBeOpen, _gpioDriver.Pins[pin].IsOpen);
                Assert.Equal(shouldBeMode, _gpioDriver.Pins[pin].Mode);
                Assert.Equal(shouldBePinValue, _gpioDriver.Pins[pin].Value);
            }
        }

        /// <summary>
        /// Stopping the initializer should drive the pins to high to close the valves and then close the pins
        /// </summary>
        [Fact]
        public async Task StopAsync_ClosesAllValvesAndPins()
        {
            var pinsToClose = new List<int> { _config.MasterControlValveGpio };
            pinsToClose.AddRange(_config.Valves.Select(v => v.GpioPin));

            //we need to have the controller open the pins so that the initizer can close them on stop
            foreach (var pin in pinsToClose)
            { _gpioController.OpenPin(pin, PinMode.Output); }

            await _pinInitializer.StopAsync(CancellationToken.None);

            for (var pin = 0; pin < _gpioDriver.Pins.Count; pin++)
            {
                //ensure that the desired pins are in their should-be state. Otherwise, they should be untouched.
                var shouldBeOpen = _gpioDriver.Pins[pin].IsOpen;
                var shouldBeMode = _gpioDriver.Pins[pin].Mode;
                var shouldBePinValue = _gpioDriver.Pins[pin].Value;

                if (pinsToClose.Contains(pin))
                {
                    shouldBeOpen = false;
                    shouldBeMode = PinMode.Output;
                    shouldBePinValue = PinValue.High;
                }

                Assert.Equal(shouldBeOpen, _gpioDriver.Pins[pin].IsOpen);
                Assert.Equal(shouldBeMode, _gpioDriver.Pins[pin].Mode);
                Assert.Equal(shouldBePinValue, _gpioDriver.Pins[pin].Value);
            }
        }
    }
}
