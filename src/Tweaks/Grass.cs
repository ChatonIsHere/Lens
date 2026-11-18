using System.Collections.Generic;
using Alta.Chunks;
using HarmonyLib;
using UnityEngine;

namespace Lens.Tweaks;

// The small grass tufts, baked into chunk prefabs under known group names: hidden as each chunk's props spawn, and
// in the chunks already loaded when the setting changes. With grass on (the default) the patch returns at once.
// From FastTale.
internal static class Grass
{
    private static readonly HashSet<GameObject> Hidden = new();
    private static bool shown = true;

    public static void Apply()
    {
        LensMod.Patch(AccessTools.PropertySetter(typeof(ChunkPrefabPointer.PointerInstance), "Spawned"), "Grass", typeof(Grass), postfix: nameof(Spawned));
        LensConfig.Changed += Refresh;
        shown = LensConfig.Grass;
        if (!shown) LensMod.Active.Add("grass hidden");
    }

    private static void Spawned(ChunkPrefabPointer.PointerInstance __instance)
    {
        if (shown || __instance.Spawned == null) return;
        Hide(__instance.Spawned.transform);
    }

    private static void Hide(Transform root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (!t.name.Contains("Grass LO Group") && !t.name.Contains("_Grass_")) continue;
            Hidden.Add(t.gameObject);
            t.gameObject.SetActive(false);
        }
    }

    private static void Refresh()
    {
        if (shown == LensConfig.Grass) return;
        shown = LensConfig.Grass;
        if (shown)
        {
            foreach (var go in Hidden)
                if (go != null) go.SetActive(true);
            Hidden.Clear();
            return;
        }
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (var root in scene.GetRootGameObjects()) Hide(root.transform);
        }
    }
}
