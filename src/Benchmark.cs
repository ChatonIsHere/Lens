using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using MelonLoader;
using UnityEngine;
using UnityEngine.XR;

namespace Lens;

// Benchmark = true: standing still in game, Lens tries each setting on its own for 40 s (10 s to settle, 30 s
// measured) between runs of the game's own settings, twice over, and logs what each saved: the GPU's time a frame (from
// SteamVR's compositor, else Unity's XR stats) and the frame interval. A table goes to the log and to
// UserData/Lens/benchmark-<time>.csv. Every setting is put back as it was, and Benchmark turns itself off.
internal static class Benchmark
{
    private const float Settle = 10f, Measure = 30f;

    private sealed class Step
    {
        public string Name;
        public Action Set;
        public readonly List<float> Gpu = new(), Interval = new();
        public int Dropped;
    }

    private static List<Step> steps;
    private static int at = -1;
    private static float stepStart, startAfter = -1f;
    private static Dictionary<MelonPreferences_Entry, object> before;
    private static uint lastFrameIndex;

    public static bool Running => steps != null;

    public static void Tick()
    {
        if (!LensConfig._benchmark.Value) return;
        if (steps == null)
        {
            if (PlayerController.Current == null) return;
            if (startAfter < 0f) startAfter = Time.unscaledTime + 60f; // let the scene settle after joining
            if (Time.unscaledTime < startAfter) return;
            Begin();
            return;
        }
        var step = steps[at];
        float t = Time.unscaledTime - stepStart;
        if (t >= Settle) Sample(step);
        if (t < Settle + Measure) return;
        LensMod.Log.Msg($"Benchmark step {at + 1}/{steps.Count}, {step.Name}: GPU {(step.Gpu.Count > 0 ? Median(step.Gpu) : 0f):0.00} ms " +
                        $"({step.Gpu.Count} frames), frame interval {Median(step.Interval):0.00} ms, dropped {step.Dropped}");
        if (++at < steps.Count) Enter();
        else Finish();
    }

    private static void Begin()
    {
        before = LensConfig.Entries.ToDictionary(e => e, e => e.BoxedValue);
        var tried = new (string name, Action set)[]
        {
            ("render scale 0.8", () => LensConfig._renderScale.Value = 0.8f),
            ("MSAA off", () => LensConfig._msaa.Value = 0),
            ("fire shadows off", () => LensConfig._fireShadows.Value = "Off"),
            ("fire shadows hard", () => LensConfig._fireShadows.Value = "Hard"),
            ("fire shadows within 20 m", () => LensConfig._fireShadowDistance.Value = 20f),
            ("fire shadow resolution low", () => LensConfig._fireShadowResolution.Value = "Low"),
            ("shadow distance 40 m", () => LensConfig._shadowDistance.Value = 40f),
            ("2 shadow cascades", () => LensConfig._shadowCascades.Value = 2),
            ("shadow resolution medium", () => LensConfig._shadowResolution.Value = "Medium"),
            ("sun shadows hard", () => LensConfig._sunShadows.Value = "Hard"),
            ("sun shadows off", () => LensConfig._sunShadows.Value = "Off"),
            ("3 pixel lights", () => LensConfig._pixelLights.Value = 3),
            ("LOD bias 0.7", () => LensConfig._lodBias.Value = 0.7f),
            ("tree distance 0.75", () => LensConfig._treeDistance.Value = 0.75f),
            ("view distance 600 m", () => LensConfig._viewDistance.Value = 600f),
            ("small things to 60 m", () => LensConfig._cullDistance.Value = 60f),
            ("bloom off", () => LensConfig._bloom.Value = false),
            ("grass off", () => LensConfig._grass.Value = false),
            ("no shadows from things under 0.5 m", () => LensConfig._smallShadowSize.Value = 0.5f),
            ("no shader warm-up", () => LensConfig._warmShaders.Value = false),
            ("Overview Camera off", () => LensConfig._overviewCamera.Value = false),
            ("desktop view one eye", () => LensConfig._desktopView.Value = "OneEye"),
            ("desktop view off", () => LensConfig._desktopView.Value = "Off"),
            ("anisotropic forced", () => LensConfig._anisotropic.Value = "Forced"),
            ("2 skin weights", () => LensConfig._skinWeights.Value = 2),
            ("no skinning out of view", () => LensConfig._offscreenSkinning.Value = false),
            ("no water reflections", () => LensConfig._waterReflections.Value = false),
        };
        steps = new List<Step>();
        for (int pass = 0; pass < 2; pass++)
            for (int i = 0; i < tried.Length; i++)
            {
                if (i % 6 == 0) steps.Add(new Step { Name = "game's own", Set = null });
                steps.Add(new Step { Name = tried[i].name, Set = tried[i].set });
            }
        steps.Add(new Step { Name = "game's own", Set = null });
        LensMod.Log.Msg($"Benchmark: {steps.Count} steps of {Settle + Measure:0} s, about {steps.Count * (Settle + Measure) / 60f:0} minutes. Keep still.");
        at = 0;
        Enter();
    }

