using MarketPulse.Domain.Anomaly;
using MarketPulse.Domain.Features;

namespace MarketPulse.Domain.Interfaces;

public interface IAnomalyDetectionEngine
{
    /// <summary>
    /// Returns null when history is thin or the combined score is below the configured record threshold.
    /// </summary>
    AnomalyResult? Evaluate(MarketFeatures features);
}
