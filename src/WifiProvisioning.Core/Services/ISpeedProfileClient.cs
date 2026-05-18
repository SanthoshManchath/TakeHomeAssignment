using WifiProvisioning.Core.Models.Domain;
using WifiProvisioning.Core.Models.Input;

namespace WifiProvisioning.Core.Services;
/// <summary>
/// Client for the Network Infrastructure API.
/// Fetches the catalog of available speed profiles supported on the network
/// and returns the one whose <c>profileId</c> matches the supplied speed code.
/// </summary>
public interface ISpeedProfileClient
{
    Task<SpeedProfile> GetSpeedProfileAsync(string speedCode, CancellationToken cancellationToken);
}
