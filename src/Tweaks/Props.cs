using System.Collections.Generic;
using Alta.Chunks;
using HarmonyLib;
using UnityEngine;

namespace Lens.Tweaks;

// The small grass tufts, baked into chunk prefabs under known group names (from FastTale): hidden as each chunk's
// props spawn, and in the props already spawned when the setting changes; shown again when it's cleared.
internal static class Props
{
    private static readonly HashSet<GameObject> Spawned = new();
    private static readonly HashSet<GameObject> HiddenGrass = new();
    private static bool grass = true;

    public static void Apply()
    {
        LensMod.Patch(AccessTools.PropertySetter(typeof(ChunkPrefabPointer.PointerInstance), "Spawned"), "Props", typeof(Props), postfix: nameof(PropsSpawned));
        LensConfig.Changed += Refresh;
        grass = LensConfig.Grass;
        if (!grass) LensMod.Active.Add("grass hidden");
    }

    private static void PropsSpawned(ChunkPrefabPointer.PointerInstance __instance)
    {
        var root = __instance.Spawned;
        if (root == null) return;
        Spawned.Add(root);
        if (!grass) HideGrass(root.transform);
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
    }
}
