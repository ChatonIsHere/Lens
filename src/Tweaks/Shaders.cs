using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace Lens.Tweaks;

// WarmShaders: the game ships lists of the shader variants its materials use, and warms them while a scene loads on
// the platforms it lists them for. Lens warms any still cold as each scene loads, so the first sight of a material
// doesn't stutter while its shader compiles. The cost is a longer load, once.
internal static class Shaders
{
    public static void Warm()
    {
        if (!LensConfig.WarmShaders) return;
        var cold = Resources.FindObjectsOfTypeAll<ShaderVariantCollection>().Where(c => c != null && !c.isWarmedUp).ToList();
        if (cold.Count == 0) return;
        var watch = Stopwatch.StartNew();
        int variants = 0;
        foreach (var c in cold)
        {
            variants += c.variantCount;
            c.WarmUp();
        }
        LensMod.Log.Msg($"Warmed {cold.Count} shader lists ({variants} variants) in {watch.ElapsedMilliseconds} ms.");
    }
}
