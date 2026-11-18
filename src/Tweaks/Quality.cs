using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.XR;

namespace Lens.Tweaks;

// Unity's quality settings: render scale, anti-aliasing, shadows, LOD bias, pixel lights. The game sets some of them
// itself (its OptimizationManager moves LOD bias and shadow distance and resolution with the frame rate, and the
// light count comes from its settings), so each is put back straight after the game's own and checked once a second.
internal static class Quality
{
    // One Unity value Lens may hold: the game's own is kept while Lens holds it, so letting go puts it back.
    private sealed class Held<T>
    {
        private readonly string name;
        private readonly Func<T> read;
        private readonly Action<T> write;
        private bool holding;
        private T game;

        public Held(string name, Func<T> read, Action<T> write)
        {
            this.name = name;
            this.read = read;
            this.write = write;
        }

        public void Set(bool want, T value)
        {
            if (want)
            {
                var now = read();
                if (!holding) game = now;
                else if (!Equals(now, value)) game = now; // the game set its own since
                holding = true;
                if (!Equals(now, value)) write(value);
            }
            else if (holding)
            {
                holding = false;
                write(game);
            }
        }

        public override string ToString() => name;
    }

    private static readonly Held<float> Scale = new("render scale", () => XRSettings.eyeTextureResolutionScale, v => XRSettings.eyeTextureResolutionScale = v);
    private static readonly Held<int> Msaa = new("anti-aliasing", () => QualitySettings.antiAliasing, v => QualitySettings.antiAliasing = v);
    private static readonly Held<float> ShadowDistance = new("shadow distance", () => QualitySettings.shadowDistance, v => QualitySettings.shadowDistance = v);
    private static readonly Held<int> Cascades = new("shadow cascades", () => QualitySettings.shadowCascades, v => QualitySettings.shadowCascades = v);
    private static readonly Held<ShadowResolution> Resolution = new("shadow resolution", () => QualitySettings.shadowResolution, v => QualitySettings.shadowResolution = v);
    private static readonly Held<float> LodBias = new("LOD bias", () => QualitySettings.lodBias, v => QualitySettings.lodBias = v);
    private static readonly Held<int> Lights = new("pixel lights", () => QualitySettings.pixelLightCount, v => QualitySettings.pixelLightCount = v);
    private static readonly Held<GameViewRenderMode> Desktop = new("desktop view", () => XRSettings.gameViewRenderMode, v => XRSettings.gameViewRenderMode = v);
    private static readonly Held<AnisotropicFiltering> Aniso = new("anisotropic filtering", () => QualitySettings.anisotropicFiltering, v => QualitySettings.anisotropicFiltering = v);
    private static readonly Held<SkinWeights> Skin = new("skin weights", () => QualitySettings.skinWeights, v => QualitySettings.skinWeights = v);
    private static readonly Held<int> UploadMs = new("upload time", () => QualitySettings.asyncUploadTimeSlice, v => QualitySettings.asyncUploadTimeSlice = v);
    private static readonly Held<int> UploadMb = new("upload buffer", () => QualitySettings.asyncUploadBufferSize, v => QualitySettings.asyncUploadBufferSize = v);

    private static float nextCheck;

