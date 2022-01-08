using UnityEngine.Playables;

namespace uLipSync.Timeline
{

public class uLipSyncBehaviour : PlayableBehaviour
{
    public uLipSyncClip asset;
    public BakedFrame frame { get; private set; } = BakedFrame.zero;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var t = (float)playable.GetTime() + asset.timeOffset;
        frame = asset.bakedData.GetFrame(t);
    }
}

}