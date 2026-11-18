using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Lens.Tweaks;
using MelonLoader;

[assembly: MelonInfo(typeof(Lens.LensMod), "Lens", Lens.LensMod.Version, "ChatonIsHere", "https://github.com/ChatonIsHere/Lens")]
[assembly: MelonGame("Alta", "A Township Tale")]

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
        Grass.Apply();
        MelonEvents.OnApplicationLateStart.Subscribe(() =>
            Log.Msg("On: " + (Active.Count == 0 ? "nothing (every setting is the game's own)" : string.Join(", ", Active))));
    }

    public override void OnUpdate()
    {
        if (_harmony == null) return;
        Quality.Tick();
        Cameras.Tick();
        RenderReport.Tick();
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (_harmony == null) return;
        RenderReport.SceneLoaded(sceneName);
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
