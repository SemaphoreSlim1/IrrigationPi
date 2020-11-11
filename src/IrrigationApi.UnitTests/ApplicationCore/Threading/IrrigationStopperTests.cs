using IrrigationApi.ApplicationCore.Threading;
using System;
using Xunit;

namespace IrrigationApi.UnitTests.ApplicationCore.Threading
{
    public class IrrigationStopperTests
    {
        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void RequestStop_RaisesStopEvent(bool wireEvent, bool expectedRaised)
        {
            var stopRaised = false;

            void OnStop(object sender, EventArgs e)
            {
                stopRaised = true;
            }

            var stopper = new IrrigationStopper();

            if (wireEvent)
            { stopper.StopRequested += OnStop; }

            stopper.RequestStop();

            Assert.Equal(expectedRaised, stopRaised);
        }
    }
}
