namespace MarketPulse.Application.Alerts;

public static class AlertDestination
{
    public static bool TryValidate(string? value, bool allowHttpLoopback, out Uri uri, out string error)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(value) || value.Length > 2048)
        {
            error = "Destination must be a URL under 2048 characters.";
            return false;
        }

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out uri!))
        {
            error = "Destination must be an absolute URL.";
            return false;
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            error = "Destination must not include credentials.";
            uri = null!;
            return false;
        }

        if (uri.Scheme == Uri.UriSchemeHttps)
        {
            error = string.Empty;
            return true;
        }

        if (allowHttpLoopback
            && uri.Scheme == Uri.UriSchemeHttp
            && (uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)))
        {
            error = string.Empty;
            return true;
        }

        error = "Destination must be https, or http on localhost for local tests.";
        uri = null!;
        return false;
    }
}
