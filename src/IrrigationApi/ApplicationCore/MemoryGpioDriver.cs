using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Device.Gpio;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace IrrigationApi.ApplicationCore
{
    //excluding because this is to facilitate local testing/execution and unit tests. 
    //This class does not execute in a production environment
    [ExcludeFromCodeCoverage]
    public class MemoryGpioDriver : GpioDriver
    {
        public class PinData
        {
            private readonly int _pinNumber;

            public bool IsOpen { get; set; }
            public PinMode Mode { get; set; }

            private PinValue _value;
            public PinValue Value
            {
                get => _value;
                set
                {
                    if (_value == value)
                    {
                        //nothing to do
                    }
                    else if (_value == PinValue.High)
                    {
                        _value = value;
                        ValueFalling?.Invoke(this, new PinValueChangedEventArgs(PinEventTypes.Falling, _pinNumber));
                    }
                    else
                    {
                        _value = value;
                        ValueRising?.Invoke(this, new PinValueChangedEventArgs(PinEventTypes.Rising, _pinNumber));
                    }
                }
            }

            public event PinChangeEventHandler ValueRising;
            public event PinChangeEventHandler ValueFalling;

            public PinData(int pinNumber)
            {
                _pinNumber = pinNumber;
            }
        }

        protected override int PinCount => Pins.Count;

        public ReadOnlyDictionary<int, PinData> Pins { get; }


        public MemoryGpioDriver(int pinCount)
        {
            var initialData = new Dictionary<int, PinData>();
            for (var i = 0; i < pinCount; i++)
            {
                var data = new PinData(i);
                initialData[i] = data;
            }

            Pins = new ReadOnlyDictionary<int, PinData>(initialData);
        }



        protected override void ClosePin(int pinNumber)
        {
            Pins[pinNumber].IsOpen = false;
        }

        protected override void OpenPin(int pinNumber)
        {
            Pins[pinNumber].IsOpen = true;
        }

        protected override int ConvertPinNumberToLogicalNumberingScheme(int pinNumber) => pinNumber;

        protected override void SetPinMode(int pinNumber, PinMode mode)
        {
            Pins[pinNumber].Mode = mode;
        }

        protected override PinMode GetPinMode(int pinNumber) => Pins[pinNumber].Mode;

        protected override bool IsPinModeSupported(int pinNumber, PinMode mode) => true;

        protected override PinValue Read(int pinNumber) => Pins[pinNumber].Value;
        protected override void Write(int pinNumber, PinValue value)
        {
            Pins[pinNumber].Value = value;
        }

        protected override void AddCallbackForPinValueChangedEvent(int pinNumber, PinEventTypes eventTypes, PinChangeEventHandler callback)
        {
            switch (eventTypes)
            {
                case PinEventTypes.Rising:
                    Pins[pinNumber].ValueRising += callback;
                    break;
                case PinEventTypes.Falling:
                    Pins[pinNumber].ValueFalling += callback;
                    break;
                default:
                    break;
            }
        }
        protected override void RemoveCallbackForPinValueChangedEvent(int pinNumber, PinChangeEventHandler callback)
        {

            Pins[pinNumber].ValueRising -= callback;
            Pins[pinNumber].ValueFalling -= callback;
        }


        protected override WaitForEventResult WaitForEvent(int pinNumber, PinEventTypes eventTypes, CancellationToken cancellationToken)
        {

            var eventDetected = false;
            var detectedEventType = PinEventTypes.None;

            var callbackCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            PinChangeEventHandler callBack = (object s, PinValueChangedEventArgs e) =>
            {
                detectedEventType = e.ChangeType;
                eventDetected = true;
                callbackCts.Cancel();
            };

            //wire up a callback event to detect when the pin changes value
            switch (eventTypes)
            {
                case PinEventTypes.Rising:
                    Pins[pinNumber].ValueRising += callBack;
                    break;
                case PinEventTypes.Falling:
                    Pins[pinNumber].ValueFalling += callBack;
                    break;
                default:
                    break;
            }

            //because our callback cts is linked to the external cancel token,
            //our delay will end when either one sends the cancellation signal
            try { Task.Delay(Timeout.Infinite, callbackCts.Token).Wait(); }
            catch { }

            //unwire our event
            switch (eventTypes)
            {
                case PinEventTypes.Rising:
                    Pins[pinNumber].ValueRising -= callBack;
                    break;
                case PinEventTypes.Falling:
                    Pins[pinNumber].ValueFalling -= callBack;
                    break;
                default:
                    break;
            }


            var result = new WaitForEventResult
            {
                EventTypes = detectedEventType,
                TimedOut = !eventDetected
            };

            return result;
        }


    }
}
