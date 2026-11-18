using Alta.ScreenshotCamera;
using HarmonyLib;
using UnityEngine;

namespace Lens.Tweaks;

// Cameras that render the scene again: the viewfinder of a camera another player holds (the game shows one of them),
// and the scene's "Overview Camera", which FastTale found rendering in VR. It's found by name, as nothing in the
// game's code refers to it.
internal static class Cameras
{
    private const string OverviewName = "Overview Camera";

    private static Camera overview;
    private static bool overviewOff;
    private static float nextSearch;

    public static void Apply()
    {
        LensMod.Patch(AccessTools.Method(typeof(HandCamera), "VisibilityUpdated"), "OtherPlayersCameras", typeof(Cameras), prefix: nameof(Visibility));
        LensConfig.Changed += Refresh;
        if (!LensConfig.OtherPlayersCameras) LensMod.Active.Add("other players' viewfinders hidden");
        if (!LensConfig.OverviewCamera) LensMod.Active.Add("Overview Camera off");
    }

    // Another player's camera coming into view stays off.
    private static bool Visibility(HandCamera __instance)
    {
        if (LensConfig.OtherPlayersCameras || __instance.IsLocalPlayer) return true;
        __instance.enabled = false;
        return false;
    }

    private static void Refresh()
    {
        if (!LensConfig.OtherPlayersCameras)
            foreach (var camera in Object.FindObjectsOfType<HandCamera>())
                if (!camera.IsLocalPlayer && camera.enabled) camera.enabled = false;
        nextSearch = 0f;
    }

    public static void Tick()
    {
        if (overview == null)
        {
            if (LensConfig.OverviewCamera || Time.unscaledTime < nextSearch) return;
            nextSearch = Time.unscaledTime + 1f;
            // A disabled camera isn't in Camera.allCameras, so it's kept once found; the headset's eyes are skipped.
            foreach (var camera in Camera.allCameras)
                if (camera != null && camera.name == OverviewName && !camera.stereoEnabled)
                {
                    overview = camera;
                    break;
                }
            if (overview == null) return;
        }
        if (!LensConfig.OverviewCamera && overview.enabled)
        {
            overview.enabled = false;
            overviewOff = true;
        }
        else if (LensConfig.OverviewCamera && overviewOff)
        {
            overview.enabled = true; // only one Lens turned off
            overviewOff = false;
        }
    }

    internal static Camera Overview => overview;
}
