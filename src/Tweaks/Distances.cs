using System.Reflection;
using Alta.Utilities;
using HarmonyLib;
using UnityEngine;

namespace Lens.Tweaks;

// How far things are drawn: trees' detailed models (the game's own LOD groups, not Unity's), the view distance (the
// camera's far clip, which the game's screen fader puts back after each fade) and the cull distance for small things
// on the default layer (the game's only per-layer distance).
internal static class Distances
{
    private static readonly FieldInfo FaderPlayer = AccessTools.Field(typeof(ExposurePlayerScreenFader), "playerController");
    private static readonly AccessTools.FieldRef<ExposurePlayerScreenFader, float> FaderFar = AccessTools.FieldRefAccess<ExposurePlayerScreenFader, float>("fullFarDistance");

    private static float gameFar = -1f, gameCull = -1f;
    private static bool treesPatched;

    public static void Apply()
    {
        LensMod.Patch(AccessTools.Method(typeof(ExposurePlayerScreenFader), "Awake"), "ViewDistance", typeof(Distances), postfix: nameof(FaderAwake));
        LensMod.Patch(AccessTools.Method(typeof(PlayerController), "UpdateCameraCulling"), "CullDistance", typeof(Distances), postfix: nameof(Culling));
        LensConfig.Changed += Refresh;
        Trees();
        if (LensConfig.TreeDistance != 1f) LensMod.Active.Add($"tree detail distance x{LensConfig.TreeDistance:0.##}");
        if (LensConfig.ViewDistance > 0f) LensMod.Active.Add($"view distance {LensConfig.ViewDistance:0} m");
        if (LensConfig.CullDistance > 0f) LensMod.Active.Add($"small things to {LensConfig.CullDistance:0} m");
    }

    private static void Refresh()
    {
        Trees();
        foreach (var fader in Object.FindObjectsOfType<ExposurePlayerScreenFader>()) FaderAwake(fader);
        var player = PlayerController.Current;
        if (player != null && player.Camera != null && gameCull >= 0f) SetCulling(player.Camera);
    }

    // Patched only once a tree distance is set: it runs for every tree, every LOD update.
    private static void Trees()
    {
        if (treesPatched || LensConfig.TreeDistance == 1f) return;
        treesPatched = LensMod.Patch(AccessTools.Method(typeof(DistanceLODGroup), nameof(DistanceLODGroup.GetLevel)), "TreeDistance",
            typeof(Distances), prefix: nameof(TreeLevel));
    }

    // Each tree picks its model as if it were 1/TreeDistance times as far away.
    private static void TreeLevel(ref float distanceSquared)
    {
        float scale = LensConfig.TreeDistance;
        if (scale != 1f) distanceSquared /= scale * scale;
    }

    private static void FaderAwake(ExposurePlayerScreenFader __instance)
    {
        if (gameFar < 0f) gameFar = FaderFar(__instance);
        float far = LensConfig.ViewDistance > 0f ? LensConfig.ViewDistance : gameFar;
        FaderFar(__instance) = far;
        // Mid-fade the camera sees 1 m; the fader puts the full distance back when it ends.
        if (FaderPlayer?.GetValue(__instance) is PlayerController player && player.Camera != null && player.Camera.farClipPlane > 1f)
            player.Camera.farClipPlane = far;
    }

#pragma warning disable CS0618 // GameSettings is obsolete in the game, but it's what this method of the game's takes
    private static void Culling(PlayerController __instance, GameSettings settings)
    {
        gameCull = settings.DefaultCullingDistance;
        if (__instance.Camera != null) SetCulling(__instance.Camera);
    }
#pragma warning restore CS0618

    private static void SetCulling(Camera camera)
    {
        var distances = camera.layerCullDistances;
        distances[0] = LensConfig.CullDistance > 0f ? LensConfig.CullDistance : gameCull;
        camera.layerCullDistances = distances;
    }
}
