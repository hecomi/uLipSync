using UnityEngine;
#if USE_VRM0X
using VRM;
#endif

namespace uLipSync
{

#if USE_VRM0X
    
[ExecuteAlways]
[RequireComponent(typeof(VRMBlendShapeProxy))]
public class uLipSyncBlendShapeVRM : uLipSyncBlendShape
{
    protected override void OnApplyBlendShapes()
    {
        var proxy = GetComponent<VRMBlendShapeProxy>();
        if (!proxy) return;

        var avatar = proxy.BlendShapeAvatar;
        if (!avatar) return;

        var clips = avatar.Clips;
        foreach (var bs in blendShapes)
        {
            var index = bs.index + 1;
            if (index < 0 || index >= clips.Count) continue;
            var key = clips[index].Key;
            var weight = bs.weight * bs.maxWeight * volume;
            proxy.AccumulateValue(key, weight);
        }
        proxy.Apply();
    }

    public new BlendShapeInfo AddBlendShape(string phoneme, string blendShape)
    {
        var bs = GetBlendShapeInfo(phoneme);
        if (bs == null) bs = new BlendShapeInfo() { phoneme = phoneme };

        blendShapes.Add(bs);

        var proxy = GetComponent<VRMBlendShapeProxy>();
        if (!proxy || !proxy.BlendShapeAvatar) return bs;

        var clips = proxy.BlendShapeAvatar.Clips;
        int index = clips.FindIndex(x => x.BlendShapeName == blendShape);
        bs.index = index - 1;

        return bs;
    }
}

#else

public class uLipSyncBlendShapeVRM : uLipSyncBlendShape
{
}

#endif

}