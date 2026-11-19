using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Lens.Tweaks;
using MelonLoader;

[assembly: MelonInfo(typeof(Lens.LensMod), "Lens", Lens.LensMod.Version, "ChatonIsHere", "https://github.com/ChatonIsHere/Lens")]
[assembly: MelonGame("Alta", "A Township Tale")]
// Loaded first when it's there, for the tablet; Lens works without it.
[assembly: MelonOptionalDependencies("TabletUILib")]

namespace Lens;

public class LensMod : MelonMod
{
    public const string Version = Build.Version;

    internal static MelonLogger.Instance Log { get; private set; }

    // What's on, for the startup line.
    internal static readonly List<string> Active = new();

    private static HarmonyLib.Harmony _harmony;

    // Servers and bots without graphics draw nothing, so there's nothing for Lens to do there.
    private static bool Drawing =>
        !Environment.GetCommandLineArgs().Any(a => a.Equals("/start_server", StringComparison.OrdinalIgnoreCase)
                                                   || a.Equals("-batchmode", StringComparison.OrdinalIgnoreCase)
                                                   || a.Equals("-nographics", StringComparison.OrdinalIgnoreCase));

    public override void OnInitializeMelon()
    {
        Log = LoggerInstance;
        if (!Drawing)
        {
            Log.Msg("No graphics here (a server or a headless client): Lens is off.");
            return;
        }
        _harmony = HarmonyInstance;
        LensConfig.Init();
        Quality.Apply();
        Shadows.Apply();
        Distances.Apply();
        Cameras.Apply();
        Bloom.Apply();
        Props.Apply();
        World.Apply();
        if (LensConfig.WarmShaders) Active.Add("shader warm-up");
        MelonEvents.OnApplicationLateStart.Subscribe(() =>
        {
            Log.Msg("On: " + (Active.Count == 0 ? "nothing (every setting is the game's own)" : string.Join(", ", Active)));
            AddToTablet();
        });
    }

    // The tablet app, with TabletUILib. Its types are only touched inside Tablet, so without it nothing loads them.
    private static void AddToTablet()
    {
        if (!RegisteredMelons.Any(m => m.Info.Name == "TabletUILib")) return;
        try
        {
            RegisterTablet();
            Log.Msg("Settings are on the tablet (Lens, on the wheel).");
        }
        catch (Exception e)
        {
            Log.Warning($"Couldn't add Lens to the tablet ({e.GetType().Name}: {e.Message}); the settings are still in MelonPreferences.cfg.");
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void RegisterTablet() => Tablet.Register();

    public override void OnUpdate()
    {
        if (_harmony == null) return;
        Quality.Tick();
        Cameras.Tick();
        Shadows.Tick();
        World.Tick();
        RenderReport.Tick();
        Unconfirmed.Tick();
        Benchmark.Tick();
    }

    // Changes nobody kept don't outlive the game.
    public override void OnApplicationQuit()
    {
        if (_harmony == null) return;
        Benchmark.Restore();
        Unconfirmed.Revert(" (the game closed before they were kept)");
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (_harmony == null) return;
        RenderReport.SceneLoaded(sceneName);
        Shaders.Warm();
    }

    // Patches one method, or warns and carries on if the game doesn't have it.
    internal static bool Patch(MethodBase target, string name, Type patches, string prefix = null, string postfix = null)
    {
        if (target == null)
        {
            Log.Warning($"{name}: target not found, skipped.");
            return false;
        }
        try
        {
            _harmony.Patch(target, prefix == null ? null : new HarmonyMethod(patches, prefix),
                postfix == null ? null : new HarmonyMethod(patches, postfix));
            return true;
        }
        catch (Exception e)
        {
            Log.Warning($"{name}: couldn't patch {target.DeclaringType?.Name}.{target.Name}, skipped. {e.Message}");
            return false;
        }
    }
}
