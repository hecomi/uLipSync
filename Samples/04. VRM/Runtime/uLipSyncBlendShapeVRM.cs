using UnityEngine;
using VRM;

namespace uLipSync
{

[RequireComponent(typeof(VRMBlendShapeProxy))]
public class uLipSyncBlendShapeVRM : uLipSyncBlendShape
{
    protected override void LateUpdateBlendShapes()
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
            var clip = clips[index];
            proxy.AccumulateValue(clip.Key, bs.normalizedWeight);
        }
        proxy.Apply();
    }
}

}