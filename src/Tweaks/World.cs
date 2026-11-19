using System.Collections.Generic;
using UnityEngine;

namespace Lens.Tweaks;

// Settings for things that load with the world rather than being set up once: small meshes' shadows, skin animated
// out of view, and water that mirrors the scene. Every 5 s Lens looks for new ones, but only for a setting that's
// changed (small shadows: every 10 s); clearing a setting puts back what each had.
internal static class World
{
    private static readonly Dictionary<MeshRenderer, UnityEngine.Rendering.ShadowCastingMode> Shadowless = new();
    private static float small, nextSmall;
    private static readonly Dictionary<SkinnedMeshRenderer, bool> Offscreen = new();
    private static readonly Dictionary<StylizedWaterShader.StylizedWater, bool> Mirrors = new();
    private static float nextScan;

    public static void Apply()
    {
        LensConfig.Changed += () => { nextScan = nextSmall = 0f; Tick(); };
        if (LensConfig.SmallShadowSize > 0f) LensMod.Active.Add($"no shadows from things under {LensConfig.SmallShadowSize:0.##} m");
        if (!LensConfig.OffscreenSkinning) LensMod.Active.Add("no skinning out of view");
        if (!LensConfig.WaterReflections) LensMod.Active.Add("no water reflections");
    }

    public static void Tick()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 5f;
        SmallShadows();
        Hold(Offscreen, !LensConfig.OffscreenSkinning, false, () => Object.FindObjectsOfType<SkinnedMeshRenderer>(),
            r => r.updateWhenOffscreen, (r, v) => r.updateWhenOffscreen = v);
        Hold(Mirrors, !LensConfig.WaterReflections, false, () => Object.FindObjectsOfType<StylizedWaterShader.StylizedWater>(),
            w => w.useReflection, (w, v) => w.useReflection = v);
    }

    // Every mesh in the world smaller than SmallShadowSize stops casting shadows; a changed size puts them all back first.
    private static void SmallShadows()
    {
        if (small != LensConfig.SmallShadowSize)
        {
            foreach (var kv in Shadowless)
                if (kv.Key != null) kv.Key.shadowCastingMode = kv.Value;
            Shadowless.Clear();
            small = LensConfig.SmallShadowSize;
        }
        if (small <= 0f || Time.unscaledTime < nextSmall) return;
        nextSmall = Time.unscaledTime + 10f;
        foreach (var r in Object.FindObjectsOfType<MeshRenderer>())
        {
            if (r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off || Shadowless.ContainsKey(r) || r.bounds.size.magnitude >= small) continue;
            Shadowless[r] = r.shadowCastingMode;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    // While wanted, each one found is set to value (its own kept); once not, each is put back.
    private static void Hold<T>(Dictionary<T, bool> held, bool want, bool value, System.Func<T[]> find, System.Func<T, bool> read, System.Action<T, bool> write)
        where T : Object
    {
        if (!want)
        {
            if (held.Count == 0) return;
            foreach (var kv in held)
                if (kv.Key != null) write(kv.Key, kv.Value);
            held.Clear();
            return;
        }
        foreach (var o in find())
        {
            if (o == null || held.ContainsKey(o) || read(o) == value) continue;
            held[o] = read(o);
            write(o, value);
        }
        var gone = new List<T>();
        foreach (var o in held.Keys)
            if (o == null) gone.Add(o);
        foreach (var o in gone) held.Remove(o);
    }
}
