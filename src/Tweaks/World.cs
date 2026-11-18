using System.Collections.Generic;
using UnityEngine;

namespace Lens.Tweaks;

// Settings for things that load with the world rather than being set up once: terrain instancing, skin animated
// out of view, and water that mirrors the scene. Every 5 s Lens looks for new ones, but only for a setting that's
// changed; clearing a setting puts back what each had.
internal static class World
{
    private static readonly Dictionary<Terrain, bool> Instanced = new();
    private static readonly Dictionary<SkinnedMeshRenderer, bool> Offscreen = new();
    private static readonly Dictionary<StylizedWaterShader.StylizedWater, bool> Mirrors = new();
    private static float nextScan;

    public static void Apply()
    {
        LensConfig.Changed += () => { nextScan = 0f; Tick(); };
        if (LensConfig.TerrainInstancing) LensMod.Active.Add("terrain instancing");
        if (!LensConfig.OffscreenSkinning) LensMod.Active.Add("no skinning out of view");
        if (!LensConfig.WaterReflections) LensMod.Active.Add("no water reflections");
    }

    public static void Tick()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 5f;
        Hold(Instanced, LensConfig.TerrainInstancing, true, Terrain.activeTerrains, t => t.drawInstanced, (t, v) => t.drawInstanced = v);
        Hold(Offscreen, !LensConfig.OffscreenSkinning, false, () => Object.FindObjectsOfType<SkinnedMeshRenderer>(),
            r => r.updateWhenOffscreen, (r, v) => r.updateWhenOffscreen = v);
        Hold(Mirrors, !LensConfig.WaterReflections, false, () => Object.FindObjectsOfType<StylizedWaterShader.StylizedWater>(),
            w => w.useReflection, (w, v) => w.useReflection = v);
    }

    private static void Hold<T>(Dictionary<T, bool> held, bool want, bool value, T[] found, System.Func<T, bool> read, System.Action<T, bool> write)
        where T : Object => Hold(held, want, value, () => found, read, write);

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
