using System;
using MelonLoader;

namespace Lens;

// [Lens] in UserData/MelonPreferences.cfg. Every setting starts at the game's own; changes made while the game runs
// (in a mods menu, or the file reloaded) apply straight away.
internal static class LensConfig
{
    internal static MelonPreferences_Entry<float> _renderScale;
    internal static MelonPreferences_Entry<int> _msaa;
    internal static MelonPreferences_Entry<float> _shadowDistance;
    internal static MelonPreferences_Entry<int> _shadowCascades;
    internal static MelonPreferences_Entry<string> _shadowResolution;
    internal static MelonPreferences_Entry<string> _sunShadows;
    internal static MelonPreferences_Entry<string> _fireShadows;
    internal static MelonPreferences_Entry<int> _pixelLights;
    internal static MelonPreferences_Entry<float> _lodBias;
    internal static MelonPreferences_Entry<float> _treeDistance;
    internal static MelonPreferences_Entry<float> _viewDistance;
    internal static MelonPreferences_Entry<float> _cullDistance;
    internal static MelonPreferences_Entry<bool> _otherPlayersCameras;
    internal static MelonPreferences_Entry<bool> _overviewCamera;
    internal static MelonPreferences_Entry<bool> _bloom;
    internal static MelonPreferences_Entry<bool> _grass;
    internal static MelonPreferences_Entry<bool> _renderReport;
    internal static MelonPreferences_Entry<string> _desktopView;
    internal static MelonPreferences_Entry<string> _anisotropic;
    internal static MelonPreferences_Entry<float> _fireShadowDistance;
    internal static MelonPreferences_Entry<string> _fireShadowResolution;
    internal static MelonPreferences_Entry<float> _smallShadowSize;
    internal static MelonPreferences_Entry<int> _skinWeights;
    internal static MelonPreferences_Entry<bool> _waterReflections;
    internal static MelonPreferences_Entry<bool> _offscreenSkinning;
    internal static MelonPreferences_Entry<int> _uploadMs;
    internal static MelonPreferences_Entry<int> _uploadBufferMb;
    internal static MelonPreferences_Entry<bool> _warmShaders;
    internal static MelonPreferences_Entry<bool> _benchmark;

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
    public static string DesktopView => _desktopView.Value?.Trim() ?? "";
    public static string Anisotropic => _anisotropic.Value?.Trim() ?? "";
    public static float FireShadowDistance => Math.Max(0f, _fireShadowDistance.Value);
    public static string FireShadowResolution => _fireShadowResolution.Value?.Trim() ?? "";
    public static float SmallShadowSize => Math.Max(0f, _smallShadowSize.Value);
    public static int SkinWeights => _skinWeights.Value;
    public static bool WaterReflections => _waterReflections.Value;
    public static bool OffscreenSkinning => _offscreenSkinning.Value;
    public static int UploadMs => Math.Max(0, Math.Min(33, _uploadMs.Value));
    public static int UploadBufferMb => Math.Max(0, Math.Min(256, _uploadBufferMb.Value));
    public static bool WarmShaders => _warmShaders.Value;

    // Any setting changed while the game runs.
    public static event Action Changed;

    private static MelonPreferences_Category category;
    internal static System.Collections.Generic.List<MelonPreferences_Entry> Entries => category.Entries;

    public static void Save() => category.SaveToFile(false);

    public static void Init()
    {
        var c = category = MelonPreferences.CreateCategory("Lens", "Lens");
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
        _desktopView = c.CreateEntry("DesktopView", "", description: "What the game's desktop window shows: OneEye or Off (empty keeps the game's). The headset is unaffected; mirroring both eyes to the desktop costs a little each frame.");
        _anisotropic = c.CreateEntry("Anisotropic", "", description: "Anisotropic filtering, which keeps ground and walls seen at a shallow angle sharp: Off, On (where a texture asks for it) or Forced (everywhere). Empty keeps the game's. Almost free on current graphics cards.");
        _fireShadowDistance = c.CreateEntry("FireShadowDistance", 0f, description: "Fires and torches farther away than this, in metres, cast no shadows (0 keeps them at any distance). Their shadows are small by then, and each costs six extra renders.");
        _fireShadowResolution = c.CreateEntry("FireShadowResolution", "", description: "Shadow resolution for fires and torches: Low, Medium, High or VeryHigh (empty keeps the game's). Lower gives softer torch shadows for less GPU work.");
        _smallShadowSize = c.CreateEntry("SmallShadowSize", 0f, description: "Props in the world smaller than this, in metres across, cast no shadows (0 keeps every shadow). 0.5 drops the shadows of grass tufts, pebbles and the like.");
        _skinWeights = c.CreateEntry("SkinWeights", 0, description: "Bones that move each point of a character's skin: 1, 2 or 4 (0 keeps the game's). The Quest build uses 2: slightly simpler bends, cheaper characters.");
        _waterReflections = c.CreateEntry("WaterReflections", true, description: "Water that mirrors the scene (false turns the mirror off). Each such water renders the scene again; the render report shows whether any does.");
        _offscreenSkinning = c.CreateEntry("OffscreenSkinning", true, description: "Keep animating the skin of characters out of view where the game asks to (false stops it). Their skeletons still move; only their skin waits until they're seen.");
        _uploadMs = c.CreateEntry("UploadMs", 0, description: "Milliseconds a frame for sending loaded textures and meshes to the graphics card (0 keeps the game's). More loads new areas with fewer hitches.");
        _uploadBufferMb = c.CreateEntry("UploadBufferMb", 0, description: "Size of the buffer for those uploads, in MB (0 keeps the game's). A bigger one takes large textures without stalling.");
        _warmShaders = c.CreateEntry("WarmShaders", true, description: "Prepare every shader variant the game ships with as each scene loads, so materials don't stutter the first time they're seen. The game only does this on some platforms, not PC; it takes about 50 ms, once.");
        _benchmark = c.CreateEntry("Benchmark", false, description: "Measure what each setting saves: once you're in game, stand still for about 40 minutes while Lens tries each one on its own and logs the GPU time it takes (from SteamVR). Every setting is put back afterwards and this turns itself off.");
        foreach (var entry in c.Entries)
            entry.OnEntryValueChangedUntyped.Subscribe((_, _) => Changed?.Invoke());
        c.SaveToFile(false);
    }

    public static bool Off(string mode) => mode.Equals("Off", StringComparison.OrdinalIgnoreCase);
    public static bool Hard(string mode) => mode.Equals("Hard", StringComparison.OrdinalIgnoreCase);
}
