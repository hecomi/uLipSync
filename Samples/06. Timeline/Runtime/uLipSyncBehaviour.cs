using UnityEngine;
using UnityEngine.Playables;

namespace uLipSync.Timeline
{

public class uLipSyncBehaviour : PlayableBehaviour
{
    public uLipSyncClip asset;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var target = playerData as uLipSyncTimelineEvent;
        if (target == null) return;

        target.bakedData = asset.bakedData;
        target.OnFrame((float)playable.GetTime());
    }
}

}