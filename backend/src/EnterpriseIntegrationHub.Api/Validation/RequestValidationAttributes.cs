using System.ComponentModel.DataAnnotations;

namespace EnterpriseIntegrationHub.Api.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
internal sealed class NotEmptyGuidAttribute : ValidationAttribute
{
    public NotEmptyGuidAttribute() : base("{0} must be a non-empty GUID.") { }

    public override bool IsValid(object? value) =>
        value is Guid identifier && identifier != Guid.Empty;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
internal sealed class DefinedEnumAttribute : ValidationAttribute
{
    private readonly Type _enumType;

    public DefinedEnumAttribute(Type enumType) : base("{0} has an invalid value.")
    {
        if (!enumType.IsEnum)
            throw new ArgumentException("The supplied type must be an enum.", nameof(enumType));

        _enumType = enumType;
    }

    public override bool IsValid(object? value) =>
        value is not null && Enum.IsDefined(_enumType, value);
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
internal sealed class NotWhiteSpaceAttribute : ValidationAttribute
{
    public NotWhiteSpaceAttribute() : base("{0} is required.") { }

    public override bool IsValid(object? value) =>
        value is string text && !string.IsNullOrWhiteSpace(text);
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
internal sealed class HttpUrlAttribute : ValidationAttribute
{
    public HttpUrlAttribute() : base("{0} must be a valid absolute HTTP/HTTPS URL.") { }

    public override bool IsValid(object? value) =>
        value is string text &&
        Uri.TryCreate(text, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

