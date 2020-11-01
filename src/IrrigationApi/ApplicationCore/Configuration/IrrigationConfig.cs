using System;
using System.Collections.Generic;

namespace IrrigationApi.ApplicationCore.Configuration
{
    public class IrrigationConfig
    {
        public TimeSpan PressureBleedTime { get; set; }
        public int MasterControlValveGpio { get; set; }
        public List<PinMapping> Valves { get; set; }
    }

    public class PinMapping
    {
        public int GpioPin { get; set; }
        public int ValveNumber { get; set; }
    }

    public class HardwareConfig
    {
        public bool UseMemoryDriver { get; set; }
    }
}
