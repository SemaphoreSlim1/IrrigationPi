using IrrigationApi.ApplicationCore;
using IrrigationApi.ApplicationCore.Configuration;
using IrrigationApi.ApplicationCore.Hardware;
using IrrigationApi.ApplicationCore.Threading;
using IrrigationApi.Backround;
using IrrigationApi.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;

namespace IrrigationApi.UnitTests.Background
{
    public class IrrigationProcessorTests : IDisposable
    {
        private readonly Channel<IrrigationJob> _channel;
        private readonly MemoryGpioDriver _gpioDriver;
        private readonly IrrigationStopper _stopper;
        private readonly IrrigationConfig _config;

        private readonly IrrigationProcessorStatus _status;
        private readonly Mock<ILogger<IrrigationProcessor>> _loggerMock;
        private readonly IrrigationProcessor _processor;

        public IrrigationProcessorTests()
        {
            _channel = Channel.CreateUnbounded<IrrigationJob>(
                new UnboundedChannelOptions()
                {
                    AllowSynchronousContinuations = false,
                    SingleWriter = true,
                    SingleReader = true
                });

            _gpioDriver = new MemoryGpioDriver(pinCount: 50);

            var gpioController = new GpioController(PinNumberingScheme.Logical, _gpioDriver);

            _stopper = new IrrigationStopper();
            _config = new IrrigationConfig
            {
                MasterControlValveGpio = 1,
                PressureBleedTime = TimeSpan.Zero,
                ZoneSwitchDelay = TimeSpan.Zero,
                Valves = new List<PinMapping>
                {
                    new PinMapping{ GpioPin = 2, ValveNumber = 1},
                    new PinMapping{ GpioPin = 3, ValveNumber = 2},
                    new PinMapping{ GpioPin = 4, ValveNumber = 3}
                }
            };

            var pins = (_config.Valves.Select(v => v.GpioPin).Union(new[] { _config.MasterControlValveGpio })).ToArray();
            var relayBoard = new RelayBoard(RelayType.NormallyOpen, gpioController, pins);

            var irrigationOptionsMock = new Mock<IOptions<IrrigationConfig>>();
            irrigationOptionsMock.SetupGet(x => x.Value).Returns(_config);

            _loggerMock = new Mock<ILogger<IrrigationProcessor>>();
            _status = new IrrigationProcessorStatus();

            _processor = new IrrigationProcessor(_channel.Reader, relayBoard, _stopper, _status, irrigationOptionsMock.Object, _loggerMock.Object);

            //before the Microsoft GPIO controller can be used, the pins must be opened and set to the desired input/output state
            //run our initializer code to make that happen
            var pinInitializer = new PinInitializer(relayBoard, irrigationOptionsMock.Object);
            pinInitializer.StartAsync(CancellationToken.None).Wait();
        }

        [Fact]
        public async Task Processor_TerminatesEarly_ForStopSignal()
        {
            //create a ridiculously long job for purposes of this unit test
            var job = new IrrigationJob { Duration = TimeSpan.FromMinutes(5), Valve = 1 };

            //queue it for execution
            await _channel.Writer.WriteAsync(job);
            await _processor.StartAsync(CancellationToken.None);

            //give it a little bit to kick off
            await Task.Delay(TimeSpan.FromSeconds(1));

            _stopper.RequestStop();

            //give it a few seconds to wrap up
            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.Equal(PinValue.High, _gpioDriver.Pins[1].Value); //master control valve should be off
            Assert.Equal(PinValue.High, _gpioDriver.Pins[2].Value); //irrigation valve should be off
        }

        [Fact]
        public async Task CancellingViaCancellationToken_TerminatesEarly()
        {
            var cts = new CancellationTokenSource();

            //create a ridiculously long job for purposes of this unit test
            var job = new IrrigationJob { Duration = TimeSpan.FromMinutes(5), Valve = 1 };

            //queue it for execution
            await _channel.Writer.WriteAsync(job);
            await _processor.StartAsync(cts.Token);

            //give it a little bit to kick off
            await Task.Delay(TimeSpan.FromSeconds(1));

            //now stop it
            cts.Cancel();

            //give it a few seconds to wrap up
            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.Equal(PinValue.High, _gpioDriver.Pins[1].Value); //master control valve should be off
            Assert.Equal(PinValue.High, _gpioDriver.Pins[2].Value); //irrigation valve should be off
        }

        [Fact]
        public async Task IrrigationJob_Irrigates()
        {
            var job = new IrrigationJob { Duration = TimeSpan.FromSeconds(3), Valve = 1 };

            //queue it for execution
            await _channel.Writer.WriteAsync(job);
            await _processor.StartAsync(CancellationToken.None);

            //give it a little bit to kick off
            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.Equal(PinValue.Low, _gpioDriver.Pins[1].Value); //master control valve should be on
            Assert.Equal(PinValue.Low, _gpioDriver.Pins[2].Value); //irrigation valve should be on

            //give it a few seconds to wrap up
            await Task.Delay(TimeSpan.FromSeconds(3));

            Assert.Equal(PinValue.High, _gpioDriver.Pins[1].Value); //master control valve should be off
            Assert.Equal(PinValue.High, _gpioDriver.Pins[2].Value); //irrigation valve should be off
        }

        [Fact]
        public async Task Processor_ReportsStarted_When_Started()
        {
            await _processor.StartAsync(CancellationToken.None);

            Assert.True(_status.Running);
        }

        [Fact]
        public async Task Processor_ReportsNotRunning_WhenStopped()
        {
            await _processor.StartAsync(CancellationToken.None);
            _channel.Writer.Complete();

            //give a bit to complete
            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.False(_status.Running);
        }

        public void Dispose()
        {
            //force the processor to come to completion so the test does not run unnecessarily long
            _channel.Writer.TryComplete();
            _processor.StopAsync(CancellationToken.None).Wait();
        }
    }
}
