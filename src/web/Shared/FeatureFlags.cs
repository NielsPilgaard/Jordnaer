using System.Collections.Concurrent;

namespace Jordnaer.Shared;

public static class FeatureFlags
{
    public const string EVENTS = "Events";
    public const string INTERNAL_REGISTRATION = "InternalRegistration";

    /// <summary>
    /// Blazor WASM doesn't support Feature Flags directly, this is used as a workaround
    /// </summary>
    public static readonly ConcurrentDictionary<string, bool> States = new()
    {
        [EVENTS] = false,
        [INTERNAL_REGISTRATION] = false,
    };
}
