using System.Collections.Generic;
using Alta.Environmental.Atmosphere;
using Alta.Heat;
using Alta.Lighting;
using HarmonyLib;
using UnityEngine;

namespace Lens.Tweaks;

// The sun's and moon's shadows, and fires' and torches'. The game sets a fire's shadows from its settings each time a
// pooled fire is used again, so Lens sets them after that; the sun and moon are set once, when the sky starts.
internal static class Shadows
{
    private static readonly AccessTools.FieldRef<DayTimeManager, Light> Sun = AccessTools.FieldRefAccess<DayTimeManager, Light>("sunLight");
    private static readonly AccessTools.FieldRef<DayTimeManager, Light> Moon = AccessTools.FieldRefAccess<DayTimeManager, Light>("moonLight");
    private static readonly AccessTools.FieldRef<PooledFireEffect, PlatformLight> FireLight = AccessTools.FieldRefAccess<PooledFireEffect, PlatformLight>("target");
    private static readonly AccessTools.FieldRef<PooledFireEffect, FireSettings> FireSetup = AccessTools.FieldRefAccess<PooledFireEffect, FireSettings>("currentSettings");

    private static readonly Dictionary<Light, LightShadows> SkyOriginal = new();

    public static void Apply()
    {
        LensMod.Patch(AccessTools.Method(typeof(DayTimeManager), "Awake"), "SunShadows", typeof(Shadows), postfix: nameof(SkyStarted));
        LensMod.Patch(AccessTools.Method(typeof(PooledFireEffect), nameof(PooledFireEffect.UpdateSettings)), "FireShadows", typeof(Shadows), postfix: nameof(FireSet));
        LensConfig.Changed += Refresh;
        if (LensConfig.SunShadows.Length > 0) LensMod.Active.Add($"sun shadows {LensConfig.SunShadows.ToLowerInvariant()}");
        if (LensConfig.FireShadows.Length > 0) LensMod.Active.Add($"fire shadows {LensConfig.FireShadows.ToLowerInvariant()}");
    }

    private static void Refresh()
    {
        foreach (var sky in Object.FindObjectsOfType<DayTimeManager>()) SkyStarted(sky);
        foreach (var fire in Object.FindObjectsOfType<PooledFireEffect>()) FireSet(fire);
    }

    private static LightShadows? Mode(string setting) =>
        LensConfig.Off(setting) ? LightShadows.None : LensConfig.Hard(setting) ? LightShadows.Hard : (LightShadows?)null;

    private static void SkyStarted(DayTimeManager __instance)
    {
        foreach (var light in new[] { Sun(__instance), Moon(__instance) })
        {
            if (light == null) continue;
            if (!SkyOriginal.ContainsKey(light)) SkyOriginal[light] = light.shadows;
            var mode = Mode(LensConfig.SunShadows) ?? SkyOriginal[light];
            // Hard never makes a light without shadows cast them.
            light.shadows = mode == LightShadows.Hard && SkyOriginal[light] == LightShadows.None ? LightShadows.None : mode;
        }
    }

    private static void FireSet(PooledFireEffect __instance)
    {
        var light = FireLight(__instance);
        var settings = FireSetup(__instance);
        if (light == null || settings == null) return;
        var mode = Mode(LensConfig.FireShadows) ?? settings.Shadows;
        light.Shadows = mode == LightShadows.Hard && settings.Shadows == LightShadows.None ? LightShadows.None : mode;
    }
}
