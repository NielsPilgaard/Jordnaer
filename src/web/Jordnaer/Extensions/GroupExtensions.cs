using Jordnaer.Features.Profile;
using Jordnaer.Shared;

namespace Jordnaer.Extensions;

public static class GroupExtensions
{
    /// <summary>
    /// Clears all location fields on a group.
    /// </summary>
    public static void ClearLocation(this Group group)
    {
        group.Address = null;
        group.ZipCode = null;
        group.City = null;
        group.Location = null;
        group.ZipCodeLocation = null;
    }

    /// <summary>
    /// Applies a location result from an address lookup to a group.
    /// Sets the full address along with zip code and city.
    /// </summary>
    public static void ApplyAddressLocation(this Group group, string address, LocationResult result)
    {
        group.Address = address;
        group.ZipCode = result.ZipCode;
        group.City = result.City;
        group.Location = result.Location;
        group.ZipCodeLocation = result.ZipCodeLocation;
    }

    /// <summary>
    /// Applies a location result from a zip code lookup to a group.
    /// Sets only the zip code and city, without a specific address.
    /// </summary>
    public static void ApplyZipCodeLocation(this Group group, LocationResult result)
    {
        group.Address = null; // Don't store address when using zip code only
        group.ZipCode = result.ZipCode;
        group.City = result.City;
        group.Location = result.Location;
        // When using zip code only, both locations are the same (zip code center)
        group.ZipCodeLocation = result.ZipCodeLocation ?? result.Location;
    }
}
