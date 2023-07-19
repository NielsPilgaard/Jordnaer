using System.Collections.Concurrent;

namespace Jordnaer.Shared;

public static class FeatureFlags
{
    public const string Events = "Events";
    public const string Internal_Registration = "InternalRegistration";

    /// <summary>
    /// Blazor WASM doesn't support Feature Flags directly, this is used as a workaround
    /// </summary>
    public static readonly ConcurrentDictionary<string, bool> States = new()
    {
        [Events] = false,
        [Internal_Registration] = false,
    };
}