    private static void Enter()
    {
        // Back to how it was, then this step's one change.
        foreach (var kv in before)
            if (kv.Key != LensConfig._benchmark && !Equals(kv.Key.BoxedValue, kv.Value)) kv.Key.BoxedValue = kv.Value;
        steps[at].Set?.Invoke();
        stepStart = Time.unscaledTime;
    }

    private static void Sample(Step step)
    {
        step.Interval.Add(Time.unscaledDeltaTime * 1000f);
        if (TryCompositor(out float gpu, out int dropped))
        {
            if (gpu > 0f) step.Gpu.Add(gpu);
            step.Dropped += dropped;
        }
        else if (TryUnityGpu(out gpu) && gpu > 0f) step.Gpu.Add(gpu * 1000f);
    }

    // SteamVR's own numbers for the last frame, the ones its frame timing graph shows.
    private static bool TryCompositor(out float gpuMs, out int dropped)
    {
        gpuMs = 0f;
        dropped = 0;
        try
        {
            return Compositor(out gpuMs, out dropped);
        }
        catch (Exception)
        {
            return false; // not on SteamVR
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static bool Compositor(out float gpuMs, out int dropped)
    {
        gpuMs = 0f;
        dropped = 0;
        var compositor = Valve.VR.OpenVR.Compositor;
        if (compositor == null) return false;
        var timing = new Valve.VR.Compositor_FrameTiming { m_nSize = (uint)Marshal.SizeOf(typeof(Valve.VR.Compositor_FrameTiming)) };
        if (!compositor.GetFrameTiming(ref timing, 0)) return false;
        if (timing.m_nFrameIndex == lastFrameIndex) return true; // the same frame as last time
        lastFrameIndex = timing.m_nFrameIndex;
        gpuMs = timing.m_flTotalRenderGpuMs;
        dropped = (int)timing.m_nNumDroppedFrames;
        return true;
    }

#pragma warning disable CS0618 // Unity 2020's own call for it
    private static bool TryUnityGpu(out float seconds) => XRStats.TryGetGPUTimeLastFrame(out seconds);
#pragma warning restore CS0618

    private static void Finish()
    {
        Restore();
        var own = steps.Where(s => s.Set == null && s.Gpu.Count > 0).ToList();
        float baseGpu = own.Count > 0 ? Median(own.SelectMany(s => s.Gpu)) : 0f;
        float baseInterval = Median(steps.Where(s => s.Set == null).SelectMany(s => s.Interval));
        var rows = steps.Where(s => s.Set != null).GroupBy(s => s.Name).Select(g => new
        {
            Name = g.Key,
            Gpu = g.Any(s => s.Gpu.Count > 0) ? Median(g.SelectMany(s => s.Gpu)) : 0f,
            Interval = Median(g.SelectMany(s => s.Interval)),
            Dropped = g.Sum(s => s.Dropped),
        }).OrderBy(r => r.Gpu - baseGpu).ToList();
        var lines = new List<string>
        {
            $"Benchmark: the game's own settings, GPU {baseGpu:0.00} ms a frame (median of {own.Sum(s => s.Gpu.Count)} frames; " +
            $"the spread between its runs: {string.Join(", ", own.Select(s => Median(s.Gpu).ToString("0.00")))}), frame interval {baseInterval:0.00} ms",
        };
        foreach (var r in rows)
            lines.Add($"  {r.Name}: GPU {r.Gpu:0.00} ms ({(baseGpu > 0f ? (r.Gpu - baseGpu) / baseGpu * 100f : 0f):+0;-0}%), " +
                      $"frame interval {r.Interval:0.00} ms, dropped frames {r.Dropped}");
        if (baseGpu <= 0f) lines.Add("  No GPU times came through (no VR, or the headset was asleep): only the frame interval is meaningful.");
        LensMod.Log.Msg(string.Join("\n", lines));
        try
        {
            var dir = Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "Lens");
            Directory.CreateDirectory(dir);
            var csv = new List<string> { "step,setting,gpu_ms_median,gpu_frames,interval_ms_median,dropped" };
            csv.AddRange(steps.Select((s, i) => $"{i},\"{s.Name}\",{(s.Gpu.Count > 0 ? Median(s.Gpu) : 0f):0.000},{s.Gpu.Count},{Median(s.Interval):0.000},{s.Dropped}"));
            File.WriteAllLines(Path.Combine(dir, $"benchmark-{DateTime.UtcNow:yyyyMMddTHHmmss}.csv"), csv);
        }
        catch (Exception e)
        {
            LensMod.Log.Warning($"Benchmark: couldn't write the table: {e.Message}");
        }
        steps = null;
        LensConfig._benchmark.Value = false;
        LensConfig.Save();
    }

    // Everything as it was before the benchmark, also if the game closes during it.
    public static void Restore()
    {
        if (before == null) return;
        foreach (var kv in before)
            if (kv.Key != LensConfig._benchmark && !Equals(kv.Key.BoxedValue, kv.Value)) kv.Key.BoxedValue = kv.Value;
        before = null;
    }

    private static float Median(IEnumerable<float> values)
    {
        var v = values.OrderBy(x => x).ToList();
        return v.Count == 0 ? 0f : v[v.Count / 2];
    }
}
