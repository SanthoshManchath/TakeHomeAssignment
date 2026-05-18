namespace WifiProvisioning.Core.Exceptions;

/// <summary>
/// Base type for all WiFi-provisioning-related failures.
/// Never thrown directly — throw one of the concrete subclasses.
/// </summary>
public abstract class ProvisioningException : Exception
{
    public ProvisioningException(string message) : base(message)
    {
    }

    public ProvisioningException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
/// Thrown when a speed code cannot be resolved to an available profile.
/// </summary>
public sealed class SpeedNotAvailableException : ProvisioningException
{
    public SpeedNotAvailableException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Thrown when the Speed Profile downstream API fails or returns an invalid response.
/// </summary>
public sealed class SpeedProfileServiceException : ProvisioningException
{
    public SpeedProfileServiceException(string message)
        : base(message)
    {
    }

    public SpeedProfileServiceException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>
/// Thrown when the Activation downstream API fails or returns an invalid response.
/// </summary>
public sealed class ActivationServiceException : ProvisioningException
{
    public ActivationServiceException(string message)
        : base(message)
    {
    }

    public ActivationServiceException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>
/// Thrown when an incoming TM Forum order is structurally valid JSON but
/// fails domain validation (e.g., a required characteristic is missing).
/// </summary>
public sealed class InvalidProvisioningOrderException : ProvisioningException
{
    public InvalidProvisioningOrderException(string message)
        : base(message)
    {
    }
}
}
