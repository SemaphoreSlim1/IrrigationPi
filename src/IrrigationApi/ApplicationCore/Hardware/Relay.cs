using System;
using System.Device.Gpio;

namespace IrrigationApi.ApplicationCore.Hardware
{
    public class Relay : IDisposable
    {
        private readonly System.Device.Gpio.PinValue _activeValue;
        private readonly GpioController _gpioController;
        private bool _disposed;

        public int Pin { get; }

        public RelayType Type { get; }

        public bool On
        {
            get => Type == RelayType.NormallyClosed ? _gpioController.Read(Pin) == PinValue.High : _gpioController.Read(Pin) == PinValue.Low;
            set
            {
                var val = Type == RelayType.NormallyClosed ? value : !value;
                _gpioController.Write(Pin, val);
            }
        }

        public Relay(int pin, RelayType relayType, GpioController gpioController)
        {
            Pin = pin;
            Type = relayType;
            _gpioController = gpioController;

            _gpioController.OpenPin(pin, PinMode.Output);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            { return; }

            _gpioController.ClosePin(Pin);
            _disposed = true;
        }

        ~Relay()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
