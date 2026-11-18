using System.Collections.Generic;
using Alta.Environmental.Atmosphere;
using Alta.Heat;
using Alta.Lighting;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lens.Tweaks;

// The sun's and moon's shadows, and fires' and torches'. The game sets a fire's shadows from its settings each time a
// pooled fire is used again, so Lens sets them after that, and twice a second for fires that moved in or out of
// FireShadowDistance; the sun and moon are set once, when the sky starts.
internal static class Shadows
{
    private static readonly AccessTools.FieldRef<DayTimeManager, Light> Sun = AccessTools.FieldRefAccess<DayTimeManager, Light>("sunLight");
    private static readonly AccessTools.FieldRef<DayTimeManager, Light> Moon = AccessTools.FieldRefAccess<DayTimeManager, Light>("moonLight");
    private static readonly AccessTools.FieldRef<PooledFireEffect, PlatformLight> FireLight = AccessTools.FieldRefAccess<PooledFireEffect, PlatformLight>("target");
    private static readonly AccessTools.FieldRef<PooledFireEffect, FireSettings> FireSetup = AccessTools.FieldRefAccess<PooledFireEffect, FireSettings>("currentSettings");

    private static readonly Dictionary<Light, LightShadows> SkyOriginal = new();
    private static readonly HashSet<PooledFireEffect> Fires = new();
    private static readonly Dictionary<Light, LightShadowResolution> FireResolution = new();
    private static float nextFires;

    public static void Apply()
    {
        LensMod.Patch(AccessTools.Method(typeof(DayTimeManager), "Awake"), "SunShadows", typeof(Shadows), postfix: nameof(SkyStarted));
        LensMod.Patch(AccessTools.Method(typeof(PooledFireEffect), nameof(PooledFireEffect.UpdateSettings)), "FireShadows", typeof(Shadows), postfix: nameof(FireSet));
        LensConfig.Changed += Refresh;
        if (LensConfig.SunShadows.Length > 0) LensMod.Active.Add($"sun shadows {LensConfig.SunShadows.ToLowerInvariant()}");
        if (LensConfig.FireShadows.Length > 0) LensMod.Active.Add($"fire shadows {LensConfig.FireShadows.ToLowerInvariant()}");
        if (LensConfig.FireShadowDistance > 0f) LensMod.Active.Add($"fire shadows within {LensConfig.FireShadowDistance:0} m");
        if (FireResolutionMode(out var r)) LensMod.Active.Add($"fire shadow resolution {r}");
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
        Fires.Add(__instance);
        Fire(__instance, Head());
    }

    public static void Tick()
    {
        if (Fires.Count == 0 || Time.unscaledTime < nextFires) return;
        nextFires = Time.unscaledTime + 0.5f;
        Fires.RemoveWhere(f => f == null);
        if (LensConfig.FireShadowDistance <= 0f) return;
        var head = Head();
        foreach (var fire in Fires)
            if (fire.isActiveAndEnabled) Fire(fire, head);
    }

    private static Vector3? Head() => PlayerController.Current != null && PlayerController.Current.Head != null ? PlayerController.Current.Head.position : (Vector3?)null;

    private static void Fire(PooledFireEffect fire, Vector3? head)
    {
        var light = FireLight(fire);
        var settings = FireSetup(fire);
        if (light == null || settings == null || light.TargetLight == null) return;
        var mode = Mode(LensConfig.FireShadows) ?? settings.Shadows;
        if (mode == LightShadows.Hard && settings.Shadows == LightShadows.None) mode = LightShadows.None;
        float far = LensConfig.FireShadowDistance;
        if (far > 0f && head != null && (light.TargetLight.transform.position - head.Value).sqrMagnitude > far * far) mode = LightShadows.None;
        if (light.Shadows != mode) light.Shadows = mode;
        var target = light.TargetLight;
        if (!FireResolution.ContainsKey(target)) FireResolution[target] = target.shadowResolution;
        var resolution = FireResolutionMode(out var r) ? r : FireResolution[target];
        if (target.shadowResolution != resolution) target.shadowResolution = resolution;
    }

    private static bool FireResolutionMode(out LightShadowResolution resolution)
    {
        resolution = LightShadowResolution.FromQualitySettings;
        return System.Enum.TryParse(LensConfig.FireShadowResolution, true, out resolution)
               && resolution != LightShadowResolution.FromQualitySettings && System.Enum.IsDefined(typeof(LightShadowResolution), resolution);
    }
}
