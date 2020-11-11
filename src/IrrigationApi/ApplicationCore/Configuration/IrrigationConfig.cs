using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace IrrigationApi.ApplicationCore.Configuration
{
    [ExcludeFromCodeCoverage]
    public class IrrigationConfig
    {
        public TimeSpan PressureBleedTime { get; set; }
        public int MasterControlValveGpio { get; set; }
        public List<PinMapping> Valves { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class PinMapping
    {
        public int GpioPin { get; set; }
        public int ValveNumber { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class HardwareConfig
    {
        public bool UseMemoryDriver { get; set; }
    }
}
