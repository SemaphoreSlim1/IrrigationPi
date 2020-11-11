using IrrigationApi.ApplicationCore;
using IrrigationApi.ApplicationCore.Configuration;
using IrrigationApi.ApplicationCore.Threading;
using IrrigationApi.Backround;
using IrrigationApi.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;

namespace IrrigationApi.UnitTests.Background
{
    public class IrrigationProcessorTests
    {
        private readonly ChannelWriter<IrrigationJob> _jobWriter;
        private readonly MemoryGpioDriver _gpioDriver;
        private readonly IrrigationStopper _stopper;
        private readonly IrrigationConfig _config;

        private readonly Mock<ILogger<IrrigationProcessor>> _loggerMock;
        private readonly IrrigationProcessor _processor;

        public IrrigationProcessorTests()
        {
            var channel = Channel.CreateUnbounded<IrrigationJob>(
                new UnboundedChannelOptions()
                {
                    AllowSynchronousContinuations = false,
                    SingleWriter = true,
                    SingleReader = true
                });

            _jobWriter = channel.Writer;

            _gpioDriver = new MemoryGpioDriver(pinCount: 50);

            var gpioController = new GpioController(PinNumberingScheme.Logical, _gpioDriver);

            _stopper = new IrrigationStopper();
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

            _loggerMock = new Mock<ILogger<IrrigationProcessor>>();


            _processor = new IrrigationProcessor(channel.Reader, gpioController, _stopper, irrigationOptionsMock.Object, _loggerMock.Object);

            //before the Microsoft GPIO controller can be used, the pins must be opened and set to the desired input/output state
            //run our initializer code to make that happen
            var pinInitializer = new PinInitializer(gpioController, irrigationOptionsMock.Object);
            pinInitializer.StartAsync(CancellationToken.None).Wait();
        }

        [Fact]
        public async Task Processor_TerminatesEarly_ForStopSignal()
        {
            //create a ridiculously long job for purposes of this unit test
            var job = new IrrigationJob { Duration = TimeSpan.FromMinutes(5), Valve = 1 };

            //queue it for execution 2x, so that way when we cancel mid run on the first one, we can verify the second didn't execute
            await _jobWriter.WriteAsync(job);
            await _jobWriter.WriteAsync(job);

            await _processor.StartAsync(CancellationToken.None);

            //give it a little bit to kick off
            await Task.Delay(TimeSpan.FromSeconds(2));

            _stopper.RequestStop();

            //give it a few seconds to wrap up
            await Task.Delay(TimeSpan.FromSeconds(2));

            Assert.Equal(PinValue.High, _gpioDriver.Pins[1].Value); //master control valve should be off
            Assert.Equal(PinValue.High, _gpioDriver.Pins[2].Value); //irrigation valve should be off

        }
    }
}
