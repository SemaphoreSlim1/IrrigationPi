using IrrigationApi.ApplicationCore.Threading;
using IrrigationApi.Controllers;
using IrrigationApi.Model;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;

namespace IrrigationApi.UnitTests.Controllers
{
    public class IrrigateControllerTests
    {
        private readonly Channel<IrrigationJob> _channel;
        private readonly Mock<IIrrigationStopper> _stopperMock;
        private readonly IrrigateController _controller;

        public IrrigateControllerTests()
        {
            _channel = Channel.CreateUnbounded<IrrigationJob>();
            _stopperMock = new Mock<IIrrigationStopper>();
            var logger = new Mock<ILogger<IrrigateController>>();

            _controller = new IrrigateController(_channel.Writer, _stopperMock.Object, logger.Object);
        }

        [Fact]
        private async Task Irrigate_CreatesIrrigationJob()
        {
            var job = new IrrigationJob();

            var result = await _controller.Irrigate(new[] { job });
            _channel.Writer.Complete();

            var writtenJobs = new List<IrrigationJob>();

            await foreach (var writtenJob in _channel.Reader.ReadAllAsync())
            {
                writtenJobs.Add(writtenJob);
            }
        }

        [Fact]
        public void Stop_RequestsStop()
        {
            var result = _controller.Stop();

            _stopperMock.Verify(x => x.RequestStop(), Times.Once());
        }
    }
}
