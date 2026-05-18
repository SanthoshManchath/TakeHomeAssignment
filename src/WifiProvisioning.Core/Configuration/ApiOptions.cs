using System.Diagnostics.CodeAnalysis;

namespace WifiProvisioning.Core.Configuration;
/// <summary>
/// Configuration for the Network Infrastructure API (speed profile catalog).
/// </summary>
/// 
[ExcludeFromCodeCoverage] 
public sealed class SpeedProfileApiOptions
{
    public const string SectionName = "SpeedProfileApi";

    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 10;
}

/// <summary>
/// Configuration for the Network Controller API (activation).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ActivationApiOptions
{
    public const string SectionName = "ActivationApi";

    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 15;
}
