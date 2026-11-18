using System.Collections.Generic;
using Alta.Chunks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lens.Tweaks;

// What chunks spawn into the world: the small grass tufts (baked into chunk prefabs under known group names, from
// FastTale) and the shadows of small props. Both act as each chunk's props spawn, and on the props already spawned
// when a setting changes; everything changed is put back as it was when the setting is cleared.
internal static class Props
{
    private static readonly HashSet<GameObject> Spawned = new();
    private static readonly HashSet<GameObject> HiddenGrass = new();
    private static readonly Dictionary<Renderer, ShadowCastingMode> Shadowless = new();
    private static bool grass = true;
    private static float small;

    public static void Apply()
    {
        LensMod.Patch(AccessTools.PropertySetter(typeof(ChunkPrefabPointer.PointerInstance), "Spawned"), "Props", typeof(Props), postfix: nameof(PropsSpawned));
        LensConfig.Changed += Refresh;
        grass = LensConfig.Grass;
        small = LensConfig.SmallShadowSize;
        if (!grass) LensMod.Active.Add("grass hidden");
        if (small > 0f) LensMod.Active.Add($"no shadows from props under {small:0.##} m");
    }

    private static void PropsSpawned(ChunkPrefabPointer.PointerInstance __instance)
    {
        var root = __instance.Spawned;
        if (root == null) return;
        Spawned.Add(root);
        if (!grass) HideGrass(root.transform);
        if (small > 0f) DropShadows(root.transform);
    }

    private static void HideGrass(Transform root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (!t.name.Contains("Grass LO Group") && !t.name.Contains("_Grass_")) continue;
            HiddenGrass.Add(t.gameObject);
            t.gameObject.SetActive(false);
        }
    }

    private static void DropShadows(Transform root)
    {
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r.shadowCastingMode == ShadowCastingMode.Off || Shadowless.ContainsKey(r) || r.bounds.size.magnitude >= small) continue;
            Shadowless[r] = r.shadowCastingMode;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    private static void Refresh()
    {
        Spawned.RemoveWhere(go => go == null);
        if (grass != LensConfig.Grass)
        {
            grass = LensConfig.Grass;
            if (grass)
            {
                foreach (var go in HiddenGrass)
                    if (go != null) go.SetActive(true);
                HiddenGrass.Clear();
            }
            else
                foreach (var root in Spawned) HideGrass(root.transform);
        }
        if (small != LensConfig.SmallShadowSize)
        {
            small = LensConfig.SmallShadowSize;
            foreach (var kv in Shadowless)
                if (kv.Key != null) kv.Key.shadowCastingMode = kv.Value;
            Shadowless.Clear();
            if (small > 0f)
                foreach (var root in Spawned) DropShadows(root.transform);
        }
    }
}
