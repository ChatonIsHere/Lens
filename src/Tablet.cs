using System;
using MelonLoader;
using TabletUILib;

namespace Lens;

// Lens's settings on the in-game tablet, with TabletUILib (optional: Lens only loads this class when it's there).
// Each row shows a setting's value and steps to the next on a press. Changes apply at once and go back unless kept
// (Unconfirmed).
internal static class Tablet
{
    public static void Register() => TabletUI.AddApp("Lens", "LENS", Home);

    private static Page Start(string title, string about)
    {
        var page = new Page(title, Unconfirmed.Pending ? $"Keep these settings? Going back in {Unconfirmed.SecondsLeft} s" : about);
        if (Unconfirmed.Pending)
            page.Button("keep", "keep these", Unconfirmed.Keep)
                .Button("go back", "as they were", () => Unconfirmed.Revert());
        return page;
    }

    private static Page Home() =>
        Start("LENS", "Graphics settings; each applies at once")
            .Link("resolution", "and anti-aliasing", Resolution)
            .Link("shadows", Shadows)
            .Link("distance", "trees, props, view", Distance)
            .Link("extras", "effects, characters, loading", Extras);

    private static Page Resolution()
    {
        var page = Start("RESOLUTION", "Pixels each eye renders");
        Pick(page, "render scale", LensConfig._renderScale, (0f, "game"), (0.6f, "60%"), (0.7f, "70%"), (0.8f, "80%"), (0.9f, "90%"), (1.1f, "110%"), (1.25f, "125%"));
        Pick(page, "anti-aliasing", LensConfig._msaa, (-1, "game"), (0, "off"), (2, "2x"), (4, "4x"), (8, "8x"));
        Pick(page, "texture sharpness", LensConfig._anisotropic, ("", "game"), ("Off", "off"), ("On", "on"), ("Forced", "forced"));
        Pick(page, "desktop window", LensConfig._desktopView, ("", "game"), ("OneEye", "one eye"), ("Off", "off"));
        return page;
    }

    private static Page Shadows()
    {
        var page = Start("SHADOWS", "Fire shadows cost the most");
        Pick(page, "fire shadows", LensConfig._fireShadows, ("", "game"), ("Hard", "hard"), ("Off", "off"));
        Pick(page, "fire shadows within", LensConfig._fireShadowDistance, (0f, "game"), (10f, "10 m"), (20f, "20 m"), (30f, "30 m"), (50f, "50 m"));
        Pick(page, "fire shadow detail", LensConfig._fireShadowResolution, ("", "game"), ("Low", "low"), ("Medium", "medium"), ("High", "high"));
        Pick(page, "small things' shadows", LensConfig._smallShadowSize, (0f, "game"), (0.3f, "none under 0.3 m"), (0.5f, "none under 0.5 m"), (1f, "none under 1 m"));
        Pick(page, "distance", LensConfig._shadowDistance, (0f, "game"), (20f, "20 m"), (40f, "40 m"), (70f, "70 m"), (100f, "100 m"), (150f, "150 m"));
        Pick(page, "cascades", LensConfig._shadowCascades, (0, "game"), (1, "1"), (2, "2"), (4, "4"));
        Pick(page, "resolution", LensConfig._shadowResolution, ("", "game"), ("Low", "low"), ("Medium", "medium"), ("High", "high"), ("VeryHigh", "very high"));
        Pick(page, "sun shadows", LensConfig._sunShadows, ("", "game"), ("Hard", "hard"), ("Off", "off"));
        Pick(page, "pixel lights", LensConfig._pixelLights, (0, "game"), (1, "1"), (2, "2"), (3, "3"), (4, "4"), (6, "6"), (8, "8"));
        return page;
    }

    private static Page Distance()
    {
        var page = Start("DISTANCE", "How far detail is drawn");
        Pick(page, "trees", LensConfig._treeDistance, (1f, "game"), (0.5f, "50%"), (0.75f, "75%"), (1.25f, "125%"), (1.5f, "150%"));
        Pick(page, "grass and props", LensConfig._lodBias, (0f, "game"), (0.5f, "0.5"), (0.7f, "0.7"), (1f, "1"), (1.5f, "1.5"), (2f, "2"));
        Pick(page, "view distance", LensConfig._viewDistance, (0f, "game"), (300f, "300 m"), (600f, "600 m"), (1000f, "1000 m"));
        Pick(page, "small things", LensConfig._cullDistance, (0f, "game"), (40f, "40 m"), (60f, "60 m"), (150f, "150 m"), (250f, "250 m"));
        return page;
    }

    private static Page Extras()
    {
        var page = Start("EXTRAS", "Extra renders and effects");
        Pick(page, "others' viewfinders", LensConfig._otherPlayersCameras, (true, "on"), (false, "off"));
        Pick(page, "overview camera", LensConfig._overviewCamera, (true, "on"), (false, "off"));
        Pick(page, "bloom", LensConfig._bloom, (true, "on"), (false, "off"));
        Pick(page, "grass", LensConfig._grass, (true, "on"), (false, "off"));
        Pick(page, "water reflections", LensConfig._waterReflections, (true, "on"), (false, "off"));
        Pick(page, "skin out of view", LensConfig._offscreenSkinning, (true, "game"), (false, "off"));
        Pick(page, "skin weights", LensConfig._skinWeights, (0, "game"), (1, "1 bone"), (2, "2 bones"), (4, "4 bones"));
        Pick(page, "upload time", LensConfig._uploadMs, (0, "game"), (2, "2 ms"), (4, "4 ms"), (8, "8 ms"));
        Pick(page, "upload buffer", LensConfig._uploadBufferMb, (0, "game"), (16, "16 MB"), (32, "32 MB"), (64, "64 MB"));
        Pick(page, "warm shaders", LensConfig._warmShaders, (true, "on"), (false, "off, next load"));
        page.Button("render report", "to the log now", RenderReport.Now);
        return page;
    }

    // A row showing the setting's value; a press steps to the next option. A value set in the file that isn't one of
    // the options shows as it is, and a press goes to the first.
    private static Page Pick<T>(Page page, string label, MelonPreferences_Entry<T> entry, params (T value, string name)[] options)
    {
        int at = Array.FindIndex(options, o => Equals(o.value, entry.Value));
        string shown = at >= 0 ? options[at].name : $"{entry.Value} (from the file)";
        return page.Add(label, shown, () => Unconfirmed.Set(entry, options[(at + 1) % options.Length].value));
    }
}
