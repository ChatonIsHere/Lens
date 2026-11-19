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
// measured), with a run of the game's own settings before and after every three, and logs what each saved against
// the runs either side: the GPU's and the game's CPU time a frame (from SteamVR's compositor), and the share of frames
// at the headset's full rate. It starts once the world has finished loading in (GPU time steady for 2 minutes; up to
// 12), since a world still streaming in makes every number move. A table goes to the log and to
// UserData/Lens/benchmark-<time>.csv. Every setting is put back as it was, and Benchmark turns itself off.
internal static class Benchmark
{
    private const float Settle = 10f, Measure = 30f, Window = 30f, LongestWait = 12 * 60f;

    private sealed class Step
    {
        public string Name;
        public Action Set;
        public readonly List<float> Gpu = new(), Cpu = new(), Interval = new();
        public int Full, Frames;
    }

    private static List<Step> steps;
    private static int at = -1;
    private static float stepStart, joined = -1f, windowStart, nextWaitLog;
    private static readonly List<float> windowGpu = new();
    private static readonly List<float> windows = new();
    private static Dictionary<MelonPreferences_Entry, object> before;
    private static uint lastFrameIndex;

    public static void Tick()
    {
        if (!LensConfig._benchmark.Value) return;
        if (steps == null)
        {
            WaitForSteadyWorld();
            return;
        }
        if (PlayerController.Current == null)
        {
            // Out of the world (a menu measures nothing useful): stop, and start over once back in.
            LensMod.Log.Warning($"Benchmark: left the world at step {at + 1}/{steps.Count}; stopped, and starting over once back in game.");
            Restore();
            Reset();
            return;
        }
        var step = steps[at];
        float t = Time.unscaledTime - stepStart;
        if (t >= Settle) Sample(step);
        if (t < Settle + Measure) return;
        LensMod.Log.Msg($"Benchmark step {at + 1}/{steps.Count}, {step.Name}: GPU {Median(step.Gpu):0.00} ms, CPU {Median(step.Cpu):0.00} ms " +
                        $"({step.Gpu.Count} frames), {Share(step):0}% at full rate");
        if (++at < steps.Count) Enter();
        else Finish();
    }

    // Starts once the GPU's time a frame has held within 10% for four 30 s windows, at least 3 minutes after joining.
    private static void WaitForSteadyWorld()
    {
        if (PlayerController.Current == null)
        {
            if (joined >= 0f) Reset(); // the wait starts again in the next world
            return;
        }
        float now = Time.unscaledTime;
        if (joined < 0f)
        {
            joined = windowStart = now;
            LensMod.Log.Msg("Benchmark: waiting for the world to finish loading in. Keep still.");
        }
        if (Frame(out float gpu, out _, out _) && gpu > 0f) windowGpu.Add(gpu);
        if (now - windowStart < Window) return;
        windows.Add(Median(windowGpu));
        windowGpu.Clear();
        windowStart = now;
        var last = windows.Skip(Math.Max(0, windows.Count - 4)).ToList();
        bool steady = last.Count == 4 && last.Max() <= last.Average() * 1.1f && last.Min() >= last.Average() * 0.9f && now - joined >= 180f;
        if (now >= nextWaitLog)
        {
            nextWaitLog = now + 60f;
            LensMod.Log.Msg($"Benchmark: still loading in, GPU {windows[windows.Count - 1]:0.00} ms a frame.");
        }
        if (steady || now - joined >= LongestWait) Begin(steady);
    }

