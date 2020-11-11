using System;
using System.Diagnostics.CodeAnalysis;

namespace IrrigationApi.Model
{
    [ExcludeFromCodeCoverage]
    public record IrrigationJob
    {
        public int Valve { get; init; }
        public TimeSpan Duration { get; init; }
    }
}
