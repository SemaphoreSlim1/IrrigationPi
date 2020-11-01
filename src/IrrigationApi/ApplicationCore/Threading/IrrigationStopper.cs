using System;

namespace IrrigationApi.ApplicationCore.Threading
{
    public interface IIrrigationStopper
    {
        event EventHandler StopRequested;

        void RequestStop();
    }

    public class IrrigationStopper : IIrrigationStopper
    {
        public event EventHandler StopRequested;

        public void RequestStop()
        {
            StopRequested?.Invoke(this, new EventArgs());
        }
    }
}
