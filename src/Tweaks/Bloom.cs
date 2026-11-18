using System.Collections.Generic;
using HarmonyLib;
using UnityEngine.Rendering.PostProcessing;

namespace Lens.Tweaks;

// Bloom on every post-processing volume, as each is enabled and again after the game resets its profiles (on death,
// respawn and in the character room, which copy the shared profiles afresh). From FastTale.
internal static class Bloom
{
    private static readonly List<PostProcessVolume> Volumes = new();
    // Each profile's own bloom setting, by shared profile: a copy made after Lens turned bloom off would remember off.
    private static readonly Dictionary<PostProcessProfile, bool> Original = new();

    public static void Apply()
    {
        LensMod.Patch(AccessTools.Method(typeof(PostProcessVolume), "OnEnable"), "Bloom", typeof(Bloom), postfix: nameof(VolumeEnabled));
        LensMod.Patch(AccessTools.Method(typeof(Township.PostProcessing.Windows.WindowsPostProcessController), "Reset"), "Bloom",
            typeof(Bloom), postfix: nameof(SetAll));
        LensConfig.Changed += SetAll;
        if (!LensConfig.Bloom) LensMod.Active.Add("bloom off");
    }

    private static void VolumeEnabled(PostProcessVolume __instance)
    {
        if (!Volumes.Contains(__instance)) Volumes.Add(__instance);
        if (!LensConfig.Bloom) Set(__instance, false);
    }

    private static void SetAll()
    {
        for (int i = Volumes.Count - 1; i >= 0; i--)
        {
            if (Volumes[i] == null) Volumes.RemoveAt(i);
            else Set(Volumes[i], LensConfig.Bloom);
        }
    }

    private static void Set(PostProcessVolume volume, bool on)
    {
        // .profile makes a copy when there isn't one yet, so it's only read once there is.
        var profile = volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile;
        if (profile == null || !profile.TryGetSettings<UnityEngine.Rendering.PostProcessing.Bloom>(out var bloom)) return;
        var key = volume.sharedProfile != null ? volume.sharedProfile : profile;
        if (!Original.ContainsKey(key))
            Original[key] = key != profile && key.TryGetSettings<UnityEngine.Rendering.PostProcessing.Bloom>(out var shared) ? shared.active : bloom.active;
        bloom.active = on && Original[key];
    }
}