    public static void Apply()
    {
        var level = AccessTools.Inner(typeof(OptimizationManager), "QualitySetting");
        LensMod.Patch(level == null ? null : AccessTools.Method(level, "Apply"), "Quality", typeof(Quality), postfix: nameof(Hold));
        LensMod.Patch(AccessTools.Method(typeof(Township.PostProcessing.Windows.WindowsPostProcessController), "OnSettingsChanged"),
            "PixelLights", typeof(Quality), postfix: nameof(Hold));
        LensConfig.Changed += Hold;
        Hold();
        if (LensConfig.RenderScale > 0f) LensMod.Active.Add($"render scale {LensConfig.RenderScale:0.##}");
        if (LensConfig.Msaa >= 0) LensMod.Active.Add($"MSAA {LensConfig.Msaa}x");
        if (LensConfig.ShadowDistance > 0f) LensMod.Active.Add($"shadows to {LensConfig.ShadowDistance:0} m");
        if (LensConfig.ShadowCascades > 0) LensMod.Active.Add($"{LensConfig.ShadowCascades} shadow cascades");
        if (ParseResolution(out var r)) LensMod.Active.Add($"{r} shadow resolution");
        if (LensConfig.LodBias > 0f) LensMod.Active.Add($"LOD bias {LensConfig.LodBias:0.##}");
        if (LensConfig.PixelLights > 0) LensMod.Active.Add($"{LensConfig.PixelLights} pixel lights");
        if (DesktopMode(out var d)) LensMod.Active.Add($"desktop view {d}");
        if (AnisoMode(out var a)) LensMod.Active.Add($"anisotropic filtering {a}");
        if (SkinMode(out var w)) LensMod.Active.Add($"skin weights {w}");
        if (LensConfig.UploadMs > 0) LensMod.Active.Add($"uploads {LensConfig.UploadMs} ms a frame");
        if (LensConfig.UploadBufferMb > 0) LensMod.Active.Add($"upload buffer {LensConfig.UploadBufferMb} MB");
    }

    // Once a second: anything the game set back since.
    public static void Tick()
    {
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 1f;
        Hold();
    }

    private static void Hold()
    {
        Scale.Set(LensConfig.RenderScale > 0f, Mathf.Clamp(LensConfig.RenderScale, 0.3f, 2f));
        Msaa.Set(LensConfig.Msaa >= 0, LensConfig.Msaa >= 8 ? 8 : LensConfig.Msaa >= 4 ? 4 : LensConfig.Msaa >= 2 ? 2 : 0);
        ShadowDistance.Set(LensConfig.ShadowDistance > 0f, LensConfig.ShadowDistance);
        Cascades.Set(LensConfig.ShadowCascades > 0, LensConfig.ShadowCascades >= 4 ? 4 : LensConfig.ShadowCascades >= 2 ? 2 : 1);
        Resolution.Set(ParseResolution(out var resolution), resolution);
        LodBias.Set(LensConfig.LodBias > 0f, LensConfig.LodBias);
        Lights.Set(LensConfig.PixelLights > 0, LensConfig.PixelLights);
        Desktop.Set(DesktopMode(out var desktop), desktop);
        Aniso.Set(AnisoMode(out var aniso), aniso);
        Skin.Set(SkinMode(out var skin), skin);
        UploadMs.Set(LensConfig.UploadMs > 0, LensConfig.UploadMs);
        UploadMb.Set(LensConfig.UploadBufferMb > 0, LensConfig.UploadBufferMb);
    }

    private static bool DesktopMode(out GameViewRenderMode mode)
    {
        mode = LensConfig.Off(LensConfig.DesktopView) ? GameViewRenderMode.None : GameViewRenderMode.LeftEye;
        return LensConfig.Off(LensConfig.DesktopView) || LensConfig.DesktopView.Equals("OneEye", StringComparison.OrdinalIgnoreCase);
    }

    private static bool AnisoMode(out AnisotropicFiltering mode)
    {
        var s = LensConfig.Anisotropic;
        mode = LensConfig.Off(s) ? AnisotropicFiltering.Disable : s.Equals("Forced", StringComparison.OrdinalIgnoreCase) ? AnisotropicFiltering.ForceEnable : AnisotropicFiltering.Enable;
        return LensConfig.Off(s) || s.Equals("On", StringComparison.OrdinalIgnoreCase) || s.Equals("Forced", StringComparison.OrdinalIgnoreCase);
    }

    private static bool SkinMode(out SkinWeights weights)
    {
        int n = LensConfig.SkinWeights;
        weights = n >= 4 ? SkinWeights.FourBones : n >= 2 ? SkinWeights.TwoBones : SkinWeights.OneBone;
        return n > 0;
    }

    private static bool ParseResolution(out ShadowResolution resolution) =>
        Enum.TryParse(LensConfig.ShadowResolution, true, out resolution) && Enum.IsDefined(typeof(ShadowResolution), resolution);
}
