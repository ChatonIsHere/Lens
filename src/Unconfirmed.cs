using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using UnityEngine;

namespace Lens;

// Settings changed on the tablet apply at once but aren't kept until confirmed, the way a display mode change works:
// left unconfirmed for 15 s after the last change, every setting goes back to what it was before the first. Nothing
// is saved until then, and quitting with changes unconfirmed puts them back first.
internal static class Unconfirmed
{
    public const float Seconds = 15f;

    private static Dictionary<MelonPreferences_Entry, object> before;
    private static float deadline;

    public static bool Pending => before != null;
    public static int SecondsLeft => Mathf.Max(0, Mathf.CeilToInt(deadline - Time.unscaledTime));

    public static void Set<T>(MelonPreferences_Entry<T> entry, T value)
    {
        before ??= LensConfig.Entries.ToDictionary(e => e, e => e.BoxedValue);
        deadline = Time.unscaledTime + Seconds;
        entry.Value = value;
    }

    public static void Keep()
    {
        if (before == null) return;
        before = null;
        LensConfig.Save();
        LensMod.Log.Msg("Kept the new settings.");
    }

    public static void Revert(string why = "")
    {
        if (before == null) return;
        var old = before;
        before = null;
        foreach (var kv in old)
            if (!Equals(kv.Key.BoxedValue, kv.Value)) kv.Key.BoxedValue = kv.Value;
        LensConfig.Save();
        LensMod.Log.Msg("Put the settings back" + why + ".");
    }

    public static void Tick()
    {
        if (before != null && Time.unscaledTime > deadline) Revert(" (not kept within 15 s)");
    }
}
