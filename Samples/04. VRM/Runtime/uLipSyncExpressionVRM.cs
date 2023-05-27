using System.Linq;
using UnityEngine;
#if USE_VRM10
using UniVRM10;
#endif

namespace uLipSync
{

#if USE_VRM10
    
[ExecuteAlways]
[RequireComponent(typeof(Vrm10Instance))]
public class uLipSyncExpressionVRM : uLipSyncBlendShape
{
    protected override void OnApplyBlendShapes()
    {
        var vrm10Instance = GetComponent<Vrm10Instance>();
        if (!vrm10Instance || !vrm10Instance.Vrm) return;

        var clips = vrm10Instance.Vrm.Expression.Clips.ToArray();
        foreach (var bs in blendShapes)
        {
            var index = bs.index + 1;
            if (index < 0 || index >= clips.Length) continue;
            var clip = clips[index];
            var weight = bs.weight * bs.maxWeight * volume;
            var key = new ExpressionKey(clip.Preset, clip.Clip.name);
            vrm10Instance.Runtime.Expression.SetWeight(key, weight);
        }
    }

    public new BlendShapeInfo AddBlendShape(string phoneme, string expression)
    {
        var bs = GetBlendShapeInfo(phoneme);
        if (bs == null) bs = new BlendShapeInfo() { phoneme = phoneme };

        blendShapes.Add(bs);

        var vrm10Instance = GetComponent<Vrm10Instance>();
        if (!vrm10Instance || !vrm10Instance.Vrm) return bs;

        var clips = vrm10Instance.Vrm.Expression.Clips;
        int index = clips.ToList().FindIndex(x => x.Clip.name == expression);
        bs.index = index - 1;

        return bs;
    }
}

#else

public class uLipSyncExpressionVRM : uLipSyncBlendShape
{
}

#endif

}