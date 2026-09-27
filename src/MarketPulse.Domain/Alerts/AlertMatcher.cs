using MarketPulse.Domain.Anomaly;

namespace MarketPulse.Domain.Alerts;

public static class AlertMatcher
{
    public const string WebhookChannel = "webhook";

    public static bool Matches(AlertRule rule, AnomalyResult anomaly)
    {
        if (!rule.IsEnabled)
        {
            return false;
        }

        if (!string.Equals(rule.Channel, WebhookChannel, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!SymbolMatches(rule.Symbol, anomaly.Symbol))
        {
            return false;
        }

        return anomaly.Severity >= rule.MinSeverity;
    }

    public static bool SymbolMatches(string ruleSymbol, string anomalySymbol)
    {
        if (string.IsNullOrWhiteSpace(ruleSymbol) || ruleSymbol.Trim() == "*")
        {
            return true;
        }

        return string.Equals(ruleSymbol.Trim(), anomalySymbol.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
