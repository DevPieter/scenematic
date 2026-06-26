using System.ComponentModel.DataAnnotations;

namespace Scenematic.Common.Annotations;

public class ValidWebUrlAttribute : ValidationAttribute
{
    private readonly bool _requireHttps;

    public ValidWebUrlAttribute(bool requireHttps = false)
    {
        _requireHttps = requireHttps;

        ErrorMessage = requireHttps
            ? "The field must be a valid HTTPS URL."
            : "The field must be a valid HTTP or HTTPS URL.";
    }

    public override bool IsValid(object? value)
    {
        if (value is null) return true;
        if (value is not string url || string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uriResult)) return false;

        if (uriResult.Scheme == Uri.UriSchemeHttps) return true;
        return !_requireHttps && uriResult.Scheme == Uri.UriSchemeHttp;
    }
}