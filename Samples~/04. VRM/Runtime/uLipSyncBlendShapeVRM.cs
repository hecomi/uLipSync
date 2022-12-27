using UnityEngine;
using VRM;

namespace uLipSync
{

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
}

}