    private static void Begin(bool steady)
    {
        before = LensConfig.Entries.ToDictionary(e => e, e => e.BoxedValue);
        var tried = new (string name, Action set)[]
        {
            ("render scale 0.8", () => LensConfig._renderScale.Value = 0.8f),
            ("MSAA off", () => LensConfig._msaa.Value = 0),
            ("bloom off", () => LensConfig._bloom.Value = false),
            ("fire shadows off", () => LensConfig._fireShadows.Value = "Off"),
            ("fire shadows hard", () => LensConfig._fireShadows.Value = "Hard"),
            ("fire shadows within 20 m", () => LensConfig._fireShadowDistance.Value = 20f),
            ("fire shadow resolution low", () => LensConfig._fireShadowResolution.Value = "Low"),
            ("sun shadows hard", () => LensConfig._sunShadows.Value = "Hard"),
            ("sun shadows off", () => LensConfig._sunShadows.Value = "Off"),
            ("shadow distance 40 m", () => LensConfig._shadowDistance.Value = 40f),
            ("2 shadow cascades", () => LensConfig._shadowCascades.Value = 2),
            ("shadow resolution medium", () => LensConfig._shadowResolution.Value = "Medium"),
            ("no shadows from things under 0.5 m", () => LensConfig._smallShadowSize.Value = 0.5f),
            ("3 pixel lights", () => LensConfig._pixelLights.Value = 3),
            ("LOD bias 0.7", () => LensConfig._lodBias.Value = 0.7f),
            ("tree distance 0.75", () => LensConfig._treeDistance.Value = 0.75f),
            ("view distance 600 m", () => LensConfig._viewDistance.Value = 600f),
            ("small things to 60 m", () => LensConfig._cullDistance.Value = 60f),
            ("grass off", () => LensConfig._grass.Value = false),
            ("Overview Camera off", () => LensConfig._overviewCamera.Value = false),
            ("desktop view one eye", () => LensConfig._desktopView.Value = "OneEye"),
            ("desktop view off", () => LensConfig._desktopView.Value = "Off"),
            ("anisotropic forced", () => LensConfig._anisotropic.Value = "Forced"),
            ("2 skin weights", () => LensConfig._skinWeights.Value = 2),
            ("no skinning out of view", () => LensConfig._offscreenSkinning.Value = false),
            ("no water reflections", () => LensConfig._waterReflections.Value = false),
        };
        steps = new List<Step>();
        for (int i = 0; i < tried.Length; i++)
        {
            if (i % 3 == 0) steps.Add(new Step { Name = "game's own" });
            steps.Add(new Step { Name = tried[i].name, Set = tried[i].set });
        }
        steps.Add(new Step { Name = "game's own" });
        LensMod.Log.Msg($"Benchmark: {(steady ? "the world has loaded in" : "the world never held steady; starting anyway")}. " +
                        $"{steps.Count} steps of {Settle + Measure:0} s, about {steps.Count * (Settle + Measure) / 60f:0} minutes.");
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
        if (!Frame(out float gpu, out float cpu, out float interval)) return;
        step.Frames++;
        if (gpu > 0f) step.Gpu.Add(gpu);
        if (cpu > 0f) step.Cpu.Add(cpu);
        if (interval > 0f) step.Interval.Add(interval);
        float period = XRDevice.refreshRate > 0f ? 1000f / XRDevice.refreshRate : 0f;
        if (period > 0f && interval > 0f && interval < period * 1.25f) step.Full++;
    }

    private static float Share(Step s) => s.Interval.Count == 0 ? 0f : 100f * s.Full / s.Interval.Count;

