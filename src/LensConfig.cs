using System;
using MelonLoader;

namespace Lens;

// [Lens] in UserData/MelonPreferences.cfg. Every setting starts at the game's own; changes made while the game runs
// (in a mods menu, or the file reloaded) apply straight away.
internal static class LensConfig
{
    private static MelonPreferences_Entry<float> _renderScale;
    private static MelonPreferences_Entry<int> _msaa;
    private static MelonPreferences_Entry<float> _shadowDistance;
    private static MelonPreferences_Entry<int> _shadowCascades;
    private static MelonPreferences_Entry<string> _shadowResolution;
    private static MelonPreferences_Entry<string> _sunShadows;
    private static MelonPreferences_Entry<string> _fireShadows;
    private static MelonPreferences_Entry<int> _pixelLights;
    private static MelonPreferences_Entry<float> _lodBias;
    private static MelonPreferences_Entry<float> _treeDistance;
    private static MelonPreferences_Entry<float> _viewDistance;
    private static MelonPreferences_Entry<float> _cullDistance;
    private static MelonPreferences_Entry<bool> _otherPlayersCameras;
    private static MelonPreferences_Entry<bool> _overviewCamera;
    private static MelonPreferences_Entry<bool> _bloom;
    private static MelonPreferences_Entry<bool> _grass;
    private static MelonPreferences_Entry<bool> _renderReport;

    public static float RenderScale => Math.Max(0f, _renderScale.Value);
    public static int Msaa => _msaa.Value;
    public static float ShadowDistance => Math.Max(0f, _shadowDistance.Value);
    public static int ShadowCascades => _shadowCascades.Value;
    public static string ShadowResolution => _shadowResolution.Value?.Trim() ?? "";
    public static string SunShadows => _sunShadows.Value?.Trim() ?? "";
    public static string FireShadows => _fireShadows.Value?.Trim() ?? "";
    public static int PixelLights => Math.Max(0, Math.Min(8, _pixelLights.Value));
    public static float LodBias => Math.Max(0f, _lodBias.Value);
    public static float TreeDistance => _treeDistance.Value > 0f ? _treeDistance.Value : 1f;
    public static float ViewDistance => Math.Max(0f, _viewDistance.Value);
    public static float CullDistance => Math.Max(0f, _cullDistance.Value);
    public static bool OtherPlayersCameras => _otherPlayersCameras.Value;
    public static bool OverviewCamera => _overviewCamera.Value;
    public static bool Bloom => _bloom.Value;
    public static bool Grass => _grass.Value;
    public static bool RenderReport => _renderReport.Value;

    // Any setting changed while the game runs.
    public static event Action Changed;

    public static void Init()
    {
        var c = MelonPreferences.CreateCategory("Lens", "Lens");
        _renderScale = c.CreateEntry("RenderScale", 0f, description: "Resolution each eye renders at, as a share of the headset's (0 keeps the game's, 1.0). 0.8 draws about a third fewer pixels; SteamVR's own resolution setting multiplies with it.");
        _msaa = c.CreateEntry("Msaa", -1, description: "Anti-aliasing samples: 0, 2, 4 or 8 (-1 keeps the game's). Fewer is faster with more jagged edges.");
        _shadowDistance = c.CreateEntry("ShadowDistance", 0f, description: "How far away shadows are drawn, in metres (0 keeps the game's, which it lowers and raises with the frame rate).");
        _shadowCascades = c.CreateEntry("ShadowCascades", 0, description: "Sun shadow cascades: 1, 2 or 4 (0 keeps the game's). Fewer is faster, with blurrier shadows near you.");
        _shadowResolution = c.CreateEntry("ShadowResolution", "", description: "Shadow map resolution: Low, Medium, High or VeryHigh (empty keeps the game's).");
        _sunShadows = c.CreateEntry("SunShadows", "", description: "The sun's and moon's shadows: Hard (no soft edges, faster) or Off (empty keeps the game's soft shadows).");
        _fireShadows = c.CreateEntry("FireShadows", "", description: "Shadows from fires and torches: Hard or Off (empty keeps the game's). A shadowed fire renders the scene six more times, so towns full of torches gain the most.");
        _pixelLights = c.CreateEntry("PixelLights", 0, description: "Lights drawn fully at once, 1 to 8 (0 keeps the game's 5). The rest are drawn the cheap way, by vertex.");
        _lodBias = c.CreateEntry("LodBias", 0f, description: "How far away grass and props keep their detailed models, as a multiple (0 keeps the game's). Lower switches to simpler models sooner.");
        _treeDistance = c.CreateEntry("TreeDistance", 1f, description: "How far away trees keep their detailed models, as a multiple (1 keeps the game's). 0.75 switches each tree to its simpler model a quarter sooner.");
        _viewDistance = c.CreateEntry("ViewDistance", 0f, description: "How far you can see, in metres (0 keeps the game's). Nothing past it is drawn.");
        _cullDistance = c.CreateEntry("CullDistance", 0f, description: "How far away small things on the default layer are drawn, in metres (0 keeps the game's 100).");
        _otherPlayersCameras = c.CreateEntry("OtherPlayersCameras", true, description: "Show the viewfinder of a camera another player holds. Each one shown renders the whole scene again.");
        _overviewCamera = c.CreateEntry("OverviewCamera", true, description: "Render the scene's \"Overview Camera\" (false skips it). FastTale measured skipping it as a large CPU saving; the render report shows what it draws to.");
        _bloom = c.CreateEntry("Bloom", true, description: "The bloom glow around bright things (false turns it off).");
        _grass = c.CreateEntry("Grass", true, description: "The small grass tufts (false hides them).");
        _renderReport = c.CreateEntry("RenderReport", true, description: "Log what the game is rendering (cameras, lights and shadows, post-processing, quality settings) once each scene has settled, to see where the time goes.");
        foreach (var entry in c.Entries)
            entry.OnEntryValueChangedUntyped.Subscribe((_, _) => Changed?.Invoke());
        c.SaveToFile(false);
    }

    public static bool Off(string mode) => mode.Equals("Off", StringComparison.OrdinalIgnoreCase);
    public static bool Hard(string mode) => mode.Equals("Hard", StringComparison.OrdinalIgnoreCase);
}
