using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.XR;

namespace Lens;

// What the game renders, logged once each scene has settled (20 s after it loads, with the player in it): every
// camera and what it draws to, the lights and which cast shadows, post-processing effects, reflection probes,
// terrain, water reflections and the quality settings. The things that cost the most and that the game's code
// doesn't show (they're set in its scenes) show here.
internal static class RenderReport
{
    private static float due = -1f;
    private static string scene;

    public static void SceneLoaded(string name)
    {
        scene = name;
        due = Time.unscaledTime + 20f;
    }

    public static void Tick()
    {
        if (due < 0f || Time.unscaledTime < due || !LensConfig.RenderReport) return;
        if (PlayerController.Current == null)
        {
            due = Time.unscaledTime + 5f; // not playing yet
            return;
        }
        due = -1f;
        LensMod.Log.Msg(Build());
    }

    public static void Now() => LensMod.Log.Msg(Build());

    private static string Build()
    {
        var s = new StringBuilder($"Render report, scene {scene}:\n");
        s.Append($"  Quality: MSAA {QualitySettings.antiAliasing}x, shadows {QualitySettings.shadows} to {QualitySettings.shadowDistance:0} m, " +
                 $"{QualitySettings.shadowCascades} cascades, {QualitySettings.shadowResolution} resolution, LOD bias {QualitySettings.lodBias:0.##}, " +
                 $"{QualitySettings.pixelLightCount} pixel lights, max LOD {QualitySettings.maximumLODLevel}, texture limit {QualitySettings.masterTextureLimit}, " +
                 $"anisotropic {QualitySettings.anisotropicFiltering}, skin weights {QualitySettings.skinWeights}, uploads {QualitySettings.asyncUploadTimeSlice} ms " +
                 $"into {QualitySettings.asyncUploadBufferSize} MB, soft particles {QualitySettings.softParticles}, realtime probes {QualitySettings.realtimeReflectionProbes}\n");
        s.Append($"  XR: {XRSettings.loadedDeviceName}, eye texture {XRSettings.eyeTextureWidth}x{XRSettings.eyeTextureHeight} at scale " +
                 $"{XRSettings.eyeTextureResolutionScale:0.##}, game view {XRSettings.gameViewRenderMode}\n");
        foreach (var c in Camera.allCameras.OrderBy(c => c.depth))
        {
            var target = c.targetTexture == null ? $"display {c.targetDisplay}" : $"texture {c.targetTexture.name} {c.targetTexture.width}x{c.targetTexture.height}";
            s.Append($"  Camera {c.name}: depth {c.depth}, {(c.stereoEnabled ? "stereo" : "mono")}, to {target}, {c.actualRenderingPath}, " +
                     $"far {c.farClipPlane:0} m, post-processing {(c.GetComponent<PostProcessLayer>() != null ? "yes" : "no")}, " +
                     $"occlusion culling {c.useOcclusionCulling}, tag {c.tag}{(c == Camera.main ? " (Camera.main)" : "")}\n");
        }
        var lights = Object.FindObjectsOfType<Light>().Where(l => l.isActiveAndEnabled).ToList();
        s.Append($"  Lights on: {lights.Count} (" + string.Join(", ", lights.GroupBy(l => l.type).Select(g => $"{g.Count()} {g.Key}")) + "); with shadows: " +
                 string.Join(", ", lights.Where(l => l.shadows != LightShadows.None).GroupBy(l => $"{l.type} {l.shadows}").Select(g => $"{g.Count()} {g.Key}")) + "\n");
        var head = PlayerController.Current != null && PlayerController.Current.Head != null ? PlayerController.Current.Head.position : Vector3.zero;
        var shadowed = lights.Where(l => l.shadows != LightShadows.None && l.type != LightType.Directional)
            .OrderBy(l => (l.transform.position - head).sqrMagnitude).ToList();
        if (shadowed.Count > 0)
            s.Append($"  Shadowed point and spot lights: {shadowed.Count(l => (l.transform.position - head).magnitude <= 20f)} within 20 m of you, " +
                     $"{shadowed.Count(l => (l.transform.position - head).magnitude > 20f)} farther\n");
        foreach (var l in shadowed.Take(8))
            s.Append($"    {l.name}: {(l.transform.position - head).magnitude:0} m away, range {l.range:0} m, {l.renderMode}, resolution {l.shadowResolution}\n");
        foreach (var v in Object.FindObjectsOfType<PostProcessVolume>().Where(v => v.isActiveAndEnabled))
        {
            var profile = v.HasInstantiatedProfile() ? v.profile : v.sharedProfile;
            var on = profile == null ? "" : string.Join(", ", profile.settings.Where(e => e.active && e.enabled.value).Select(e => e.GetType().Name));
            s.Append($"  Post-processing {v.name}: {(v.isGlobal ? "global" : "local")}, weight {v.weight:0.##}, on: {(on.Length == 0 ? "nothing" : on)}\n");
        }
        var probes = Object.FindObjectsOfType<ReflectionProbe>().Where(p => p.isActiveAndEnabled).ToList();
        if (probes.Count > 0)
            s.Append("  Reflection probes: " + string.Join(", ", probes.GroupBy(p => $"{p.mode} {(p.mode == ReflectionProbeMode.Realtime ? p.refreshMode.ToString() : "")}".Trim())
                .Select(g => $"{g.Count()} {g.Key}")) + "\n");
        foreach (var t in Terrain.activeTerrains.Take(3))
            s.Append($"  Terrain {t.name}: pixel error {t.heightmapPixelError:0.#}, basemap {t.basemapDistance:0} m, detail {t.detailObjectDistance:0} m " +
                     $"at density {t.detailObjectDensity:0.##}, trees {t.treeDistance:0} m, shadows {t.shadowCastingMode}, instanced {t.drawInstanced}, " +
                     $"shader {(t.materialTemplate != null ? t.materialTemplate.shader.name : "built-in")}\n");
        if (Terrain.activeTerrains.Length > 3) s.Append($"  ({Terrain.activeTerrains.Length} terrains)\n");
        int water = Object.FindObjectsOfType<StylizedWaterShader.StylizedWater>().Count(w => w.isActiveAndEnabled && w.useReflection);
        s.Append($"  Water with planar reflections: {water}\n");
        var skinned = Object.FindObjectsOfType<SkinnedMeshRenderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy).ToList();
        s.Append($"  Skinned meshes: {skinned.Count}, {skinned.Count(r => r.updateWhenOffscreen)} animated out of view, {skinned.Count(r => !r.isVisible)} out of view now\n");
        var meshes = Object.FindObjectsOfType<MeshRenderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy && r.shadowCastingMode != ShadowCastingMode.Off).ToList();
        s.Append($"  Meshes casting shadows: {meshes.Count}, under 0.5 m across {meshes.Count(r => r.bounds.size.magnitude < 0.5f)}, under 1 m {meshes.Count(r => r.bounds.size.magnitude < 1f)}\n");
        var lists = Resources.FindObjectsOfTypeAll<ShaderVariantCollection>();
        s.Append($"  Shader lists loaded: {lists.Length}, warmed {lists.Count(c => c.isWarmedUp)}, {lists.Sum(c => c.variantCount)} variants\n");
        s.Append($"  Fire effects burning: {Object.FindObjectsOfType<PooledFireEffect>().Count(f => f.isActiveAndEnabled)}\n");
        s.Append($"  Lens settings at start: {(LensMod.Active.Count == 0 ? "none" : string.Join(", ", LensMod.Active))}");
        return s.ToString();
    }
}