    // SteamVR's numbers for the last frame, as its frame timing graph shows them; false when there's no new frame.
    private static bool Frame(out float gpuMs, out float cpuMs, out float intervalMs)
    {
        gpuMs = cpuMs = intervalMs = 0f;
        try
        {
            return Compositor(out gpuMs, out cpuMs, out intervalMs);
        }
        catch (Exception)
        {
            return false; // not on SteamVR
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static bool Compositor(out float gpuMs, out float cpuMs, out float intervalMs)
    {
        gpuMs = cpuMs = intervalMs = 0f;
        var compositor = Valve.VR.OpenVR.Compositor;
        if (compositor == null) return false;
        var timing = new Valve.VR.Compositor_FrameTiming { m_nSize = (uint)Marshal.SizeOf(typeof(Valve.VR.Compositor_FrameTiming)) };
        if (!compositor.GetFrameTiming(ref timing, 0) || timing.m_nFrameIndex == lastFrameIndex) return false;
        lastFrameIndex = timing.m_nFrameIndex;
        gpuMs = timing.m_flTotalRenderGpuMs;
        // The game's CPU time: from the poses it waits for to its frame handed over.
        cpuMs = timing.m_flNewFrameReadyMs - timing.m_flNewPosesReadyMs;
        intervalMs = timing.m_flClientFrameIntervalMs;
        return true;
    }

    private static void Finish()
    {
        Restore();
        var lines = new List<string>();
        var csv = new List<string> { "step,setting,gpu_ms,cpu_ms,full_rate_pct,frames,gpu_vs_own_pct,cpu_vs_own_pct,own_steady" };
        var rows = new List<(string line, float saving)>();
        for (int i = 0; i < steps.Count; i++)
        {
            var s = steps[i];
            // Against the runs of the game's own settings just before and after it.
            var prev = steps.Take(i).LastOrDefault(x => x.Set == null);
            var next = steps.Skip(i + 1).FirstOrDefault(x => x.Set == null);
            float ownGpu = Mean(prev, next, x => Median(x.Gpu)), ownCpu = Mean(prev, next, x => Median(x.Cpu));
            bool steadyOwn = prev == null || next == null || Math.Abs(Median(prev.Gpu) - Median(next.Gpu)) <= 0.1f * ownGpu;
            float gpuChange = ownGpu > 0f ? (Median(s.Gpu) - ownGpu) / ownGpu * 100f : 0f;
            float cpuChange = ownCpu > 0f ? (Median(s.Cpu) - ownCpu) / ownCpu * 100f : 0f;
            csv.Add($"{i},\"{s.Name}\",{Median(s.Gpu):0.000},{Median(s.Cpu):0.000},{Share(s):0.0},{s.Frames},{(s.Set == null ? "" : gpuChange.ToString("0.0"))}," +
                    $"{(s.Set == null ? "" : cpuChange.ToString("0.0"))},{steadyOwn}");
            if (s.Set != null)
                rows.Add(($"  {s.Name}: GPU {Median(s.Gpu):0.00} ms ({gpuChange:+0;-0}%), CPU {Median(s.Cpu):0.00} ms ({cpuChange:+0;-0}%), " +
                          $"{Share(s):0}% at full rate{(steadyOwn ? "" : " (the game's own runs either side differed by more than 10%: unreliable)")}", gpuChange));
        }
        var own = steps.Where(s => s.Set == null).ToList();
        lines.Add($"Benchmark: the game's own settings, GPU {Median(own.SelectMany(s => s.Gpu)):0.00} ms, CPU {Median(own.SelectMany(s => s.Cpu)):0.00} ms a frame " +
                  $"(its runs: {string.Join(", ", own.Select(s => Median(s.Gpu).ToString("0.00")))} ms), {own.Average(Share):0}% at full rate; each setting against the runs either side:");
        lines.AddRange(rows.OrderBy(r => r.saving).Select(r => r.line));
        if (own.All(s => s.Gpu.Count == 0)) lines.Add("  No GPU times came through (no VR, or the headset was asleep).");
        LensMod.Log.Msg(string.Join("\n", lines));
        try
        {
            var dir = Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "Lens");
            Directory.CreateDirectory(dir);
            File.WriteAllLines(Path.Combine(dir, $"benchmark-{DateTime.UtcNow:yyyyMMddTHHmmss}.csv"), csv);
        }
        catch (Exception e)
        {
            LensMod.Log.Warning($"Benchmark: couldn't write the table: {e.Message}");
        }
        Reset();
        LensConfig._benchmark.Value = false;
        LensConfig.Save();
    }

    private static void Reset()
    {
        steps = null;
        joined = -1f;
        windows.Clear();
        windowGpu.Clear();
    }

    // Everything as it was before the benchmark, also if the game closes during it.
    public static void Restore()
    {
        if (before == null) return;
        foreach (var kv in before)
            if (kv.Key != LensConfig._benchmark && !Equals(kv.Key.BoxedValue, kv.Value)) kv.Key.BoxedValue = kv.Value;
        before = null;
    }

    private static float Mean(Step a, Step b, Func<Step, float> f)
    {
        var v = new[] { a, b }.Where(x => x != null).Select(f).Where(x => x > 0f).ToList();
        return v.Count == 0 ? 0f : v.Average();
    }

    private static float Median(IEnumerable<float> values)
    {
        var v = values.OrderBy(x => x).ToList();
        return v.Count == 0 ? 0f : v[v.Count / 2];
    }
}
