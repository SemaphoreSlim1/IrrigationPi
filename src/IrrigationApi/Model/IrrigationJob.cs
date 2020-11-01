using System;

namespace IrrigationApi.Model
{
    public class IrrigationJob
    {
        public int Valve { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
